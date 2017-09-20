namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ProfileSettingsViewModel : BaseViewModel
    {
        public ProfileSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Profile";
            Settings = settings;
        }

        internal SettingsViewModel Settings { get; }
        public ApplicationViewModel Application => Settings.Application;
    }
}