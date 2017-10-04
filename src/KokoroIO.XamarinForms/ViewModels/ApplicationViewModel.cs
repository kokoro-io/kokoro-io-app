using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Views;
using Realms;
using Xamarin.Forms;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services.Media;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ApplicationViewModel : ObservableObject
    {
        internal ApplicationViewModel(Client client, Profile loginUser)
        {
            Client = client;
            OpenUrlCommand = new Command(OpenUrl);

            _LoginUser = GetProfileViewModel(loginUser);

            client.ProfileUpdated += Client_ProfileUpdated;
            client.MessageCreated += Client_MessageCreated;
            client.MessageUpdated += Client_MessageUpdated;
            client.ChannelsUpdated += Client_ChannelsUpdated;

            client.MemberJoined += Client_MemberJoined;
            client.MemberLeaved += Client_MemberLeaved;

            client.Disconnected += Client_Disconnected;
        }

        private float _DisplayScale;

        public float DisplayScale
            => _DisplayScale <= 0 ? _DisplayScale = DependencyService.Get<IDeviceService>().GetDisplayScale() : _DisplayScale;

        #region kokoro.io API Client

        private readonly Client Client;
        private readonly Queue<Func<Task>> _ClientTasks = new Queue<Func<Task>>();

        private void EnqueueClientTaskCore(Func<Task> taskExecutor)
        {
            lock (_ClientTasks)
            {
                _ClientTasks.Enqueue(taskExecutor);

                if (_ClientTasks.Count == 1)
                {
                    ExecuteClientTasks().GetHashCode();
                }
            }
        }

        private async Task ExecuteClientTasks()
        {
            for (; ; )
            {
                Func<Task> e;
                lock (_ClientTasks)
                {
                    if (!_ClientTasks.Any())
                    {
                        return;
                    }
                    e = _ClientTasks.Dequeue();
                }

                try
                {
                    await e().ConfigureAwait(false);
                }
                catch { }
            }
        }

        private Task EnqueueClientTask(Func<Task> task)
        {
            var tcs = new TaskCompletionSource<object>();

            EnqueueClientTaskCore(() => task().ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    tcs.SetResult(null);
                }
                else if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception);
                }
                else
                {
                    tcs.SetCanceled();
                }
            }));

            return tcs.Task;
        }

        private Task<T> EnqueueClientTask<T>(Func<Task<T>> task)
        {
            var tcs = new TaskCompletionSource<T>();

            EnqueueClientTaskCore(() => task().ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    tcs.SetResult(t.Result);
                }
                else if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception);
                }
                else
                {
                    tcs.SetCanceled();
                }
            }));

            return tcs.Task;
        }

        public async Task<ProfileViewModel> PutProfileAsync(string screenName = null, string displayName = null, Stream avatar = null)
        {
            var p = await EnqueueClientTask(() => Client.PutProfileAsync(screenName: screenName, displayName: displayName, avatar: avatar));

            var pvm = GetProfileViewModel(p);

            LoginUser = pvm;

            return pvm;
        }

        public async Task<Membership[]> GetMembershipsAsync(bool? archived = null, Authority? authority = null)
        {
            var r = await EnqueueClientTask(() => Client.GetMembershipsAsync(archived: archived, authority: authority)).ConfigureAwait(false);

            foreach (var ms in r)
            {
                GetProfileViewModel(ms.Profile);
                GetOrCreateJoinedChannelViewModel(ms);
            }

            return r;
        }

        public Task DeleteMembershipAsync(string membershipId)
            => EnqueueClientTask(() => Client.DeleteMembershipAsync(membershipId));

        public async Task<ChannelViewModel> GetChannelAsync(string channelId)
        {
            var r = await EnqueueClientTask(() => Client.GetChannelAsync(channelId)).ConfigureAwait(false);
            return GetOrCreateChannelViewModel(r);
        }

        public async Task<ChannelViewModel[]> GetChannelsAsync(bool? archived = null)
        {
            var r = await EnqueueClientTask(() => Client.GetChannelsAsync(archived: archived)).ConfigureAwait(false);
            return r.Select(c => GetOrCreateChannelViewModel(c)).ToArray();
        }

        public async Task<Channel> GetChannelMembershipsAsync(string channelId)
        {
            var r = await EnqueueClientTask(() => Client.GetChannelMembershipsAsync(channelId)).ConfigureAwait(false);

            GetChannelViewModel(r.Id)?.Update(r);

            foreach (var ms in r.Memberships)
            {
                GetProfileViewModel(ms.Profile);
            }

            return r;
        }

        public async Task<Message[]> GetMessagesAsync(string channelId, int? limit = null, int? beforeId = null, int? afterId = null)
        {
            var r = await EnqueueClientTask(() => Client.GetMessagesAsync(channelId, limit: limit, beforeId: beforeId, afterId: afterId)).ConfigureAwait(false);

            foreach (var ms in r)
            {
                GetProfileViewModel(ms.Profile);
            }

            return r;
        }

        public async Task<Message> PostMessageAsync(string channelId, string message, bool isNsfw, Guid idempotentKey = default(Guid))
        {
            var r = await EnqueueClientTask(() => Client.PostMessageAsync(channelId, message, isNsfw, idempotentKey)).ConfigureAwait(false);

            GetProfileViewModel(r.Profile);

            return r;
        }

        public async Task<ChannelViewModel> PostDirectMessageChannelAsync(string targetUserProfileId)
        {
            var channel = await EnqueueClientTask(() => Client.PostDirectMessageChannelAsync(targetUserProfileId));

            return GetOrCreateJoinedChannelViewModel(channel);
        }

        #endregion kokoro.io API Client

        #region Channels

        private Task _LoadInitialDataTask;

        private Dictionary<string, WeakReference<ChannelViewModel>> _ChannelDictionary;

        private ObservableRangeCollection<ChannelViewModel> _Channels;

        public ObservableRangeCollection<ChannelViewModel> Channels
        {
            get
            {
                if (_Channels == null)
                {
                    _Channels = new ObservableRangeCollection<ChannelViewModel>();
                }
                LoadInitialDataTask.GetHashCode();

                return _Channels;
            }
        }

        internal Task LoadInitialDataTask
            => _LoadInitialDataTask ?? (_LoadInitialDataTask = LoadInitialDataAsync());

        private async Task LoadInitialDataAsync()
        {
            if (_Channels == null)
            {
                _Channels = new ObservableRangeCollection<ChannelViewModel>();
            }

            var memberships = await GetMembershipsAsync().ConfigureAwait(false);

            foreach (var ms in memberships)
            {
                GetOrCreateJoinedChannelViewModel(ms);
            }

            try
            {
                string channelId;
                using (var realm = Realm.GetInstance())
                {
                    var rup = realm.All<ChannelUserProperties>().OrderByDescending(r => r.LastVisited).FirstOrDefault();

                    if (rup == null)
                    {
                        return;
                    }
                    else if (rup.UserId != _LoginUser.Id)
                    {
                        using (var trx = realm.BeginWrite())
                        {
                            realm.RemoveAll<ChannelUserProperties>();
                            trx.Commit();
                        }
                        return;
                    }
                    else
                    {
                        channelId = rup.ChannelId;
                    }
                }

                var rvm = _Channels.FirstOrDefault(r => r.Id == channelId);
                if (rvm != null)
                {
                    SelectedChannel = rvm;
                }
            }
            catch (Exception ex)
            {
                ex.Trace("LoadingChannelUserPropertiesFailed");
            }
        }

        private ChannelViewModel _SelectedChannel;

        public ChannelViewModel SelectedChannel
        {
            get => _SelectedChannel;
            set
            {
                if (value != _SelectedChannel)
                {
                    if (_SelectedChannel != null)
                    {
                        _SelectedChannel.IsSelected = false;

                        var mp = _SelectedChannel.MessagesPage;

                        if (mp != null)
                        {
                            mp.IsSubscribing = false;
                        }
                    }

                    _SelectedChannel = value;

                    OnPropertyChanged();

                    if (_SelectedChannel != null)
                    {
                        _SelectedChannel.IsSelected = true;
                        var mp = _SelectedChannel.GetOrCreateMessagesPage();
                        mp.IsSubscribing = true;
                        mp.SelectedProfile = null;
                    }
                    OnUnreadCountChanged();
                    if (Client.State == ClientState.Disconnected
                        && _Channels.Any())
                    {
                        ConnectAsync().GetHashCode();
                    }
                }
            }
        }

        private bool _HasNotificationInMenu;

        public bool HasNotificationInMenu
        {
            get => _HasNotificationInMenu;
            private set => SetProperty(ref _HasNotificationInMenu, value);
        }

        internal void OnUnreadCountChanged()
        {
            HasNotificationInMenu = _Channels?.Where(r => r != _SelectedChannel).Sum(r => r.UnreadCount) > 0;
        }

        internal ChannelViewModel GetChannelViewModel(string id)
            => _ChannelDictionary != null
            && _ChannelDictionary.TryGetValue(id, out var w)
            && w.TryGetTarget(out var v) ? v : null;

        internal ChannelViewModel GetOrCreateChannelViewModel(Channel channel)
        {
            if (channel == null)
            {
                return null;
            }

            ChannelViewModel c;
            if (_ChannelDictionary == null)
            {
                _ChannelDictionary = new Dictionary<string, WeakReference<ChannelViewModel>>();
            }
            else if (_ChannelDictionary.TryGetValue(channel.Id, out var w)
                    && w.TryGetTarget(out c))
            {
                c.Update(channel);
                return c;
            }
            c = new ChannelViewModel(this, channel);
            _ChannelDictionary[channel.Id] = new WeakReference<ChannelViewModel>(c);
            return c;
        }

        internal ChannelViewModel GetOrCreateJoinedChannelViewModel(Channel channel)
        {
            var cvm = GetOrCreateChannelViewModel(channel);

            if (cvm == null)
            {
                return null;
            }

            if (!Channels.Contains(cvm))
            {
                for (var i = 0; i < Channels.Count; i++)
                {
                    var aft = Channels[i];

                    if (!cvm.IsArchived && aft.IsArchived
                        || (cvm.IsArchived == aft.IsArchived
                            && (cvm.Kind < aft.Kind
                            || (cvm.Kind == aft.Kind && aft.ChannelName.CompareTo(cvm.ChannelName) > 0))))
                    {
                        Channels.Insert(i, cvm);
                        return cvm;
                    }
                }

                Channels.Add(cvm);
            }

            return cvm;
        }

        internal ChannelViewModel GetOrCreateJoinedChannelViewModel(Membership membership)
        {
            var cvm = GetOrCreateJoinedChannelViewModel(membership.Channel);
            cvm.Update(membership);

            return cvm;
        }

        #endregion Channels

        #region Profiles

        private ProfileViewModel _LoginUser;

        public ProfileViewModel LoginUser
        {
            get => _LoginUser;
            private set => SetProperty(ref _LoginUser, value);
        }

        private readonly Dictionary<string, Profile> _ProfileModels = new Dictionary<string, Profile>();
        private readonly Dictionary<string, ProfileViewModel> _Profiles = new Dictionary<string, ProfileViewModel>();

        internal Profile GetProfile(Message model)
        {
            var key = model.Profile.Id + '\0' + model.Profile.ScreenName + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_ProfileModels.TryGetValue(key, out var p))
            {
                p = new Profile()
                {
                    Avatar = model.Avatar,
                    Avatars = model.Avatars,
                    DisplayName = model.DisplayName,

                    Id = model.Profile.Id,
                    ScreenName = model.Profile.ScreenName,
                    IsArchived = model.Profile.IsArchived,
                    Type = model.Profile.Type
                };
                _ProfileModels[key] = p;
            }
            return p;
        }

        internal ProfileViewModel GetProfileViewModel(Profile model)
        {
            if (_Profiles.TryGetValue(model.Id, out var p))
            {
                p.Update(model);
            }
            else
            {
                p = new ProfileViewModel(this, model);
                _Profiles[model.Id] = p;
            }
            return p;
        }

        #endregion Profiles

        #region LogoutCommand

        private static Command _LogoutCommand;

        public static Command LogoutCommand
            => _LogoutCommand ?? (_LogoutCommand = new Command(BeginLogout));

        public static async void BeginLogout()
        {
            var avm = App.Current?.MainPage?.BindingContext as ApplicationViewModel;
            if (avm != null)
            {
                try
                {
                    await avm.CloseAsync();
                }
                catch { }
            }

            UserSettings.Password = null;
            UserSettings.AccessToken = null;
            UserSettings.PnsHandle = null;
            await App.Current.SavePropertiesAsync();

            try
            {
                var ds = DependencyService.Get<IDeviceService>();
                ds.UnregisterPlatformNotificationService();

                if (avm != null)
                {
                    await avm.Client.DeleteDeviceAsync(ds.GetDeviceIdentifierString());
                }
            }
            catch (Exception ex)
            {
                ex.Trace("DeleteDeviceFailed");
            }

            App.Current.MainPage = new LoginPage();
        }

        #endregion LogoutCommand

        #region SettingCommand

        private Command _SettingCommand;

        public Command SettingsCommand
            => _SettingCommand ?? (_SettingCommand = new Command(async (p) =>
            {
                await App.Current.MainPage.Navigation.PushModalAsync(new SettingsPage(new SettingsViewModel(this)
                {
                    InitialPageType = p as Type
                }));
            }));

        #endregion SettingCommand

        #region PopToRootCommand

        private static Command _PopToRootCommand;

        public static Command PopToRootCommand
            => _PopToRootCommand ?? (_PopToRootCommand = new Command(async () =>
            {
                var nav = App.Current.MainPage.Navigation;

                try
                {
                    while (nav.ModalStack.Any())
                    {
                        await nav.PopModalAsync();
                    }
                }
                catch { }
            }));

        #endregion PopToRootCommand

        public Command OpenUrlCommand { get; }

        private async void OpenUrl(object url)
        {
            var u = url as Uri ?? (url is string s ? new Uri(s) : null);

            if (u != null)
            {
                if (u.Scheme == "https"
                    && u.Host == "kokoro.io")
                {
                    if (u.AbsolutePath.StartsWith("/@"))
                    {
                        var mp = SelectedChannel.MessagesPage;

                        if (mp != null)
                        {
                            mp.SelectProfile(u.AbsolutePath.Substring(2), u.Fragment?.Length == 10 ? u.Fragment.Substring(1) : null);

                            return;
                        }
                    }
                    else if (u.AbsolutePath.StartsWith("/channels/"))
                    {
                        var id = u.AbsolutePath.Substring("/channels/".Length);

                        // TODO: search channel by id
                        var ch = GetChannelViewModel(id)
                                    ?? await GetChannelAsync(id);
                        if (ch != null)
                        {
                            ch.ShowDetailCommand.Execute(null);

                            return;
                        }
                    }
                }

                XDevice.OpenUri(u);
            }
        }

        #region Connection

        public async Task ConnectAsync()
        {
            if (Client.State == ClientState.Disconnected)
            {
                await Client.ConnectAsync();
                await Client.SubscribeAsync(Channels.Select(r => r.Id));

                OnPropertyChanged(nameof(IsDisconnected));
            }
        }

        public async Task CloseAsync()
        {
            await Client.CloseAsync();
        }

        public bool IsDisconnected
            => Channels.Any(r => !r.IsArchived)
            && Client.State == ClientState.Disconnected;

        private Command _ConnectCommand;

        public Command ConnectCommand
            => _ConnectCommand ?? (_ConnectCommand = new Command(async () =>
             {
                 if (IsDisconnected)
                 {
                     try
                     {
                         await ConnectAsync();
                     }
                     catch (Exception ex)
                     {
                         ex.Trace("Connection failed");

                         BeginReconnect();
                     }
                 }
             }));

        #region Client Events

        private void Client_ProfileUpdated(object sender, EventArgs<Profile> e)
        {
            if (e.Data == null)
            {
                return;
            }
            Debug.WriteLine("Profile updated: {0}", e.Data.Id);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                GetProfileViewModel(e.Data);
            });
        }

        private void Client_MessageCreated(object sender, EventArgs<Message> e)
        {
            if (e.Data == null)
            {
                return;
            }
            Debug.WriteLine("Message created: {0}", e.Data.Id);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                GetProfileViewModel(e.Data.Profile);

                var rvm = _Channels?.FirstOrDefault(r => r.Id == e.Data.Channel.Id);

                MessagingCenter.Send(this, "MessageCreated", e.Data);

                if (rvm != null)
                {
                    rvm.UnreadCount++;
                    if (!rvm.NotificationDisabled
                        && UserSettings.PlayRingtone)
                    {
                        try
                        {
                            DependencyService.Get<IAudioService>()?.PlayNotification();
                        }
                        catch { }
                    }
                }
            });
        }

        private void Client_MessageUpdated(object sender, EventArgs<Message> e)
        {
            if (e.Data == null)
            {
                return;
            }

            Debug.WriteLine("Message updated: {0}", e.Data.Id);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                GetProfileViewModel(e.Data.Profile);

                var mp = _Channels?.FirstOrDefault(r => r.Id == e.Data.Channel.Id)?.MessagesPage;

                MessagingCenter.Send(this, "MessageUpdated", e.Data);

                if (mp != null)
                {
                    mp.UpdateMessage(e.Data);
                }
            });
        }

        private void Client_ChannelsUpdated(object sender, EventArgs<Channel[]> e)
        {
            if (e.Data == null)
            {
                return;
            }

            Debug.WriteLine("Channels updated: {0} channels", e.Data.Length);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                foreach (var c in e.Data)
                {
                    GetOrCreateJoinedChannelViewModel(c);
                }

                Channels.RemoveRange(Channels.Where(c => !e.Data.Any(d => d.Id == c.Id)).ToArray());

                // TODO: subscribe channels
            });
        }

        private void Client_MemberJoined(object sender, EventArgs<Membership> e)
        {
            if (e.Data == null)
            {
                return;
            }
            Debug.WriteLine("Member joined: @{1} joined to {0} as {2}", e.Data.Channel.ChannelName, e.Data.Profile.ScreenName, e.Data.Authority);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                GetProfileViewModel(e.Data.Profile);

                Channels.FirstOrDefault(c => c.Id == e.Data.Id)?.Join(e.Data);
            });
        }

        private void Client_MemberLeaved(object sender, EventArgs<Membership> e)
        {
            if (e.Data == null)
            {
                return;
            }
            Debug.WriteLine("Member leaved: @{1} leaved from {0}", e.Data.Channel.ChannelName, e.Data.Profile.ScreenName);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                GetProfileViewModel(e.Data.Profile);

                Channels.FirstOrDefault(c => c.Id == e.Data.Id)?.Leave(e.Data);
            });
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsDisconnected));

            TH.TraceError("Disconnected");

            // TODO: check for explicit close

            BeginReconnect();
        }

        #endregion Client Events

        private async void BeginReconnect()
        {
            if (IsDisconnected)
            {
                await Task.Delay(5);
                if (IsDisconnected)
                {
                    try
                    {
                        await ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        ex.Trace("Reconnection failed");

                        BeginReconnect();
                    }
                }
            }
        }

        #endregion Connection

        #region Uploader

        private object _MediaPicker;

        internal IMediaPicker MediaPicker
        {
            get
            {
                if (_MediaPicker == null)
                {
                    try
                    {
                        _MediaPicker = DependencyService.Get<IMediaPicker>()
                                    ?? Resolver.Resolve<IDevice>()?.MediaPicker;
                    }
                    catch { }
                    _MediaPicker = _MediaPicker ?? new object();
                }
                return _MediaPicker as IMediaPicker;
            }
        }

        internal async void BeginUpload(UploadParameter parameter)
        {
            var su = GetSelectedUploader();

            if (su?.IsAuthorized != true)
            {
                await App.Current.MainPage.Navigation.PushModalAsync(new UploadersPage()
                {
                    BindingContext = new UploadersViewModel(this, parameter)
                });

                return;
            }
            await BeginSelectImage(su, parameter);
        }

        internal async Task BeginSelectImage(IImageUploader uploader, UploadParameter parameter)
        {
            var nav = App.Current.MainPage.Navigation;
            while (nav.ModalStack.Count > 0)
            {
                await nav.PopModalAsync();
            }

            try
            {
                string url;
                if (parameter.Data != null)
                {
                    using (parameter.Data)
                    {
                        url = await uploader.UploadAsync(parameter.Data, "pasted");
                    }
                }
                else
                {
                    var mp = MediaPicker;

                    if (mp == null)
                    {
                        parameter.OnFaulted?.Invoke("no media picker defined");
                        return;
                    }

                    var mf = await (parameter.UseCamera ? mp.TakePhotoAsync(new CameraMediaStorageOptions())
                                    : mp.SelectPhotoAsync(new CameraMediaStorageOptions()));

                    if (mf == null)
                    {
                        parameter.OnFaulted?.Invoke(null);
                        return;
                    }

                    using (var ms = mf.Source)
                    {
                        url = await uploader.UploadAsync(ms, Path.GetFileName(mf.Path));
                    }
                }

                SetSelectedUploader(uploader);

                try
                {
                    await App.Current.SavePropertiesAsync();
                }
                catch { }

                parameter.OnCompleted(url);
            }
            catch (Exception ex)
            {
                ex.Trace("Image Uploader failed");

                parameter.OnFaulted?.Invoke(ex.Message);
            }
        }

        internal static readonly IImageUploader[] Uploaders =
            { new GyazoImageUploader(), new ImgurImageUploader() };

        internal static IImageUploader GetSelectedUploader()
        {
            if (App.Current.Properties.TryGetValue(nameof(GetSelectedUploader), out var obj)
                && obj is string s)
            {
                return Uploaders.FirstOrDefault(u => u.GetType().FullName == s);
            }

            return null;
        }

        internal static void SetSelectedUploader(IImageUploader uploader)
        {
            App.Current.Properties[nameof(GetSelectedUploader)] = uploader?.GetType().FullName;
        }

        #endregion Uploader

        #region Document Interaction

        private static Func<Stream> _RequestedFile;

        public static void OpenFile(Func<Stream> streamCreator)
        {
            var avm = App.Current?.MainPage?.BindingContext as ApplicationViewModel;

            _RequestedFile = streamCreator;
            if (avm != null)
            {
                // Logged in
                avm.BeginProcessPendingFile();
            }
        }

        internal async void BeginProcessPendingFile()
        {
            var sc = _RequestedFile;
            _RequestedFile = null;

            if (sc != null)
            {
                var nav = App.Current.MainPage.Navigation;

                await nav.PushModalAsync(new UploadToChannelPage()
                {
                    BindingContext = new UploadToChannelViewModel(this, sc)
                });
            }
        }

        #endregion Document Interaction
    }
}