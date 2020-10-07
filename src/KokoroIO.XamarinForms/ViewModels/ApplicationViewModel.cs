using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.Views;
using Xamarin.Essentials;
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
                    _Client.ChannelArchived -= _Client_ChannelUpdated;
                    _Client.ChannelUnarchived -= _Client_ChannelUpdated;
                    _Client.MemberJoined -= Client_MemberJoined;
                    _Client.MemberLeaved -= Client_MemberLeaved;
                    _Client.SocketError -= _Client_SocketError;

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
                    _Client.ChannelArchived += _Client_ChannelUpdated;
                    _Client.ChannelUnarchived += _Client_ChannelUpdated;
                    _Client.MemberJoined += Client_MemberJoined;
                    _Client.MemberLeaved += Client_MemberLeaved;
                    _Client.SocketError += _Client_SocketError;

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

            EnqueueClientTaskCore(() =>
            {
                var tc = task();
                tc.ConfigureAwait(false);
                return tc.ContinueWith(t =>
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
                });
            });

            return tcs.Task;
        }

        private Task<T> EnqueueClientTask<T>(Func<Task<T>> task)
        {
            var tcs = new TaskCompletionSource<T>();

            EnqueueClientTaskCore(() =>
            {
                var tc = task();
                tc.ConfigureAwait(false);
                return tc.ContinueWith(t =>
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
                });
            });

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
                r = await EnqueueClientTask(() => Client.GetMembershipsAsync(archived: archived, authority: authority));
            }

            foreach (var ms in r)
            {
                GetProfileViewModel(ms.Profile);
                GetOrCreateJoinedChannelViewModel(ms);
            }

            await SubscribeAsync().ConfigureAwait(false);

            return r;
        }

        public async Task<ChannelViewModel> PostMembershipAsync(string channelId, bool disableNotification = false)
        {
            Membership ms;
            using (TH.BeginScope("Executing POST /memberships"))
            {
                ms = await EnqueueClientTask(() => Client.PostMembershipAsync(channelId, disableNotification == true ? NotificationPolicy.Nothing : NotificationPolicy.AllMessages));
            }
            return GetOrCreateJoinedChannelViewModel(ms);
        }

        public async Task<ChannelViewModel> PutMembershipAsync(string membershipId, bool? disableNotification = null, int? latestReadMessageId = null)
        {
            // TODO: fix client
            Membership ms;
            using (TH.BeginScope("Executing PUT /memberships"))
            {
                ms = await EnqueueClientTask(
                                () => Client.PutMembershipAsync(
                                                membershipId,
                                                disableNotification == true ? NotificationPolicy.Nothing
                                                                            : NotificationPolicy.AllMessages,
                                                latestReadMessageId: latestReadMessageId));
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

        public async Task<Message> PostMessageAsync(string channelId, string message, bool isNsfw, bool expandEmbedContents = true, Guid idempotentKey = default(Guid))
        {
            Message r;
            using (TH.BeginScope("Executing POST /channels/{id}/messages"))
            {
                r = await EnqueueClientTask(() => Client.PostMessageAsync(channelId, message, isNsfw, expandEmbedContents: expandEmbedContents, idempotentKey: idempotentKey)).ConfigureAwait(false);
            }

            GetProfileViewModel(r.Profile);

            return r;
        }

        public async Task DeleteMessageAsync(int messageId)
        {
            using (TH.BeginScope("Executing DELETE /messages"))
            {
                await EnqueueClientTask(() => Client.DeleteMessageAsync(messageId)).ConfigureAwait(false);
            }
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

        public async Task SetSubscribeNotificationAsync(bool value)
        {
            var ds = SH.Device;

            if (UserSettings.EnablePushNotification)
            {
                var pnsTask = SH.Notification.GetPlatformNotificationServiceHandleAsync();

                await Task.WhenAny(pnsTask, Task.Delay(15000));

                var pns = pnsTask.Status == TaskStatus.RanToCompletion ? pnsTask.Result : UserSettings.PnsHandle;
                await Client.PostDeviceAsync(ds.MachineName, ds.Kind, ds.GetDeviceIdentifierString(), pns, pns != null);
                UserSettings.PnsHandle = pns;
            }
            else
            {
                SH.Notification.UnregisterPlatformNotificationService();
                await Client.PostDeviceAsync(ds.MachineName, ds.Kind, ds.GetDeviceIdentifierString(), null, false);
                UserSettings.PnsHandle = null;
            }
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
                using (var trx = realm.BeginWrite())
                {
                    var notifs = realm.All<MessageNotification>().ToList();

                    var nf = SH.Notification;

                    foreach (var cvm in _Channels)
                    {
                        var lastId = cvm.LatestReadMessageId;

                        var cnt = notifs.Count(n => n.ChannelId == cvm.Id && !(n.MessageId <= lastId));

                        for (var i = notifs.Count - 1; i >= 0; i--)
                        {
                            var n = notifs[i];
                            if (n.ChannelId == cvm.Id)
                            {
                                if (n.MessageId <= lastId)
                                {
                                    nf.CancelNotification(cvm.Id, n.NotificationId, cnt);
                                    realm.Remove(n);
                                    notifs.RemoveAt(i);
                                }
                            }
                        }
                    }

                    TH.Info("Notified channel: {0}", _SelectedChannelId ?? "n/a");
                    TH.Info("Last channel: {0}", UserSettings.LastChannelId ?? "n/a");

                    channelId = _SelectedChannelId ?? UserSettings.LastChannelId;
                    trx.Commit();
                }

                SelectedChannel = _Channels.FirstOrDefault(c => c.Id == channelId);
                _SelectedChannelId = null;
                TH.Info("Selected channel: {0}", SelectedChannelId ?? "n/a");
            }
            catch (Exception ex)
            {
                ex.Warn("LoadingChannelUserPropertiesFailed");
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
                        UserSettings.LastChannelId = _SelectedChannel.Id;
                        App.Current.SavePropertiesAsync();
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

        public string SelectedChannelId
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

            if (cvm != null)
            {
                InsertChannel(cvm);
            }

            return cvm;
        }

        internal ChannelViewModel GetOrCreateJoinedChannelViewModel(Membership membership)
        {
            if (membership?.Channel != null)
            {
                ChannelViewModel c = null;
                if (_ChannelDictionary == null)
                {
                    _ChannelDictionary = new Dictionary<string, WeakReference<ChannelViewModel>>();
                }
                else if (_ChannelDictionary.TryGetValue(membership.Channel.Id, out var w)
                        && w.TryGetTarget(out c))
                {
                    c.Update(membership);

                    InsertChannel(c);

                    return c;
                }

                c = new ChannelViewModel(this, membership);
                _ChannelDictionary[membership.Channel.Id] = new WeakReference<ChannelViewModel>(c);

                InsertChannel(c);

                return c;
            }

            return null;
        }

        private void InsertChannel(ChannelViewModel cvm)
        {
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
                        return;
                    }
                }

                Channels.Add(cvm);
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
                ex.Error("DeleteDeviceFailed");
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
                    else if (u.AbsolutePath == "/"
                            && u.Fragment.StartsWith("#/channels/"))
                    {
                        var id = u.Fragment.Substring("#/channels/".Length);

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

                await Launcher.TryOpenAsync(u);
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
                        ex.Error("Connection failed");
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
            TH.Info("Profile updated: {0}", e.Data.Id);

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

            TH.Info("Message updated: {0}", e.Data.Id);

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

            TH.Info("Channels updated: {0} channels", e.Data.Length);

            XDevice.BeginInvokeOnMainThread(async () =>
            {
                foreach (var c in e.Data)
                {
                    GetOrCreateJoinedChannelViewModel(c);
                }

                Channels.RemoveRange(Channels.Where(c => !e.Data.Any(d => d.Id == c.Id)).ToArray());

                await SubscribeAsync().ConfigureAwait(false);
            });
        }

        private void _Client_ChannelUpdated(object sender, EventArgs<Channel> e)
        {
            UpdateIsDisconnected();

            if (e.Data == null)
            {
                return;
            }

            TH.Info("Channel#{0} updated", e.Data.Id);

            XDevice.BeginInvokeOnMainThread(() => GetOrCreateChannelViewModel(e.Data));
        }

        private void Client_MemberJoined(object sender, EventArgs<Membership> e)
        {
            UpdateIsDisconnected();

            if (e.Data == null)
            {
                return;
            }
            TH.Info("Member joined: @{1} joined to {0} as {2}", e.Data.Channel.ChannelName, e.Data.Profile.ScreenName, e.Data.Authority);

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
            TH.Info("Member leaved: @{1} leaved from {0}", e.Data.Channel.ChannelName, e.Data.Profile.ScreenName);

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

            TH.Warn("Disconnected");

            ReconnectAsync().ConfigureAwait(false).GetHashCode();
        }

        private void _Client_SocketError(object sender, EventArgs<Exception> e)
            => TH.Warn("Handled WebSocket exception: {0}", e.Data);

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
                UploadedImageInfo img;
                string title = null;
                if (parameter.Data != null)
                {
                    parameter.OnUploading?.Invoke();
                    using (parameter.Data)
                    {
                        img = await uploader.UploadAsync(parameter.Data, "pasted");
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
                    title = Path.GetFileName(mf.Path);
                    using (var ms = mf.Source)
                    {
                        img = await uploader.UploadAsync(ms, title);
                    }
                }

                SetSelectedUploader(uploader);

                try
                {
                    await App.Current.SavePropertiesAsync();

                    using (var r = await RealmServices.GetInstanceAsync())
                    using (var c = r.BeginWrite())
                    {
                        var ih = r.Find<ImageHistory>(img.RawUrl);
                        if (ih == null)
                        {
                            ih = new ImageHistory()
                            {
                                RawUrl = img.RawUrl,
                                Title = title,
                                ThumbnailUrl = img.ThumbnailUrl
                            };
                            r.Add(ih);
                        }
                        ih.LastUsed = DateTimeOffset.Now;

                        c.Commit();
                    }
                }
                catch { }

                parameter.OnCompleted(img.RawUrl);
            }
            catch (Exception ex)
            {
                ex.Error("Image Uploader failed");

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

        public static void ReceiveNotification(Message message)
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
            TH.Info("Message created: {0}", message.Id);

            XDevice.BeginInvokeOnMainThread(() =>
            {
                if (updateProfile)
                {
                    if (message.Channel != null)
                    {
                        GetOrCreateChannelViewModel(message.Channel);
                    }
                    GetProfileViewModel(message.Profile);
                }

                _Channels?.FirstOrDefault(r => r.Id == message.Channel.Id)?.Receive(message);
            });
        }
    }
}