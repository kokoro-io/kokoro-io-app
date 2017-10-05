using System.Threading.Tasks;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.UWP.Services;
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

        // TODO: implement ios notification service
        public string ShowNotification(Message message) => null;
    }
}