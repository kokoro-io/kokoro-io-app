using KokoroIO.XamarinForms.Models.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;

namespace KokoroIO.XamarinForms.Models
{
    internal sealed class UserSettings
    {
        public static string DeviceId
        {
            get
            {
                var did = Preferences.Get(nameof(DeviceId), null);
                if (string.IsNullOrEmpty(did))
                {
                    Preferences.Set(nameof(DeviceId), did = Guid.NewGuid().ToString());
                }
                return did;
            }
        }

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
            get => GetBoolean(defaultValue: /* TODO: Push Notification */false);
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

        #region MessageNotifications

        private const string MESSAGE_NOTIFICATIONS = "MessageNotifications";

        public static List<MessageNotification> GetMessageNotifications()
        {
            try
            {
                var js = Preferences.Get(MESSAGE_NOTIFICATIONS, null);
                if (!string.IsNullOrEmpty(js))
                {
                    return JsonConvert.DeserializeObject<List<MessageNotification>>(js);
                }
            }
            catch { }
            return new List<MessageNotification>();
        }

        public static void AddMessageNotification(MessageNotification item)
            => Preferences.Set(
                MESSAGE_NOTIFICATIONS,
                JsonConvert.SerializeObject(GetMessageNotifications().Where(e => e.MessageId != item.MessageId).Concat(new[] { item }).ToList()));

        public static void SetMessageNotifications(IEnumerable<MessageNotification> items)
            => Preferences.Set(
                MESSAGE_NOTIFICATIONS,
                JsonConvert.SerializeObject(items.ToList()));

        #endregion MessageNotifications

        #region ImageHistories

        private const string IMAGE_HISTORIES = "ImageHistories";

        public static List<ImageHistory> GetImageHistories()
        {
            try
            {
                var js = Preferences.Get(IMAGE_HISTORIES, null);
                if (!string.IsNullOrEmpty(js))
                {
                    return JsonConvert.DeserializeObject<List<ImageHistory>>(js);
                }
            }
            catch { }
            return new List<ImageHistory>();
        }

        public static void AddImageHistory(ImageHistory item)
            => Preferences.Set(
                IMAGE_HISTORIES,
                JsonConvert.SerializeObject(GetImageHistories().Where(e => e.RawUrl != item.RawUrl).Concat(new[] { item }).ToList()));

        public static void SetImageHistories(IEnumerable<ImageHistory> items)
            => Preferences.Set(
                IMAGE_HISTORIES,
                JsonConvert.SerializeObject(items.ToList()));

        #endregion ImageHistories

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