using System.Text;
using Android.Util;
using KokoroIO.XamarinForms.Droid.Services;
using KokoroIO.XamarinForms.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceService))]

namespace KokoroIO.XamarinForms.Droid.Services
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => Android.OS.Build.Model;

        public DeviceKind Kind => DeviceKind.Android;

        public byte[] GetIdentifier()
            => Encoding.UTF8.GetBytes(Android.OS.Build.Serial);

        public float GetDisplayScale()
        {
            var a = MainActivity.GetCurrentActivity();

            if (a == null)
            {
                return 1;
            }

            var dm = new DisplayMetrics();

            a.WindowManager.DefaultDisplay.GetMetrics(dm);

            return (float)dm.DensityDpi / (float)DisplayMetricsDensity.Medium;
        }
    }
}