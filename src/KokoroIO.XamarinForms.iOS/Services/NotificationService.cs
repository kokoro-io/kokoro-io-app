using System.Linq;
using System.Threading.Tasks;
using Foundation;
using KokoroIO.XamarinForms.iOS.Services;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Services;
using UserNotifications;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(NotificationService))]

namespace KokoroIO.XamarinForms.iOS.Services
{
    public class NotificationService : INotificationService
    {
        private class NotificationCenterDelegate : UNUserNotificationCenterDelegate
        {
            public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, System.Action completionHandler)
            {
                switch (response.ActionIdentifier)
                {
                    default:
                        if (response.IsDefaultAction)
                        {
                            var avm = App.Current?.MainPage?.BindingContext as ApplicationViewModel;

                            if (avm != null)
                            {
                                var req = response.Notification.Request.Content.UserInfo;

                                if (req.TryGetValue(new NSString("channelId"), out var cobj))
                                {
                                    var cid = (cobj as NSString)?.Description;

                                    var ch = avm.Channels.FirstOrDefault(c => c.Id == cid);

                                    if (ch != null)
                                    {
                                        avm.SelectedChannel = ch;
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                }
                completionHandler();
            }
        }

        public async Task<string> GetPlatformNotificationServiceHandleAsync()
        {
            var tp = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Alert);

            if (!tp.Item1)
            {
                return null;
            }


            UNUserNotificationCenter.Current.Delegate = new NotificationCenterDelegate();

            return null;
        }

        public void UnregisterPlatformNotificationService()
        {
        }

        public string ShowNotification(Message message)
        {
            var id = message.Id.ToString();

            var mc = new UNMutableNotificationContent();
            mc.Title = "#" + message.Channel.ChannelName;
            mc.Body = $"{message.Profile.DisplayName}: {message.PlainTextContent}";
            mc.UserInfo = NSDictionary.FromObjectAndKey(new NSString("channelId"), new NSString(message.Channel.Id));
            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
            var req = UNNotificationRequest.FromIdentifier(id, mc, trigger);

            UNUserNotificationCenter.Current.AddNotificationRequest(req, e => { });

            return id;
        }

        public void CancelNotification(string channelId, string notificationId, int channelUnreadCount)
        {
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(new[] { notificationId });
        }
    }
}