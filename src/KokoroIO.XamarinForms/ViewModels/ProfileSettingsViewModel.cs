using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using XLabs.Platform.Services.Media;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ProfileSettingsViewModel : BaseViewModel
    {
        public ProfileSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Profile";
            Settings = settings;

            _ScreenName = settings.Application.LoginUser.ScreenName;
            _DisplayName = settings.Application.LoginUser.DisplayName;
        }

        internal SettingsViewModel Settings { get; }
        public ApplicationViewModel Application => Settings.Application;

        #region ScreenName

        private string _ScreenName;

        public string ScreenName
        {
            get => _ScreenName;
            set => SetProperty(ref _ScreenName, value);
        }

        #endregion ScreenName

        #region DisplayName

        public string _DisplayName;

        public string DisplayName
        {
            get => _DisplayName;
            set => SetProperty(ref _DisplayName, value);
        }

        #endregion DisplayName

        #region SaveCommand

        private Command _SaveCommand;

        public Command SaveCommand
            => _SaveCommand ?? (_SaveCommand = new Command(() => SaveAsync().GetHashCode()));

        private async Task SaveAsync(Stream avatar = null)
        {
            var sn = _ScreenName;
            var dn = _DisplayName;

            if (IsBusy
                || string.IsNullOrEmpty(sn)
                || string.IsNullOrEmpty(dn)
                || (sn == Application.LoginUser.ScreenName
                    && dn == Application.LoginUser.DisplayName
                    && avatar == null))
            {
                return;
            }

            IsBusy = true;

            try
            {
                var p = await Application.PutProfileAsync(sn, dn, avatar);

                ScreenName = p.ScreenName;
                DisplayName = p.DisplayName;
            }
            catch (Exception ex)
            {
                ex.Error("UpdateProfileFailed");

                MessagingCenter.Send(this, "UpdateProfileFailed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion SaveCommand

        #region SelectPhotoCommand

        private Command _SelectPhotoCommand;

        public Command SelectPhotoCommand
            => _SelectPhotoCommand ?? (_SelectPhotoCommand = new Command(BeginSelectPhoto));

        private async void BeginSelectPhoto()
        {
            try
            {
                var mp = Application.MediaPicker;

                if (mp == null || !mp.IsPhotosSupported)
                {
                    return;
                }

                var mf = await mp.SelectPhotoAsync(new CameraMediaStorageOptions());

                if (mf != null)
                {
                    using (mf.Source)
                    {
                        await SaveAsync(mf.Source);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Error("UpdateProfileFailed");

                MessagingCenter.Send(this, "UpdateProfileFailed");
            }
        }

        #endregion SelectPhotoCommand

        #region TakePhotoCommand

        private Command _TakePhotoCommand;

        public Command TakePhotoCommand
            => _TakePhotoCommand ?? (_TakePhotoCommand = new Command(BeginTakePhoto));

        private async void BeginTakePhoto()
        {
            try
            {
                var mp = Application.MediaPicker;

                if (mp == null)
                {
                    return;
                }

                var mf = await mp.TakePhotoAsync(new CameraMediaStorageOptions());

                if (mf != null || !mp.IsCameraAvailable)
                {
                    using (mf.Source)
                    {
                        await SaveAsync(mf.Source);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Error("UpdateProfileFailed");

                MessagingCenter.Send(this, "UpdateProfileFailed");
            }
        }

        #endregion TakePhotoCommand
    }
}