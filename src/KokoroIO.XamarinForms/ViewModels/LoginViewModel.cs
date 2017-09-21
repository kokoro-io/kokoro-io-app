using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
            var svm = new SplashViewModel(async () =>
            {
                var preserve = false;
                var c = new Client();
                try
                {
                    const string TokenName = nameof(KokoroIO) + "." + nameof(XamarinForms);
                    var ds = DependencyService.Get<IDeviceService>();

                    var hash = TokenName.Select(ch => (byte)ch).Concat(ds.GetIdentifier()).ToArray();

                    hash = SHA256.Create().ComputeHash(hash);

                    var di = Convert.ToBase64String(hash);

                    var device = await c.PostDeviceAsync(em, pw, ds.MachineName, ds.Kind, di);
                    c.AccessToken = device.AccessToken.Token;

                    var me = await c.GetProfileAsync();

                    App.Current.Properties[nameof(MailAddress)] = em;
                    App.Current.Properties[nameof(Password)] = pw;
                    App.Current.Properties[nameof(AccessToken)] = c.AccessToken;
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

            await App.Current.MainPage.Navigation.PushAsync(new SplashPage()
            {
                BindingContext = svm
            });
        }
    }
}