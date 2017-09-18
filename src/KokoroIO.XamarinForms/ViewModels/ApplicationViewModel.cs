using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Views;
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
        }

        internal Client Client { get; }

        #region Rooms

        private bool _RoomsLoaded;

        private ObservableRangeCollection<RoomViewModel> _Rooms;

        public ObservableRangeCollection<RoomViewModel> Rooms
            => InitRooms()._Rooms;

        private ApplicationViewModel InitRooms()
        {
            if (_Rooms == null)
            {
                _Rooms = new ObservableRangeCollection<RoomViewModel>();
            }

            if (!_RoomsLoaded)
            {
                _RoomsLoaded = true;
                LoadRooms();
            }

            return this;
        }

        private async void LoadRooms()
        {
            var rooms = await Client.GetRoomsAsync();
            foreach (var r in rooms.OrderBy(e => e.IsArchived ? 1 : 0)
                                    .ThenBy(e => (int)e.Kind)
                                    .ThenBy(e => e.ChannelName, StringComparer.Ordinal)
                                    .ThenBy(e => e.Id))
            {
                var rvm = new RoomViewModel(this, r);

                _Rooms.Add(rvm);
            }

            if (rooms.Any())
            {
                try
                {
                    await Client.ConnectAsync();
                    await Client.SubscribeAsync(rooms);
                }
                catch { }
            }
        }

        private RoomViewModel _SelectedRoom;

        public RoomViewModel SelectedRoom
        {
            get => _SelectedRoom;
            set => SetProperty(ref _SelectedRoom, value, onChanged: () => OnUnreadCountChanged());
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
            var key = model.Profile.Id + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model.Profile.Id, model.Avatar, model.DisplayName);
                _Profiles[key] = p;
            }
            return p;
        }

        internal ProfileViewModel GetProfile(Profile model)
        {
            var key = model.Id + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model);
                _Profiles[key] = p;
            }
            return p;
        }

        #endregion Profiles

        #region LogoutCommand

        private Command _LogoutCommand;

        public Command LogoutCommand
            => _LogoutCommand ?? (_LogoutCommand = new Command(BeginLogout));

        public async void BeginLogout()
        {
            try
            {
                await CloseAsync();
            }
            catch { }

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

        public async Task ConnectAsync()
        {
            // TODO: disable push notification

            await Client.ConnectAsync();
            await Client.SubscribeAsync(Rooms.Select(r => r.Id));
        }

        public async Task CloseAsync()
        {
            await Client.CloseAsync();

            // TODO: enable push notification
        }

        private void Client_ProfileUpdated(object sender, EventArgs<Profile> e)
        {
            if (e.Data?.Id == _LoginUser.Id)
            {
                LoginUser = GetProfile(e.Data);
            }
        }

        private void Client_MessageCreated(object sender, EventArgs<Message> e)
        {
            var rvm = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id);
            if (rvm != null)
            {
                rvm.UnreadCount++;
                try
                {
                    DependencyService.Get<IAudioService>()?.PlayNotification();
                }
                catch { }
            }
        }

        private void Client_MessageUpdated(object sender, EventArgs<Message> e)
        {
            var mp = _Rooms?.FirstOrDefault(r => r.Id == e.Data.Room.Id)?.MessagesPage;
            if (mp != null)
            {
                mp.UpdateMessage(e.Data);
            }
        }

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
                    var mf = await mp.SelectPhotoAsync(new CameraMediaStorageOptions() { });

                    using (var ms = mf.Source)
                    {
                        url = await uploader.UploadAsync(ms, Path.GetFileName(mf.Path));
                    }
                }

                SetSelectedUploader(uploader);

                await App.Current.SavePropertiesAsync();

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
    }
}