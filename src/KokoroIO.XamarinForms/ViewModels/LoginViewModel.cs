using System;
using System.Diagnostics;
using System.Linq;
using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Views;
using Shipwreck.KokoroIO;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class LoginViewModel : BaseViewModel
    {
        public LoginViewModel()
        {
            Title = "Login";
            if (App.Current.Properties.TryGetValue(nameof(MailAddress), out var obj))
            {
                _MailAddress = obj as string;
            }
            if (App.Current.Properties.TryGetValue(nameof(Password), out obj))
            {
                _Password = obj as string;
            }

            LoginCommand = new Command(BeginLogin);
        }

        #region MailAddress

        private string _MailAddress;

        public string MailAddress
        {
            get => _MailAddress;
            set => SetProperty(ref _MailAddress, value);
        }

        #endregion MailAddress

        #region Password

        private string _Password;

        public string Password
        {
            get => _Password;
            set => SetProperty(ref _Password, value);
        }

        #endregion Password

        public Command LoginCommand { get; }

        private async void BeginLogin()
        {
            var em = _MailAddress;
            var pw = _Password;

            if (IsBusy)
            {
                return;
            }

            var preserve = false;
            var c = new Client();
            try
            {
                IsBusy = true;

                const string TokenName = nameof(KokoroIO) + "." + nameof(XamarinForms);

                var tokens = await c.GetAccessTokensAsync(em, pw);

                if (tokens.Any())
                {
                    var tk = tokens.FirstOrDefault(t => t.Name == TokenName) ?? tokens[0];

                    c.AccessToken = tk.Token;
                }
                else
                {
                    var tk = await c.PostAccessTokenAsync(em, pw, TokenName);

                    c.AccessToken = tk.Token;
                }

                App.Current.Properties[nameof(MailAddress)] = em;
                App.Current.Properties[nameof(Password)] = pw;

                preserve = true;

                var app = new ApplicationViewModel(c);

                App.Current.MainPage = new TabbedPage
                {
                    BindingContext = app,
                    Children =
                    {
                        new NavigationPage(new RoomsPage())
                        {
                            Title = "Rooms",
                            Icon = Device.OnPlatform<string>("tab_feed.png",null,null),
                            BindingContext = new RoomsViewModel(app)
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                if (!preserve)
                {
                    c.Dispose();
                }
                IsBusy = false;
            }
        }
    }
}