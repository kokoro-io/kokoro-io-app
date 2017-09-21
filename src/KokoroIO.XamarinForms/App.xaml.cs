using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Distribute;
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace KokoroIO.XamarinForms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            if (App.Current.Properties.TryGetValue(nameof(AccessToken), out var at)
                && at is string ats)
            {
                var svm = new SplashViewModel(async () =>
                {
                    var preserve = false;
                    var c = new Client();
                    try
                    {
                        c = new Client()
                        {
                            AccessToken = ats
                        };
                        var me = await c.GetProfileAsync().ConfigureAwait(false);

                        preserve = true;

                        return new ApplicationViewModel(c, me);
                    }
                    finally
                    {
                        if (!preserve)
                        {
                            c.Dispose();
                        }
                    }
                });

                MainPage = new SplashPage()
                {
                    BindingContext = svm
                };
            }
            else
            {
                MainPage = new LoginPage();
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

            var avm = MainPage?.BindingContext as ApplicationViewModel;
            if (avm != null)
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