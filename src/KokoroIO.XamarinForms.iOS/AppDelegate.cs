
using Foundation;
using UIKit;
using Microsoft.Azure.Mobile.Distribute;

namespace KokoroIO.XamarinForms.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();
            Distribute.DontCheckForUpdatesInDebug();
			LoadApplication(new App());

			return base.FinishedLaunching(app, options);
		}
	}
}
