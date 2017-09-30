using System.Text;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceService))]

namespace KokoroIO.XamarinForms.Droid
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => Android.OS.Build.Model;

        public DeviceKind Kind => DeviceKind.Android;

        public byte[] GetIdentifier()
            => Encoding.UTF8.GetBytes(Android.OS.Build.Serial);

        public Task<string> GetPlatformNotificationServiceHandleAsync()
            => PushHandlerService.RegisterAsync();

        public void UnregisterPlatformNotificationService()
            => PushHandlerService.Unregister();
    }
}