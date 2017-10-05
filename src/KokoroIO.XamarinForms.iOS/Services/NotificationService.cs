using System.Threading.Tasks;
using Foundation;
using KokoroIO.XamarinForms.iOS.Services;
using KokoroIO.XamarinForms.Services;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(NotificationService))]

namespace KokoroIO.XamarinForms.iOS.Services
{
    public class NotificationService : INotificationService
    {
        public Task<string> GetPlatformNotificationServiceHandleAsync()
            => Task.FromResult<string>(null);

        public void UnregisterPlatformNotificationService()
        {
        }

        public string ShowNotification(Message message)
        {
            // TODO: use UNNotificationRequest instead

            var n = new UILocalNotification();
            n.FireDate = NSDate.Now;
            n.TimeZone = NSTimeZone.DefaultTimeZone;
            n.AlertTitle = "#" + message.Channel.ChannelName;
            n.AlertBody = $"{message.Profile.DisplayName}: {message.RawContent}";

            n.UserInfo = NSDictionary.FromObjectAndKey((NSString)message.Id.ToString(), (NSString)"ID");

            UIApplication.SharedApplication.ScheduleLocalNotification(n);

            return message.Id.ToString();
        }

        public void CancelNotification(string notificationId)
        {
            var k = (NSString)"id";
            foreach (var n in UIApplication.SharedApplication.ScheduledLocalNotifications)
            {
                if (n.UserInfo.TryGetValue(k, out var id))
                {
                    if (id as NSString == notificationId)
                    {
                        UIApplication.SharedApplication.CancelLocalNotification(n);
                        break;
                    }
                }
            }
        }
    }
}