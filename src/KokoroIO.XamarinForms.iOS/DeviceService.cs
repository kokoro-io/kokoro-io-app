using KokoroIO.XamarinForms.iOS;
using Shipwreck.KokoroIO;
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
    }
}