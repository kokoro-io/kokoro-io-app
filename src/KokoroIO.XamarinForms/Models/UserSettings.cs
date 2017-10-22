using System.Runtime.CompilerServices;

namespace KokoroIO.XamarinForms.Models
{
    internal sealed class UserSettings
    {
        public static string MailAddress
        {
            get => GetString();
            set => SetValue(value);
        }

        public static string Password
        {
            get => GetString();
            set => SetValue(value);
        }

        public static string EndPoint
        {
            get => GetString();
            set => SetValue(value);
        }

        public static string AccessToken
        {
            get => GetString();
            set => SetValue(value);
        }

        public static string PnsHandle
        {
            get => GetString();
            set => SetValue(value);
        }

        public static bool PlayRingtone
        {
            get
            {
                if (App.Current.Properties.TryGetValue(nameof(PlayRingtone), out var obj)
                    && obj is bool b)
                {
                    return b;
                }
                return Xamarin.Forms.Device.Idiom == Xamarin.Forms.TargetIdiom.Desktop;
            }
            set => SetValue(value);
        }

        public static bool MobileCenterAnalyticsEnabled
        {
            get => GetBoolean() ?? true;
            set => SetValue(value);
        }
        
        public static bool MobileCenterCrashesEnabled
        {
            get => GetBoolean() ?? true;
            set => SetValue(value);
        }

        public static bool MobileCenterDistributeEnabled
        {
            get => GetBoolean() ?? true;
            set => SetValue(value);
        }

        private static bool? GetBoolean([CallerMemberName]string property = null)
            => App.Current.Properties.TryGetValue(property, out var obj)
                ? obj is bool b ? b
                    : obj is string s ? (bool.TryParse(s, out b) ? b : (bool?)null)
                    : (obj is System.IConvertible c ? c.ToDouble(null) != 0 : (bool?)null)
                : null;

        private static string GetString([CallerMemberName]string property = null)
            => App.Current.Properties.TryGetValue(property, out var obj)
                ? obj?.ToString() : null;

        private static void SetValue(object value, [CallerMemberName]string property = null)
        {
            if (value == null)
            {
                App.Current.Properties.Remove(property);
            }
            else
            {
                App.Current.Properties[property] = value;
            }
        }
    }
}