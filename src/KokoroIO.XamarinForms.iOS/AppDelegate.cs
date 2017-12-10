using System.IO;
using System.Linq;
using Foundation;
using KokoroIO.XamarinForms.ViewModels;
using Microsoft.Azure.Mobile.Distribute;
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

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            var avm = XamarinForms.App.Current?.MainPage?.BindingContext as ApplicationViewModel;

            if (avm != null)
            {
                var qp = notification.AlertAction.ParseQueryString();

                if (qp.TryGetValue("channelId", out var cid))
                {
                    var ch = avm.Channels.FirstOrDefault(c => c.Id == cid);

                    if (ch != null)
                    {
                        avm.SelectedChannel = ch;
                        return;
                    }
                }
            }

            base.ReceivedLocalNotification(application, notification);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            ApplicationViewModel.OpenFile(() => new FileStream(url.Path, FileMode.Open));
            return true;
        }
    }
}