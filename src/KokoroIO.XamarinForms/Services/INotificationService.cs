using System.Threading.Tasks;

namespace KokoroIO.XamarinForms.Services
{
    public interface INotificationService
    {
        Task<string> GetPlatformNotificationServiceHandleAsync();

        void UnregisterPlatformNotificationService();

        string ShowNotification(Message message);

        void CancelNotification(string channelId, string notificationId, int channelUnreadCount);
    }
}