using Xamarin.Forms;

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

        private string _ScreenName;

        public string ScreenName
        {
            get => _ScreenName;
            set => SetProperty(ref _ScreenName, value);
        }

        public string _DisplayName;

        public string DisplayName
        {
            get => _DisplayName;
            set => SetProperty(ref _DisplayName, value);
        }

        private Command _SaveCommand;

        public Command SaveCommand
            => _SaveCommand ?? (_SaveCommand = new Command(BeginSave));

        private async void BeginSave()
        {
            var sn = _ScreenName;
            var dn = _DisplayName;

            if (IsBusy
                || string.IsNullOrEmpty(sn)
                || string.IsNullOrEmpty(dn)
                || (sn == Application.LoginUser.ScreenName && dn == Application.LoginUser.DisplayName))
            {
                return;
            }

            IsBusy = true;

            try
            {
                var p = await Application.PutProfileAsync(sn, dn);

                ScreenName = p.ScreenName;
                DisplayName = p.DisplayName;
            }
            catch
            {
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}