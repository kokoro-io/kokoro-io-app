using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using KokoroIO.XamarinForms.ViewModels;
using XLabs.Ioc;
using XLabs.Platform.Device;

namespace KokoroIO.XamarinForms.Droid
{
    [Activity(Label = "kokoro.io", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            if (Intent.Action == Intent.ActionSend)
            {
                try
                {
                    var s = Intent.Extras.Get(Intent.ExtraStream);
                    if (s != null)
                    {
                        var u = s as Uri ?? Uri.Parse(s.ToString());
                        if (s != null)
                        {
                            ApplicationViewModel.OpenFile(() => ContentResolver.OpenInputStream(u));
                        }
                    }
                }
                catch { }
            }

            var resolver = new SimpleContainer().Register(t => AndroidDevice.CurrentDevice);
            Resolver.ResetResolver(resolver.GetResolver());

            global::Xamarin.Forms.Forms.Init(this, bundle);

            LoadApplication(new App());
        }
    }
}