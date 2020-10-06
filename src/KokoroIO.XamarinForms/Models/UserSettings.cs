using System;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;

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

        public static bool EnablePushNotification
        {
            get => GetBoolean();
            set => SetValue(value);
        }

        public static string LastChannelId
        {
            get => GetString();
            set => SetValue(value);
        }

        public static bool PlayRingtone
        {
#if !MODEL_TESTS
            get => GetBoolean(defaultValue: Xamarin.Forms.Device.Idiom == Xamarin.Forms.TargetIdiom.Desktop);
#else
            get => GetBoolean()  ;
#endif
            set => SetValue(value);
        }

        public static bool MobileCenterAnalyticsEnabled
        {
            get => GetBoolean();
            set => SetValue(value);
        }

        public static bool MobileCenterCrashesEnabled
        {
            get => GetBoolean();
            set => SetValue(value);
        }

        public static bool MobileCenterDistributeEnabled
        {
            get => GetBoolean();
            set => SetValue(value);
        }


#if !MODEL_TESTS

        private static bool GetBoolean([CallerMemberName] string property = null, bool defaultValue = true)
            => Preferences.Get(property, defaultValue);

        private static string GetString([CallerMemberName] string property = null)
            => Preferences.Get(property, null);

        private static void SetValue(bool value, [CallerMemberName] string property = null)
            => Preferences.Set(property, value);

        private static void SetValue(string value, [CallerMemberName] string property = null)
            => Preferences.Set(property, value);

#else
        private static bool? GetBoolean([CallerMemberName]string property = null)
        {
            throw new NotSupportedException();
        }

        private static string GetString([CallerMemberName]string property = null)
        {
            throw new NotSupportedException();
        }

        private static void SetValue(object value, [CallerMemberName]string property = null)
        {
            throw new NotSupportedException();
        }
#endif
    }
}