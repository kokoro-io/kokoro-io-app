using System;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel(ApplicationViewModel application)
        {
            Application = application;

            Profile = new ProfileSettingsViewModel(this);
            Notification = new NotificationSettingsViewModel(this);
            Uploader = new UploaderSettingsViewModel(this);
        }

        internal ApplicationViewModel Application { get; }

        public ProfileSettingsViewModel Profile { get; }
        public NotificationSettingsViewModel Notification { get; }
        public UploaderSettingsViewModel Uploader { get; }
        public Type InitialPageType { get; internal set; }
    }
}