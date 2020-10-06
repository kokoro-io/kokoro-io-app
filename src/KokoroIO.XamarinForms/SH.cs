using KokoroIO.XamarinForms.Services;
using System;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms
{
    internal static class SH
    {
        static SH()
        {
            Device = DependencyService.Get<IDeviceService>();
            Audio = DependencyService.Get<IAudioService>();
            Notification = DependencyService.Get<INotificationService>();
        }

        public static IDeviceService Device { get; }
        public static IAudioService Audio { get; }
        public static INotificationService Notification { get; }

        public static Client GetClient()
            => new Client(new System.Net.Http.HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(15)
            });
    }
}