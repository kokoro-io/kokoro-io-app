using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services.Media;

namespace KokoroIO.XamarinForms.Droid
{
    [Activity(
#if DEBUG
        Label = "dev)kokoro.io"
#else
        Label = "kokoro.io"
#endif
        , Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.Multiple)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private static System.WeakReference<MainActivity> _Current;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var mp = App.Current?.MainPage;

            var avm = mp?.BindingContext as ApplicationViewModel;

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            _Current = new System.WeakReference<MainActivity>(this);

            var cid = Intent.Extras?.GetString("channelId");
            var mid = Intent.Extras?.GetString("messageId");

            if (avm != null)
            {
                avm.SelectedChannelId = cid ?? avm.SelectedChannelId;

                if (avm.SelectedChannel?.Id == cid
                    && int.TryParse(mid, out var id))
                {
                    avm.SelectedChannel.GetOrCreateMessagesPage().SelectedMessageId = id;
                }

                Xamarin.Essentials.Platform.Init(this, savedInstanceState);
                global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
                DependencyService.Register<IMediaPicker, Services.MediaPicker>();
                LoadApplication(new App(avm));

                ResetBindingContext(mp);
            }

            var resolver = new SimpleContainer().Register(t => AndroidDevice.CurrentDevice);
            Resolver.ResetResolver(resolver.GetResolver());

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            DependencyService.Register<IMediaPicker, Services.MediaPicker>();

            LoadApplication(new App(channelId: cid));

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

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            var mp = App.Current?.MainPage;

            var avm = mp?.BindingContext as ApplicationViewModel;
            var cid = intent.Extras?.GetString("channelId");
            var mid = intent.Extras?.GetString("messageId");

            if (avm != null)
            {
                avm.SelectedChannelId = cid ?? avm.SelectedChannelId;

                if (avm.SelectedChannel?.Id == cid
                    && int.TryParse(mid, out var id))
                {
                    avm.SelectedChannel.GetOrCreateMessagesPage().SelectedMessageId = id;
                }
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

        internal void CreateAndShowDialog(string message, string title)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            builder.SetMessage(message);
            builder.SetTitle(title);
            builder.Create().Show();
        }

        private static void ResetBindingContext(Page p)
        {
            if (p == null)
            {
                return;
            }
            p.BindingContext = null;

            if (p is MasterDetailPage mdp)
            {
                ResetBindingContext(mdp.Master);
                ResetBindingContext(mdp.Detail);
            }
            else if (p is NavigationPage np)
            {
                foreach (var cp in np.Navigation.NavigationStack)
                {
                    ResetBindingContext(cp);
                }
            }
            else if (p is MultiPage<Page> mp)
            {
                foreach (var cp in mp.Children)
                {
                    ResetBindingContext(cp);
                }
            }
        }
    }
}