using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Views;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class SplashViewModel : BaseViewModel
    {
        public SplashViewModel(Func<Task<ApplicationViewModel>> loginAction)
        {
            Title = "Connecting...";
            LoginAction = loginAction;
        }

        internal Func<Task<ApplicationViewModel>> LoginAction { get; }
         
        public async Task<ApplicationViewModel> InitializeAsync()
        {
            var app = await LoginAction();
            if (app != null)
            {
                await app.LoadInitialDataTask;
            }
            return app;
        }
    }
}