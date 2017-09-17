﻿using Foundation;
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
            var resolver = new SimpleContainer().Register(t => AppleDevice.CurrentDevice);

            Resolver.SetResolver(resolver.GetResolver());

            global::Xamarin.Forms.Forms.Init();
            Distribute.DontCheckForUpdatesInDebug();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}