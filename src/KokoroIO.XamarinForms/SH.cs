using KokoroIO.XamarinForms.Services;

#if WINDOWS_UWP
using KokoroIO.XamarinForms.UWP.Services;
#else
#if __IOS__
using KokoroIO.XamarinForms.iOS.Services;
#else

using KokoroIO.XamarinForms.Droid.Services;

#endif
#endif

namespace KokoroIO.XamarinForms
{
    internal static class SH
    {
        static SH()
        {
            Device = new DeviceService();
            Audio = new AudioService();
            Notification = new NotificationService();
        }

        public static IDeviceService Device { get; }
        public static IAudioService Audio { get; }
        public static INotificationService Notification { get; }

        public static Client GetClient()
            => new Client();
    }
}