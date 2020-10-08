using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
[assembly: ExportFont("Material-Design-Iconic-Font.ttf", Alias = "zmdi")]

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

            InitMainPage(channelId);
        }

        private void InitMainPage(string channelId = null)
        {
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
            Client c = null;
            try
            {
                c = SH.GetClient();
                c.AccessToken = accessToken;

                var ds = SH.Device;
                var ns = SH.Notification;

                var pnsTask = UserSettings.EnablePushNotification
                    ? ns.GetPlatformNotificationServiceHandleAsync()
                    : Task.FromResult<string>(null);

                var pnsTaskTimeout = Task.WhenAny(pnsTask, Task.Delay(15000));

                if (!string.IsNullOrWhiteSpace(endPoint))
                {
                    c.EndPoint = endPoint;
                    c.WebSocketEndPoint = Regex.Replace(Regex.Replace(endPoint, "^http", "ws"), "/api$", "/cable");
                }

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
            TH.Info("App.OnStart");
            base.OnStart();

            var types = new List<Type>();
            if (UserSettings.MobileCenterAnalyticsEnabled)
            {
                types.Add(typeof(Analytics));
            }
            if (UserSettings.MobileCenterCrashesEnabled)
            {
                types.Add(typeof(Crashes));
            }
            if (UserSettings.MobileCenterDistributeEnabled)
            {
                types.Add(typeof(Distribute));
            }

            if (types.Any())
            {
                AppCenter.Start("android=2bf93410-91e9-48a0-ac2a-b7cd2b2b62c1;"
                                + "ios=05892c61-9669-4090-83d3-0fe8f350d408;"
                                + "uwp=574a9213-3122-4c61-bb98-ada1a02e7f9d", types.ToArray());
            }
        }

        protected override async void OnSleep()
        {
            TH.Info("App.OnSleep");
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
            TH.Info("App.OnResume");
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
            else if (MainPage is RootPage)
            {
                TH.Info("Recreating ApplicationViewModel");
                InitMainPage();
            }
        }
    }
}