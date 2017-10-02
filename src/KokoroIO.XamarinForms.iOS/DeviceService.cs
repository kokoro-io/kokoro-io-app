using System.Threading.Tasks;
using KokoroIO.XamarinForms.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioService))]

namespace KokoroIO.XamarinForms.iOS
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => UIKit.UIDevice.CurrentDevice.Name;

        public DeviceKind Kind => DeviceKind.Ios;

        public byte[] GetIdentifier()
            => UIKit.UIDevice.CurrentDevice.IdentifierForVendor.GetBytes();

        public float GetDisplayScale()
            => (float)UIScreen.MainScreen.Scale;

        public Task<string> GetPlatformNotificationServiceHandleAsync()
            => Task.FromResult<string>(null);

        public void UnregisterPlatformNotificationService()
        {
        }
    }
}