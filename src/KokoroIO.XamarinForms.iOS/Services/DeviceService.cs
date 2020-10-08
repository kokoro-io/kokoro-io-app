using KokoroIO.XamarinForms.iOS.Services;
using KokoroIO.XamarinForms.Services;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceService))]

namespace KokoroIO.XamarinForms.iOS.Services
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => UIKit.UIDevice.CurrentDevice.Name;

        public DeviceKind Kind => DeviceKind.Ios;

        public float GetDisplayScale()
            => (float)UIScreen.MainScreen.Scale;
    }
}