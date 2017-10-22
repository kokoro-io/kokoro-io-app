using System;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<SplashPage, string>(this, "LoginFailed", (lvm, err) =>
            {
                DisplayAlert("kokoro.io", string.IsNullOrEmpty(err) ? "Failed to login" : "Failed to login: " + err, "OK");
            });
        }

        private void MailAddress_Completed(object sender, EventArgs e)
        {
            password.Focus();
        }

        private void Password_Completed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(mailAddress.Text))
            {
                mailAddress.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password.Text))
            {
                password.Focus();
                return;
            }

            loginButton.Command?.Execute(null);
        }
    }
}