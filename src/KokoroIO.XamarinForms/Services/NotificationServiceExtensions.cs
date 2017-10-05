using System;
using KokoroIO.XamarinForms.Models.Data;

namespace KokoroIO.XamarinForms.Services
{
    internal static class NotificationServiceExtensions
    {
        public static async void ShowNotificationAndSave(this INotificationService ps, Message message)
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
                    using (var realm = await RealmServices.GetInstanceAsync())
                    using (var trx = realm.BeginWrite())
                    {
                        var mn = new MessageNotification();
                        mn.MessageId = message.Id;
                        mn.ChannelId = message.Channel.Id;
                        mn.NotificationId = mid;

                        realm.Add(mn);

                        trx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Trace("ReceiveInactiveNotificationFailed");
            }
        }
    }
}