using System.IO;
using System.Linq;
using Foundation;
using KokoroIO.XamarinForms.ViewModels;
using Microsoft.AppCenter.Distribute;
using UIKit;
using XLabs.Ioc;
using XLabs.Platform.Device;

namespace KokoroIO.XamarinForms.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //KeyboardOverlap.Forms.Plugin.iOSUnified.KeyboardOverlapRenderer.Init();

            string channelId = null;
            if (options?[UIApplication.LaunchOptionsLocalNotificationKey] is UILocalNotification n)
            {
                var qp = n.AlertAction.ParseQueryString();

                if (qp.TryGetValue("channelId", out var cid))
                {
                    channelId = cid;
                }
            }

            var resolver = new SimpleContainer().Register(t => AppleDevice.CurrentDevice);

            Resolver.SetResolver(resolver.GetResolver());

            global::Xamarin.Forms.Forms.Init();
            Distribute.DontCheckForUpdatesInDebug();

            LoadApplication(new App(channelId: channelId));

            return base.FinishedLaunching(app, options);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            ApplicationViewModel.OpenFile(() => new FileStream(url.Path, FileMode.Open));
            return true;
        }
    }
}