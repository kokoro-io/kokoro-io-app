using KokoroIO.XamarinForms.Models;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class NotificationSettingsViewModel : BaseViewModel
    {
        public NotificationSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Notification";
            Settings = settings;

            _PlayRingtone = UserSettings.PlayRingtone;
        }

        internal SettingsViewModel Settings { get; }
        public ApplicationViewModel Application => Settings.Application;

        private bool _PlayRingtone;

        public bool PlayRingtone
        {
            get => _PlayRingtone;
            set
            {
                if (_PlayRingtone != value)
                {
                    _PlayRingtone = value;
                    UserSettings.PlayRingtone = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}