using System.Threading.Tasks;
using Foundation;
using KokoroIO.XamarinForms.iOS.Services;
using KokoroIO.XamarinForms.Models;
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
            n.AlertBody = $"{message.Profile.DisplayName}: {message.PlainTextContent}";
            n.AlertAction = $"?channelId={message.Channel.Id}&messageId={message.Id}";

            var ui = new NSMutableDictionary();
            ui[(NSString)"channelId"] = (NSString)message.Channel.Id;
            ui[(NSString)"id"] = (NSString)message.Id.ToString();

            n.UserInfo = ui;

            UIApplication.SharedApplication.ScheduleLocalNotification(n);

            return message.Id.ToString();
        }

        public void CancelNotification(string channelId, string notificationId, int channelUnreadCount)
        {
            var k1 = (NSString)"channelId";
            var k2 = (NSString)"id";
            foreach (var n in UIApplication.SharedApplication.ScheduledLocalNotifications)
            {
                if (n.UserInfo.TryGetValue(k1, out var cid)
                    && n.UserInfo.TryGetValue(k2, out var id))
                {
                    if (cid as NSString == channelId
                        && id as NSString == notificationId)
                    {
                        UIApplication.SharedApplication.CancelLocalNotification(n);
                        break;
                    }
                }
            }
        }
    }
}