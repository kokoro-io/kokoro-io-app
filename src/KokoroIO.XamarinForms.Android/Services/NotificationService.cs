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
            showIntent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(ctx, 0, showIntent, PendingIntentFlags.OneShot);

            var txt = $"{message.Profile.ScreenName}: {message.RawContent}";

            var notificationBuilder = new Notification.Builder(ctx)
                .SetSmallIcon(Resource.Drawable.kokoro_white)
                .SetContentTitle(!string.IsNullOrEmpty(message.Channel.ChannelName) ? "#" + message.Channel.ChannelName : "kokoro.io")
                .SetContentText(txt)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetStyle(new Notification.BigTextStyle().BigText(txt))
                .SetVibrate(new long[] { 100, 0, 100, 0, 100, 0 })
                .SetPriority((int)NotificationPriority.Max);

            var notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            notificationManager.Notify(message.Id, notificationBuilder.Build());

            return message.Id.ToString();
        }

        public void CancelNotification(string notificationId)
        {
            var ctx = MainActivity.GetCurrentContext() ?? PushHandlerService.Current;

            if (ctx != null
                && int.TryParse(notificationId, out var id))
            {
                ((NotificationManager)ctx.GetSystemService(Context.NotificationService)).Cancel(id);
            }
        }
    }
}