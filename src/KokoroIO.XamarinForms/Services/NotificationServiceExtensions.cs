using System;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;

namespace KokoroIO.XamarinForms.Services
{
    internal static class NotificationServiceExtensions
    {
        public static void ShowNotificationAndSave(this INotificationService ps, Message message)
        {
            if (ps == null)
            {
                return;
            }
            try
            {
                var mid = ps.ShowNotification(message);

                if (mid != null)
                {
                    UserSettings.AddMessageNotification(new MessageNotification()
                    {
                        MessageId = message.Id,
                        ChannelId = message.Channel.Id,
                        NotificationId = mid,
                    });
                }
            }
            catch (Exception ex)
            {
                ex.Warn("ReceiveInactiveNotificationFailed");
            }
        }
    }
}