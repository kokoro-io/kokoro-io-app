using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Distribute;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XDevice = Xamarin.Forms.Device;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace KokoroIO.XamarinForms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            try
            {
                var app = LoginViewModel.LoginAsync().GetAwaiter().GetResult();

                if (app != null)
                {
                    SetMainPage(app);
                    return;
                }
            }
            catch { }

            MainPage = new LoginPage();
        }


        internal static void SetMainPage(ApplicationViewModel app)
        {
            App.Current.MainPage = new TabbedPage
            {
                BindingContext = app,
                Children =
                    {
                        new NavigationPage(new RoomsPage())
                        {
                            Title = "Rooms",
                            Icon = XDevice.OnPlatform<string>("tab_feed.png",null,null),
                            BindingContext = new RoomsViewModel(app)
                        }
                    }
            };
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