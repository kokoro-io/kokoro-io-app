using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Views;
using Realms;
using Shipwreck.KokoroIO;
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

        internal Client Client { get; }

        #region Rooms

        private Task _LoadInitialDataTask;

        private ObservableRangeCollection<RoomViewModel> _Rooms;

        public ObservableRangeCollection<RoomViewModel> Rooms
        {
            get
            {
                if (_Rooms == null)
                {
                    _Rooms = new ObservableRangeCollection<RoomViewModel>();
                }
                LoadInitialDataTask.GetHashCode();

                return _Rooms;
            }
        }

        internal Task LoadInitialDataTask
            => _LoadInitialDataTask ?? (_LoadInitialDataTask = LoadInitialDataAsync());

        private async Task LoadInitialDataAsync()
        {
            if (_Rooms == null)
            {
                _Rooms = new ObservableRangeCollection<RoomViewModel>();
            }

            var rooms = await Client.GetRoomsAsync().ConfigureAwait(false);

            foreach (var r in rooms.OrderBy(e => e.IsArchived ? 1 : 0)
                                    .ThenBy(e => (int)e.Kind)
                                    .ThenBy(e => e.ChannelName, StringComparer.Ordinal)
                                    .ThenBy(e => e.Id))
            {
                var rvm = new RoomViewModel(this, r);

                _Rooms.Add(rvm);
            }

            try
            {
                string roomId;
                using (var realm = Realm.GetInstance())
                {
                    var rup = realm.All<RoomUserProperties>().OrderByDescending(r => r.LastVisited).FirstOrDefault();

                    if (rup == null)
                    {
                        return;
                    }
                    else if (rup.UserId != _LoginUser.Id)
                    {
                        using (var trx = realm.BeginWrite())
                        {
                            realm.RemoveAll<RoomUserProperties>();
                            trx.Commit();
                        }
                        return;
                    }
                    else
                    {
                        roomId = rup.RoomId;
                    }
                }

                var rvm = _Rooms.FirstOrDefault(r => r.Id == roomId);
                if (rvm != null)
                {
                    SelectedRoom = rvm;
                }
            }
            catch (Exception ex)
            {
                ex.Trace("LoadingRoomUserPropertiesFailed");
            }
        }

        private RoomViewModel _SelectedRoom;

        public RoomViewModel SelectedRoom
        {
            get => _SelectedRoom;
            set
            {
                if (value != _SelectedRoom)
                {
                    if (_SelectedRoom != null)
                    {
                        _SelectedRoom.IsSelected = false;

                        var mp = _SelectedRoom.MessagesPage;

                        if (mp != null)
                        {
                            mp.IsSubscribing = false;
                        }
                    }

                    _SelectedRoom = value;

                    OnPropertyChanged();

                    if (_SelectedRoom != null)
                    {
                        _SelectedRoom.IsSelected = true;
                        _SelectedRoom.GetOrCreateMessagesPage().IsSubscribing = true;
                    }
                    OnUnreadCountChanged();
                    if (Client.State == ClientState.Disconnected
                        && _Rooms.Any())
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
            HasNotificationInMenu = _Rooms?.Where(r => r != _SelectedRoom).Sum(r => r.UnreadCount) > 0;
        }

        #endregion Rooms

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
                p = new ProfileViewModel(model.Profile.Id, model.Avatar, model.DisplayName, model.Profile.ScreenName);
                _Profiles[key] = p;
            }
            return p;
        }

        internal ProfileViewModel GetProfile(Profile model)
        {
            var key = model.Id + '\0' + model.ScreenName + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model);
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

            App.Current.Properties.Remove(nameof(LoginViewModel.Password));
            App.Current.Properties.Remove(nameof(AccessToken));
            await App.Current.SavePropertiesAsync();

            App.Current.MainPage = new LoginPage();
        }

        #endregion LogoutCommand

        #region SettingCommand

        private Command _SettingCommand;

        public Command SettingsCommand
            => _SettingCommand ?? (_SettingCommand = new Command(async () =>
            {
                await App.Current.MainPage.Navigation.PushModalAsync(new SettingsPage(new SettingsViewModel(this)));
            }));

        #endregion SettingCommand

        #region SettingCommand

        private static Command _PopToRootCommand;

        public static Command PopToRootCommand
            => _PopToRootCommand ?? (_PopToRootCommand = new Command(async () =>
            {
                var nav = App.Current.MainPage.Navigation;

                while (nav.ModalStack.Any())
                {
                    await nav.PopModalAsync();
                }
            }));

        #endregion SettingCommand

        public Command OpenUrlCommand { get; }

        private void OpenUrl(object url)
        {
            var u = url as Uri ?? (url is string s ? new Uri(s) : null);

            if (u != null)
            {
                XDevice.OpenUri(u);
            }
        }

        #region Connection

        public async Task ConnectAsync()
        {
            if (Client.State == ClientState.Disconnected)
            {
                await Client.ConnectAsync();
                await Client.SubscribeAsync(Rooms.Select(r => r.Id));

                OnPropertyChanged(nameof(IsDisconnected));
            }
        }

        public async Task CloseAsync()
        {
            await Client.CloseAsync();
        }

        public bool IsDisconnected
            => Rooms.Any(r => !r.IsArchived)
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
            var rvm = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id);

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
            var mp = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id)?.MessagesPage;

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

        public static async void OpenFile(Func<Stream> streamCreator)
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

                await nav.PushModalAsync(new UploadToRoomPage()
                {
                    BindingContext = new UploadToRoomViewModel(this, sc)
                });
            }
        }

        #endregion Document Interaction
    }
}