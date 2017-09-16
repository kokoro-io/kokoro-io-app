using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using KokoroIO.XamarinForms.Views;
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class LoginViewModel : BaseViewModel
    {
        private bool _AutomaticLogin;

        public LoginViewModel()
            : this(true)
        {
        }

        internal LoginViewModel(bool automaticLogin)
        {
            _AutomaticLogin = automaticLogin;

            Title = "Login";
            if (App.Current.Properties.TryGetValue(nameof(MailAddress), out var obj))
            {
                _MailAddress = obj as string;
            }
            if (automaticLogin)
            {
                if (App.Current.Properties.TryGetValue(nameof(Password), out obj))
                {
                    _Password = obj as string;
                }
            }
            else
            {
                App.Current.Properties.Remove(nameof(Password));
                App.Current.Properties.Remove(nameof(AccessToken));
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

                var app = new ApplicationViewModel(c, me);

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                MessagingCenter.Send(this, "LoginFailed");
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

        public async void BeginLoginByStoredToken()
        {
            if (!_AutomaticLogin
                || IsBusy
                || !App.Current.Properties.TryGetValue(nameof(AccessToken), out var at)
                || !(at is string ats))
            {
                return;
            }

            var preserve = false;
            var c = new Client()
            {
                AccessToken = ats
            };
            try
            {
                IsBusy = true;

                var me = await c.GetProfileAsync();

                preserve = true;

                var app = new ApplicationViewModel(c, me);

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                MessagingCenter.Send(this, "LoginFailed");
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