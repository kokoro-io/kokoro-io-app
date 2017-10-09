using System.Threading.Tasks;
using Android.App;
using Android.Content;
using KokoroIO.XamarinForms.Droid.Services;
using KokoroIO.XamarinForms.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(NotificationService))]

namespace KokoroIO.XamarinForms.Droid.Services
{
    public class NotificationService : INotificationService
    {
        public Task<string> GetPlatformNotificationServiceHandleAsync()
            => PushHandlerService.RegisterAsync();

        public void UnregisterPlatformNotificationService()
            => PushHandlerService.Unregister();

        public string ShowNotification(Message message)
        {
            var ctx = MainActivity.GetCurrentContext() ?? PushHandlerService.Current;

            if (ctx == null)
            {
                return null;
            }

            var showIntent = new Intent(ctx, typeof(MainActivity));
            showIntent.AddFlags(ActivityFlags.ReorderToFront);
            showIntent.PutExtra("channelId", message.Channel.Id);
            showIntent.PutExtra("messageId", message.Id);

            var pendingIntent = PendingIntent.GetActivity(ctx, 0, showIntent, PendingIntentFlags.OneShot);

            var nm = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            {
                var txt = $"{(string.IsNullOrEmpty(message.Channel.ChannelName) ? "A kokoro.io channel" : ("#" + message.Channel.ChannelName))} has unread messages.";

                var nb = new Notification.Builder(ctx)
                    .SetGroup(message.Channel.Id)
                    .SetGroupSummary(true)
                    .SetSmallIcon(Resource.Drawable.kokoro_white)
                    .SetContentTitle(!string.IsNullOrEmpty(message.Channel.ChannelName) ? "#" + message.Channel.ChannelName : "kokoro.io")
                    .SetStyle(new Notification.BigTextStyle().SetSummaryText(txt))
                    .SetContentText(txt)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetVibrate(new long[] { 100, 0, 100, 0, 100, 0 })
                    .SetPriority((int)NotificationPriority.Max);

                nm.Notify(message.Channel.Id, 0, nb.Build());
            }
            {
                var txt = $"{message.Profile.ScreenName}: {message.RawContent}";

                var nb = new Notification.Builder(ctx)
                    .SetGroup(message.Channel.Id)
                    .SetGroupSummary(false)
                    .SetSmallIcon(Resource.Drawable.kokoro_white)
                    .SetContentTitle(!string.IsNullOrEmpty(message.Channel.ChannelName) ? "#" + message.Channel.ChannelName : "kokoro.io")
                    .SetContentText(txt)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetStyle(new Notification.BigTextStyle().BigText(txt))
                    .SetVibrate(new long[] { 100, 0, 100, 0, 100, 0 })
                    .SetPriority((int)NotificationPriority.Max);

                nm.Notify(message.Channel.Id, message.Id, nb.Build());
            }
            return message.Id.ToString();
        }

        public void CancelNotification(string channelId, string notificationId, int channelUnreadCount)
        {
            var ctx = MainActivity.GetCurrentContext() ?? PushHandlerService.Current;

            if (ctx != null
                && int.TryParse(notificationId, out var id))
            {
                var nm = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
                nm?.Cancel(channelId, id);

                if (channelUnreadCount <= 0)
                {
                    nm?.Cancel(channelId, 0);
                }
            }
        }
    }
}