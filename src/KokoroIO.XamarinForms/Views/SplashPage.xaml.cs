using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            try
            {
                var vm = BindingContext as SplashViewModel;
                if (vm != null)
                {
                    var app = await vm.InitializeAsync();

                    if (app != null)
                    {
                        App.Current.MainPage = new RootPage(app);

                        while (Navigation.ModalStack.Any())
                        {
                            await Navigation.PopModalAsync();
                        }

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Trace("LoginFailed");
            }

            if (!(App.Current.MainPage is LoginPage))
            {
                App.Current.MainPage = new LoginPage();
            }

            while (Navigation.ModalStack.Any())
            {
                await Navigation.PopModalAsync();
            }

            MessagingCenter.Send(this, "LoginFailed");
        }
    }
}