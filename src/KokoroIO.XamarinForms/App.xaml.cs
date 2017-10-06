using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Distribute;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace KokoroIO.XamarinForms
{
    public partial class App : Application
    {
        public App(ApplicationViewModel viewModel = null, string channelId = null)
        {
            InitializeComponent();

            if (viewModel != null)
            {
                viewModel.SelectedChannelId = channelId ?? viewModel.SelectedChannel?.Id;
                App.Current.MainPage = new RootPage(viewModel);

                return;
            }

            var at = UserSettings.AccessToken;
            var ep = UserSettings.EndPoint;

            if (!string.IsNullOrEmpty(at))
            {
                MainPage = new SplashPage()
                {
                    BindingContext = new SplashViewModel(() => LoginAsync(at, ep, channelId))
                };
            }
            else
            {
                MainPage = new LoginPage();
            }
        }

        private async Task<ApplicationViewModel> LoginAsync(string accessToken, string endPoint, string channelId)
        {
            var preserve = false;
            var c = new Client();
            try
            {
                c = new Client()
                {
                    AccessToken = accessToken
                };

                var ds = SH.Device;
                var ns = SH.Notification;

                var pnsTask = ns.GetPlatformNotificationServiceHandleAsync();

                var pnsTaskTimeout = Task.WhenAny(pnsTask, Task.Delay(15000));

                c.EndPoint = !string.IsNullOrWhiteSpace(endPoint) ? endPoint : c.EndPoint;

                var me = await c.GetProfileAsync().ConfigureAwait(false);

                preserve = true;

                await pnsTaskTimeout;

                var pns = pnsTask.Status == TaskStatus.RanToCompletion ? pnsTask.Result : UserSettings.PnsHandle;

                if (pns != UserSettings.PnsHandle)
                {
                    await c.PostDeviceAsync(ds.MachineName, ds.Kind, ds.GetDeviceIdentifierString(), pns, pns != null);

                    UserSettings.PnsHandle = pns;
                    await App.Current.SavePropertiesAsync();
                }

                return new ApplicationViewModel(c, me) { SelectedChannelId = channelId };
            }
            finally
            {
                if (!preserve)
                {
                    c.Dispose();
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            MobileCenter.Start("android=2bf93410-91e9-48a0-ac2a-b7cd2b2b62c1;", typeof(Analytics), typeof(Crashes), typeof(Distribute));
        }

        protected override async void OnSleep()
        {
            base.OnSleep();

            if (MainPage?.BindingContext is ApplicationViewModel avm)
            {
                try
                {
                    await avm.CloseAsync();
                }
                catch { }
            }
        }

        protected override async void OnResume()
        {
            base.OnResume();

            var avm = MainPage?.BindingContext as ApplicationViewModel;
            if (avm != null)
            {
                try
                {
                    await avm.ConnectAsync();
                }
                catch { }
            }
        }
    }
}