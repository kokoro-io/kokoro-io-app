using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Services;
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
                var c = SH.GetClient();
                try
                {
                    var hasEndPoint = !string.IsNullOrWhiteSpace(ep) && c.EndPoint != ep;
                    if (hasEndPoint)
                    {
                        c.EndPoint = ep;
                        c.WebSocketEndPoint = Regex.Replace(Regex.Replace(ep, "^http", "ws"), "/api$", "/cable");
                    }

                    var ds = SH.Device;
                    var ns = SH.Notification;

                    var pnsTask = UserSettings.EnablePushNotification
                        ? ns.GetPlatformNotificationServiceHandleAsync()
                        : Task.FromResult<string>(null);

                    await Task.WhenAny(pnsTask, Task.Delay(15000));

                    var pns = pnsTask.Status == TaskStatus.RanToCompletion ? pnsTask.Result : null;

                    if (string.IsNullOrEmpty(em) && !string.IsNullOrEmpty(pw))
                    {
                        c.AccessToken = pw;
                        await c.PostDeviceAsync(ds.MachineName, ds.Kind, ds.GetDeviceIdentifierString(), pns, pns != null);
                    }
                    else
                    {
                        var device = await c.PostDeviceAsync(em, pw, ds.MachineName, ds.Kind, ds.GetDeviceIdentifierString(), pns, pns != null);
                        c.AccessToken = device.AccessToken.Token;
                    }

                    var me = await c.GetProfileAsync();

                    UserSettings.MailAddress = em;
                    UserSettings.Password = pw;
                    UserSettings.EndPoint = hasEndPoint ? ep : null;
                    UserSettings.AccessToken = c.AccessToken;
                    UserSettings.PnsHandle = pns;

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