using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.Views;
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
        }

        private float _DisplayScale;

        public float DisplayScale
            => _DisplayScale <= 0 ? _DisplayScale = SH.Device.GetDisplayScale() : _DisplayScale;

        #region kokoro.io API Client

        private Client _Client;

        private Client Client
        {
            get => _Client;
            set
            {
                if (value == _Client)
                {
                    return;
                }
                if (_Client != null)
                {
                    _Client.ProfileUpdated -= Client_ProfileUpdated;
                    _Client.MessageCreated -= Client_MessageCreated;
                    _Client.MessageUpdated -= Client_MessageUpdated;
                    _Client.ChannelsUpdated -= Client_ChannelsUpdated;
                    _Client.MemberJoined -= Client_MemberJoined;
                    _Client.MemberLeaved -= Client_MemberLeaved;

                    _Client.Disconnected -= Client_Disconnected;

                    _Client.Dispose();
                }
                _Client = value;
                if (_Client != null)
                {
                    _Client.ProfileUpdated += Client_ProfileUpdated;
                    _Client.MessageCreated += Client_MessageCreated;
                    _Client.MessageUpdated += Client_MessageUpdated;
                    _Client.ChannelsUpdated += Client_ChannelsUpdated;
                    _Client.MemberJoined += Client_MemberJoined;
                    _Client.MemberLeaved += Client_MemberLeaved;

                    _Client.Disconnected += Client_Disconnected;
                }
            }
        }

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
                    using (TH.BeginScope("Invoking REST API"))
                    {
                        await e().ConfigureAwait(false);
                    }
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
            Profile p;
            using (TH.BeginScope("Executing PUT /profiles/me"))
            {
                p = await EnqueueClientTask(() => Client.PutProfileAsync(screenName: screenName, displayName: displayName, avatar: avatar));
            }

            var pvm = GetProfileViewModel(p);

            LoginUser = pvm;

            return pvm;
        }

        public async Task<Membership[]> GetMembershipsAsync(bool? archived = null, Authority? authority = null)
        {
            Membership[] r;
            using (TH.BeginScope("Executing GET /memberships"))
            {
                r = await EnqueueClientTask(() => Client.GetMembershipsAsync(archived: archived, authority: authority)).ConfigureAwait(false);
            }

            foreach (var ms in r)
            {
                GetProfileViewModel(ms.Profile);
                GetOrCreateJoinedChannelViewModel(ms);
            }

            await GetLastReadIdAsync().ConfigureAwait(false);

            await SubscribeAsync().ConfigureAwait(false);

            return r;
        }

        public async Task<ChannelViewModel> PutMembershipAsync(string membershipId, bool? disableNotification = null)
        {
            Membership ms;
            using (TH.BeginScope("Executing PUT /memberships"))
            {
                ms = await EnqueueClientTask(() => Client.PutMembershipAsync(membershipId, disableNotification)).ConfigureAwait(false);
            }
            return GetOrCreateJoinedChannelViewModel(ms);
        }

        public async Task DeleteMembershipAsync(string membershipId)
        {
            using (TH.BeginScope("Executing DELETE /memberships"))
            {
                await EnqueueClientTask(() => Client.DeleteMembershipAsync(membershipId)).ConfigureAwait(false);
            }
        }

        public async Task<ChannelViewModel> GetChannelAsync(string channelId)
        {
            Channel r;
            using (TH.BeginScope("Executing GET /channels/{id}"))
            {
                r = await EnqueueClientTask(() => Client.GetChannelAsync(channelId)).ConfigureAwait(false);
            }
            return GetOrCreateChannelViewModel(r);
        }

        public async Task<ChannelViewModel[]> GetChannelsAsync(bool? archived = null)
        {
            Channel[] r;
            using (TH.BeginScope("Executing GET /channels"))
            {
                r = await EnqueueClientTask(() => Client.GetChannelsAsync(archived: archived)).ConfigureAwait(false);
            }
            return r.Select(c => GetOrCreateChannelViewModel(c)).ToArray();
        }

        public async Task<Channel> GetChannelMembershipsAsync(string channelId)
        {
            Channel r;
            using (TH.BeginScope("Executing GET /channels/{id}/memberships"))
            {
                r = await EnqueueClientTask(() => Client.GetChannelMembershipsAsync(channelId)).ConfigureAwait(false);
            }
            GetChannelViewModel(r.Id)?.Update(r);

            foreach (var ms in r.Memberships)
            {
                GetProfileViewModel(ms.Profile);
            }

            return r;
        }

        public async Task<Message[]> GetMessagesAsync(string channelId, int? limit = null, int? beforeId = null, int? afterId = null)
        {
            Message[] r;
            using (TH.BeginScope("Executing GET /channels/{id}/messages"))
            {
                r = await EnqueueClientTask(() => Client.GetMessagesAsync(channelId, limit: limit, beforeId: beforeId, afterId: afterId)).ConfigureAwait(false);
            }

            foreach (var ms in r)
            {
                GetProfileViewModel(ms.Profile);
            }

            return r;
        }

        public async Task<Message> PostMessageAsync(string channelId, string message, bool isNsfw, Guid idempotentKey = default(Guid))
        {
            Message r;
            using (TH.BeginScope("Executing POST /channels/{id}/messages"))
            {
                r = await EnqueueClientTask(() => Client.PostMessageAsync(channelId, message, isNsfw, idempotentKey)).ConfigureAwait(false);
            }

            GetProfileViewModel(r.Profile);

            return r;
        }

        public async Task<ChannelViewModel> PostDirectMessageChannelAsync(string targetUserProfileId)
        {
            Channel channel;
            using (TH.BeginScope("Executing POST /channels/direct_message"))
            {
                channel = await EnqueueClientTask(() => Client.PostDirectMessageChannelAsync(targetUserProfileId));
            }
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

            try
            {
                string channelId;
                using (var realm = await RealmServices.GetInstanceAsync())
                {
                    var up = realm.All<ChannelUserProperties>().OrderByDescending(r => r.LastVisited).FirstOrDefault();

                    if (up != null)
                    {
                        if (up.UserId != _LoginUser.Id)
                        {
                            using (var trx = realm.BeginWrite())
                            {
                                realm.RemoveAll<ChannelUserProperties>();
                                trx.Commit();
                            }
                            channelId = _SelectedChannelId;
                        }
                        else
                        {
                            channelId = _SelectedChannelId ?? up.ChannelId;
                        }
                    }
                    else
                    {
                        channelId = _SelectedChannelId;
                    }
                }

                SelectedChannel = _Channels.FirstOrDefault(c => c.Id == channelId);
                _SelectedChannelId = null;
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
                    }

                    _SelectedChannel = value;

                    OnPropertyChanged();

                    if (_SelectedChannel != null)
                    {
                        _SelectedChannel.IsSelected = true;
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

        private string _SelectedChannelId;

        internal string SelectedChannelId
        {
            get => _SelectedChannel?.Id ?? _SelectedChannelId;
            set
            {
                var t = LoadInitialDataTask;
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    SelectedChannel = _Channels.FirstOrDefault(c => c.Id == value);
                    _SelectedChannelId = null;
                }
                else
                {
                    _SelectedChannelId = value;
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

        private async Task GetLastReadIdAsync()
        {
            if (_Channels?.Any(c => c.LastReadId == null) != true)
            {
                return;
            }

            using (TH.BeginScope("Reading LastReadId of Channels"))
            using (var realm = await RealmServices.GetInstanceAsync())
            {
                var d = realm.All<ChannelUserProperties>().ToDictionary(s => s.ChannelId, s => s.LastReadId);

                foreach (var c in _Channels)
                {
                    d.TryGetValue(c.Id, out var lid);
                    c._LastReadId = lid != null ? Math.Max(lid.Value, c.LastReadId ?? 0) : (c.LastReadId ?? 0);
                }
            }
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
                var ds = SH.Device;
                var ns = SH.Notification;
                ns.UnregisterPlatformNotificationService();

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

        #region ChannelListCommand

        private Command _ChannelListCommand;

        public Command ChannelListCommand
            => _ChannelListCommand ?? (_ChannelListCommand = new Command(async (p) =>
            {
                await App.Current.MainPage.Navigation.PushModalAsync(new ChannelListPage()
                {
                    BindingContext = new ChannelListViewModel(this)
                });
            }));

        #endregion ChannelListCommand

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
                var mp = SelectedChannel.MessagesPage;

                if (u.Scheme == "https"
                    && u.Host == "kokoro.io")
                {
                    if (u.AbsolutePath.StartsWith("/@"))
                    {
                        if (mp != null)
                        {
                            mp.SelectProfile(u.AbsolutePath.Substring(2), u.Fragment?.Length == 10 ? u.Fragment.Substring(1) : null);

                            return;
                        }
                    }
                    else if (u.AbsolutePath.StartsWith("/profiles/"))
                    {
                        if (mp != null)
                        {
                            mp.SelectProfile(null, u.AbsolutePath.Substring("/profiles/".Length));

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

                if (mp?.OpenUrl(u) == true)
                {
                    return;
                }

                XDevice.OpenUri(u);
            }
        }

        #region Connection

        /// <summary>
        /// A value indicating whether the application enabled the Connection.
        /// </summary>
        private bool _IsWebSocketEnabled;

        private Task _ConnectTask;

        private int _ConnectionVersion;

        private string[] _SubscribingChannels;

        private Task _RecconectTask;

        private bool _IsDisconnected = true;

        public bool IsDisconnected => _IsDisconnected;

        private bool UpdateIsDisconnected()
        {
            var v = _ConnectTask == null
            || Client.State == ClientState.Disconnected
            || (Client.State == ClientState.Connected
                    && Client.LastPingAt < DateTime.Now.AddSeconds(-10));

            if (_IsDisconnected != v)
            {
                _IsDisconnected = v;
                OnPropertyChanged(nameof(IsDisconnected));
            }

            return v;
        }

        public Task ConnectAsync()
        {
            _IsWebSocketEnabled = true;

            UpdateIsDisconnected();

            if (_ConnectTask != null
                && (Client.State == ClientState.Connecting
                    || (Client.State == ClientState.Connected && !(Client.LastPingAt < DateTime.Now.AddSeconds(-10)))))
            {
                return _ConnectTask;
            }

            _ConnectTask = ConnectAsyncCore();
            _ConnectTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ReconnectAsync().ConfigureAwait(false).GetHashCode();
                }
            });
            return _ConnectTask;
        }

        private async Task ConnectAsyncCore()
        {
            UpdateIsDisconnected();

            try
            {
                Client?.Dispose();
                await Client.ConnectAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                UpdateIsDisconnected();

                var c = SH.GetClient();
                c.AccessToken = Client.AccessToken;
                c.EndPoint = Client.EndPoint;
                c.WebSocketEndPoint = Client.WebSocketEndPoint;

                Client = c;

                UpdateIsDisconnected();

                await Client.ConnectAsync().ConfigureAwait(false);
            }

            await SubscribeAsync().ConfigureAwait(false);

            var v = ++_ConnectionVersion;
            XDevice.BeginInvokeOnMainThread(() =>
            {
                XDevice.StartTimer(TimeSpan.FromSeconds(1), () =>
                {
                    UpdateIsDisconnected();

                    if (v != _ConnectionVersion)
                    {
                        return false;
                    }

                    if (Client.State == ClientState.Connected
                        && Client.LastPingAt < DateTime.Now.AddSeconds(-10))
                    {
                        ReconnectAsync().ConfigureAwait(false).GetHashCode();
                        return false;
                    }

                    return true;
                });
            });
        }

        private async Task SubscribeAsync()
        {
            UpdateIsDisconnected();

            if (Client.State != ClientState.Connected)
            {
                return;
            }
            var current = _SubscribingChannels;
            _SubscribingChannels = Channels.Where(c => !c.IsArchived).Select(r => r.Id).ToArray();

            UpdateIsDisconnected();

            if (current?.SequenceEqual(_SubscribingChannels) != true)
            {
                using (TH.BeginScope("Subscribing channels"))
                {
                    await Client.SubscribeAsync(_SubscribingChannels).ConfigureAwait(false);
                }
            }
        }

        private Task ReconnectAsync()
            => _RecconectTask ?? (_RecconectTask = ReconnectAsyncCore());

        private async Task ReconnectAsyncCore()
        {
            UpdateIsDisconnected();

            // TODO: change wait
            await Task.Delay(5000);

            UpdateIsDisconnected();

            if (_IsWebSocketEnabled)
            {
                await ConnectAsync().ConfigureAwait(false);

                UpdateIsDisconnected();
            }
        }

        public async Task CloseAsync()
        {
            UpdateIsDisconnected();

            _IsWebSocketEnabled = false;
            _ConnectTask = null;
            _ConnectionVersion++;
            _SubscribingChannels = null;
            _RecconectTask = null;
            await Client.CloseAsync().ConfigureAwait(false);

            UpdateIsDisconnected();
        }

        private Command _ConnectCommand;

        public Command ConnectCommand
            => _ConnectCommand ?? (_ConnectCommand = new Command(async () =>
            {
                if (UpdateIsDisconnected())
                {
                    try
                    {
                        await ConnectAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ex.Trace("Connection failed");
                    }
                }
            }));

        #region Client Events

        private void Client_ProfileUpdated(object sender, EventArgs<Profile> e)
        {
            UpdateIsDisconnected();

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
            UpdateIsDisconnected();

            ReceiveMessage(e.Data, true);
        }

        private void Client_MessageUpdated(object sender, EventArgs<Message> e)
        {
            UpdateIsDisconnected();

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
            UpdateIsDisconnected();

            if (e.Data == null)
            {
                return;
            }

            Debug.WriteLine("Channels updated: {0} channels", e.Data.Length);

            XDevice.BeginInvokeOnMainThread(async () =>
            {
                foreach (var c in e.Data)
                {
                    GetOrCreateJoinedChannelViewModel(c);
                }

                Channels.RemoveRange(Channels.Where(c => !e.Data.Any(d => d.Id == c.Id)).ToArray());

                await GetLastReadIdAsync().ConfigureAwait(false);

                await SubscribeAsync().ConfigureAwait(false);
            });
        }

        private void Client_MemberJoined(object sender, EventArgs<Membership> e)
        {
            UpdateIsDisconnected();

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
            UpdateIsDisconnected();

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
            _ConnectTask = null;
            _ConnectionVersion++;
            _SubscribingChannels = null;
            _RecconectTask = null;

            UpdateIsDisconnected();

            TH.TraceError("Disconnected");

            ReconnectAsync().ConfigureAwait(false).GetHashCode();
        }

        #endregion Client Events

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
                    parameter.OnUploading?.Invoke();
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

                    parameter.OnUploading?.Invoke();
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

        #region Uploaders

        private static IImageUploader[] _Uploaders;

        internal static IImageUploader[] Uploaders
            => _Uploaders ?? (_Uploaders = new IImageUploader[] { new GyazoImageUploader(), new ImgurImageUploader() });

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

        #endregion Uploaders

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

        internal static void ReceiveNotification(Message message)
        {
            var avm = App.Current?.MainPage?.BindingContext as ApplicationViewModel;

            if (avm == null)
            {
                SH.Notification.ShowNotificationAndSave(message);
            }
            else
            {
                avm.ReceiveMessage(message, false);
            }
        }

        private void ReceiveMessage(Message message, bool updateProfile)
        {
            if (message == null)
            {
                return;
            }
            Debug.WriteLine("Message created: {0}", message.Id);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                if (updateProfile)
                {
                    GetProfileViewModel(message.Profile);
                }

                _Channels?.FirstOrDefault(r => r.Id == message.Channel.Id)?.Receive(message);
            });
        }
    }
}