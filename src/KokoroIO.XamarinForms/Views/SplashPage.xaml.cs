using System;
using System.Linq;
using System.Net.Http;
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
            string httpMessage = null;
            try
            {
                var vm = BindingContext as SplashViewModel;
                if (vm != null)
                {
                    var app = await vm.InitializeAsync();

                    if (app != null)
                    {
                        App.Current.MainPage = new RootPage(app);

                        try
                        {
                            while (Navigation.ModalStack.Any())
                            {
                                await Navigation.PopModalAsync();
                            }
                        }
                        catch { }

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                httpMessage = (ex.GetBaseException() as HttpRequestException)?.Message;
                ex.Warn("LoginFailed");
            }

            if (!(App.Current.MainPage is LoginPage))
            {
                App.Current.MainPage = new LoginPage();
            }

            try
            {
                while (Navigation.ModalStack.Any())
                {
                    await Navigation.PopModalAsync();
                }
            }
            catch { }

            MessagingCenter.Send(this, "LoginFailed", httpMessage);
        }
    }
}