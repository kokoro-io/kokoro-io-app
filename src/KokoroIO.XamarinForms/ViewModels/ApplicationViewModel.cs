using System;
using System.Collections.Generic;
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

            _LoginUser = GetProfile(loginUser);

            client.ProfileUpdated += Client_ProfileUpdated;
            client.MessageCreated += Client_MessageCreated;
            client.MessageUpdated += Client_MessageUpdated;
            client.Disconnected += Client_Disconnected;
        }

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

        public Task<Profile[]> GetProfilesAsync()
            => EnqueueClientTask(() => Client.GetProfilesAsync());

        public async Task<ProfileViewModel> PutProfileAsync(string screenName = null, string displayName = null, Stream avatar = null)
        {
            var p = await EnqueueClientTask(() => Client.PutProfileAsync(screenName: screenName, displayName: displayName, avatar: avatar));

            var pvm = GetProfile(p);

            LoginUser = pvm;

            return pvm;
        }

        public Task<Membership[]> GetMembershipsAsync(bool? archived = null, Authority? authority = null)
            => EnqueueClientTask(() => Client.GetMembershipsAsync(archived: archived, authority: authority));

        public Task<Channel> GetChannelMembershipsAsync(string channelId)
            => EnqueueClientTask(() => Client.GetChannelMembershipsAsync(channelId));

        public Task<Channel[]> GetChannelsAsync(bool? archived = null)
            => EnqueueClientTask(() => Client.GetChannelsAsync(archived: archived));

        public Task<Message[]> GetMessagesAsync(string channelId, int? limit = null, int? beforeId = null, int? afterId = null)
            => EnqueueClientTask(() => Client.GetMessagesAsync(channelId, limit: limit, beforeId: beforeId, afterId: afterId));

        public Task<Message> PostMessageAsync(string channelId, string message, bool isNsfw, Guid idempotentKey = default(Guid))
            => EnqueueClientTask(() => Client.PostMessageAsync(channelId, message, isNsfw, idempotentKey));

        public async Task<ChannelViewModel> PostDirectMessageChannelAsync(string targetUserProfileId)
        {
            var channel = await EnqueueClientTask(() => Client.PostDirectMessageChannelAsync(targetUserProfileId));

            var rvm = Channels.FirstOrDefault(r => r.Id == channel.Id);

            if (rvm == null)
            {
                rvm = new ChannelViewModel(this, channel);

                for (var i = 0; i < Channels.Count; i++)
                {
                    var aft = Channels[i];

                    if ((aft.Kind == ChannelKind.DirectMessage && aft.ChannelName.CompareTo(rvm.ChannelName) > 0)
                        || aft.IsArchived)
                    {
                        Channels.Insert(i, rvm);
                        return rvm;
                    }
                }

                Channels.Add(rvm);
            }

            return rvm;
        }

        #endregion kokoro.io API Client

        #region Channels

        private Task _LoadInitialDataTask;

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
            var channels = memberships.Select(m => m.Channel);

            foreach (var r in channels.OrderBy(e => e.IsArchived ? 1 : 0)
                                    .ThenBy(e => (int)e.Kind)
                                    .ThenBy(e => e.ChannelName, StringComparer.Ordinal)
                                    .ThenBy(e => e.Id))
            {
                var rvm = new ChannelViewModel(this, r);

                _Channels.Add(rvm);
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

        #endregion Channels

        #region Profiles

        private ProfileViewModel _LoginUser;

        public ProfileViewModel LoginUser
        {
            get => _LoginUser;
            private set => SetProperty(ref _LoginUser, value);
        }

        private readonly Dictionary<string, ProfileViewModel> _Profiles = new Dictionary<string, ProfileViewModel>();

        internal ProfileViewModel GetProfile(Message model)
        {
            var key = model.Profile.Id + '\0' + model.Profile.ScreenName + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel
                    (
                        this,
                        model.Profile.Id,
                        model.Avatar,
                        model.DisplayName,
                        model.Profile.ScreenName,
                        model.Profile.Type == ProfileType.Bot
                    );

                _Profiles[key] = p;
            }
            return p;
        }

        internal ProfileViewModel GetProfile(Profile model)
        {
            var key = model.Id + '\0' + model.ScreenName + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(this, model);
                _Profiles[key] = p;
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

            var pns = UserSettings.PnsHandle;

            UserSettings.Password = null;
            UserSettings.AccessToken = null;
            UserSettings.PnsHandle = null;
            await App.Current.SavePropertiesAsync();

            try
            {
                var ds = DependencyService.Get<IDeviceService>();
                ds.UnregisterPlatformNotificationService();

                if (pns != null && avm != null)
                {
                    await avm.Client.PostDeviceAsync(ds.MachineName, ds.Kind, Convert.ToBase64String(ds.GetIdentifier()), null, false);
                }
            }
            catch { }

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

        private void OpenUrl(object url)
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
                            mp.SelectProfile(u.AbsolutePath.Substring(2));

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
            if (e.Data?.Id == _LoginUser.Id)
            {
                LoginUser = GetProfile(e.Data);
            }
        }

        private void Client_MessageCreated(object sender, EventArgs<Message> e)
        {
            if (e.Data == null)
            {
                return;
            }
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
        }

        private void Client_MessageUpdated(object sender, EventArgs<Message> e)
        {
            if (e.Data == null)
            {
                return;
            }
            var mp = _Channels?.FirstOrDefault(r => r.Id == e.Data.Channel.Id)?.MessagesPage;

            MessagingCenter.Send(this, "MessageUpdated", e.Data);

            if (mp != null)
            {
                mp.UpdateMessage(e.Data);
            }
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