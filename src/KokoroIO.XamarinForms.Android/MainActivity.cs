using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using KokoroIO.XamarinForms.ViewModels;
using XLabs.Ioc;
using Gcm.Client;
using XLabs.Platform.Device;
using Exception = System.Exception;

namespace KokoroIO.XamarinForms.Droid
{
    [Activity(Label = "kokoro.io", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private static System.WeakReference<MainActivity> _Current;

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            _Current = new System.WeakReference<MainActivity>(this);

            var resolver = new SimpleContainer().Register(t => AndroidDevice.CurrentDevice);
            Resolver.ResetResolver(resolver.GetResolver());

            global::Xamarin.Forms.Forms.Init(this, bundle);

            LoadApplication(new App());

            try
            {
                // Check to ensure everything's set up right
                GcmClient.CheckDevice(this);
                GcmClient.CheckManifest(this);

                // Register for push notifications
                System.Diagnostics.Debug.WriteLine("Registering...");
                GcmClient.Register(this, Secrets.SenderID);
            }
            catch (Java.Net.MalformedURLException)
            {
                CreateAndShowDialog("There was an error creating the client. Verify the URL.", "Error");
            }
            catch (Exception e)
            {
                CreateAndShowDialog(e.Message, "Error");
            }

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
        }

        internal static MainActivity GetCurrentActivity()
        {
            if (_Current != null && _Current.TryGetTarget(out var c))
            {
                return c;
            }
            return null;
        }
        internal static Context GetCurrentContext()
        {
            if (_Current != null && _Current.TryGetTarget(out var c))
            {
                return c.ApplicationContext;
            }
            return null;
        }
        private void CreateAndShowDialog(string message, string title)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            builder.SetMessage(message);
            builder.SetTitle(title);
            builder.Create().Show();
        }
    }
}