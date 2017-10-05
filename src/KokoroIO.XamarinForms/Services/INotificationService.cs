using System.Threading.Tasks;

namespace KokoroIO.XamarinForms.Services
{
    public interface INotificationService
    {
        Task<string> GetPlatformNotificationServiceHandleAsync();

        void UnregisterPlatformNotificationService();

        string ShowNotification(Message message);
    }
}