using System;
using System.Linq;
using System.Security.Cryptography;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class LoginViewModel : BaseViewModel
    {
        public LoginViewModel()
        {
            Title = "Login";

            _MailAddress = UserSettings.MailAddress;
            _Password = UserSettings.Password;
            _EndPoint = UserSettings.EndPoint ?? new Client().EndPoint;
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

        #region EndPoint

        private string _EndPoint;

        public string EndPoint
        {
            get => _EndPoint;
            set => SetProperty(ref _EndPoint, value);
        }

        #endregion EndPoint

        public Command LoginCommand { get; }

        private async void BeginLogin()
        {
            var em = _MailAddress;
            var pw = _Password;
            var ep = _EndPoint;
            var svm = new SplashViewModel(async () =>
            {
                var preserve = false;
                var c = new Client();
                try
                {
                    var hasEndPoint = !string.IsNullOrWhiteSpace(ep) && c.EndPoint != ep;
                    if (hasEndPoint)
                    {
                        c.EndPoint = ep;
                    }

                    const string TokenName = nameof(KokoroIO) + "." + nameof(XamarinForms);
                    var ds = DependencyService.Get<IDeviceService>();

                    var hash = TokenName.Select(ch => (byte)ch).Concat(ds.GetIdentifier()).ToArray();

                    hash = SHA256.Create().ComputeHash(hash);

                    var di = Convert.ToBase64String(hash);

                    var device = await c.PostDeviceAsync(em, pw, ds.MachineName, ds.Kind, di);
                    c.AccessToken = device.AccessToken.Token;

                    var me = await c.GetProfileAsync();

                    UserSettings.MailAddress = em;
                    UserSettings.Password = pw;
                    UserSettings.EndPoint = hasEndPoint ? ep : null;
                    UserSettings.AccessToken = c.AccessToken;
                    await App.Current.SavePropertiesAsync();

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

            await App.Current.MainPage.Navigation.PushModalAsync(new SplashPage()
            {
                BindingContext = svm
            });
        }
    }
}