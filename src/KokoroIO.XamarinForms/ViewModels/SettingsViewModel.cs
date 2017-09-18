namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel(ApplicationViewModel application)
        {
            Application = application;

            Uploader = new UploaderSettingsViewModel(this);
        }

        internal ApplicationViewModel Application { get; }

        public UploaderSettingsViewModel Uploader { get; }
    }
}