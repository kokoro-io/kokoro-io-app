using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.UWP.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(NotificationService))]

namespace KokoroIO.XamarinForms.UWP.Services
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
            var bg = new ToastBindingGeneric()
            {
                AppLogoOverride = new ToastGenericAppLogo()
                {
                    Source = message.Avatar, // TODO: apply hi res
                    HintCrop = ToastGenericAppLogoCrop.None
                }
            };
            bg.Children.Add(new AdaptiveText()
            {
                Text = $"#{message.Channel.ChannelName}",
                HintMaxLines = 1
            });
            bg.Children.Add(new AdaptiveText()
            {
                Text = message.RawContent
            });
            bg.Children.Add(new AdaptiveText()
            {
                Text = message.Profile.DisplayName,
                HintMaxLines = 1
            });
            var tc = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = bg
                },
                Launch = $"?channelId={message.Channel.Id}&messageId={message.Id}"
            };

            var toast = new ToastNotification(tc.GetXml());
            toast.Group = nameof(Message);
            toast.Tag = message.Id.ToString();
            toast.ExpirationTime = DateTime.Now.AddDays(1);

            ToastNotificationManager.CreateToastNotifier().Show(toast);

            return toast.Tag;
        }

        public void CancelNotification(string notificationId)
        {
            ToastNotificationManager.History.Remove(notificationId, nameof(Message));
        }
    }
}