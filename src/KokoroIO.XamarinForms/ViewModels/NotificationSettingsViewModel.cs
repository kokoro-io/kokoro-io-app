using KokoroIO.XamarinForms.Models;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class NotificationSettingsViewModel : BaseViewModel
    {
        public NotificationSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Notification";
            Settings = settings;

            _EnablePushNotification = UserSettings.EnablePushNotification;
            _PlayRingtone = UserSettings.PlayRingtone;
            _MobileCenterAnalyticsEnabled = UserSettings.MobileCenterAnalyticsEnabled;
            _MobileCenterCrashesEnabled = UserSettings.MobileCenterCrashesEnabled;
            _MobileCenterDistributeEnabled = UserSettings.MobileCenterDistributeEnabled;
        }

        internal SettingsViewModel Settings { get; }
        public ApplicationViewModel Application => Settings.Application;

        #region EnablePushNotification

        private bool _EnablePushNotification;

        public bool EnablePushNotification
        {
            get => _EnablePushNotification;
            set
            {
                if (_EnablePushNotification != value)
                {
                    _EnablePushNotification = value;
                    UserSettings.EnablePushNotification = value;
                    OnPropertyChanged();

#pragma warning disable 4014
                    Application.SetSubscribeNotificationAsync(value);
#pragma warning restore 4014
                }
            }
        }

        #endregion EnablePushNotification

        #region PlayRingtone

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

        #endregion PlayRingtone

        #region MobileCenterAnalyticsEnabled

        private bool _MobileCenterAnalyticsEnabled;

        public bool MobileCenterAnalyticsEnabled
        {
            get => _MobileCenterAnalyticsEnabled;
            set
            {
                if (_MobileCenterAnalyticsEnabled != value)
                {
                    _MobileCenterAnalyticsEnabled = value;
                    UserSettings.MobileCenterAnalyticsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion MobileCenterAnalyticsEnabled

        #region MobileCenterCrashesEnabled

        private bool _MobileCenterCrashesEnabled;

        public bool MobileCenterCrashesEnabled
        {
            get => _MobileCenterCrashesEnabled;
            set
            {
                if (_MobileCenterCrashesEnabled != value)
                {
                    _MobileCenterCrashesEnabled = value;
                    UserSettings.MobileCenterCrashesEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion MobileCenterCrashesEnabled

        #region MobileCenterDistributeEnabled

        private bool _MobileCenterDistributeEnabled;

        public bool MobileCenterDistributeEnabled
        {
            get => _MobileCenterDistributeEnabled;
            set
            {
                if (_MobileCenterDistributeEnabled != value)
                {
                    _MobileCenterDistributeEnabled = value;
                    UserSettings.MobileCenterDistributeEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion MobileCenterDistributeEnabled
    }
}