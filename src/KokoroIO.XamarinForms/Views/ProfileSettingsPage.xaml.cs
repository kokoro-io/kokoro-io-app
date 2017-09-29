using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProfileSettingsPage : ContentPage
    {
        public ProfileSettingsPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<ProfileSettingsViewModel>(this, "UpdateProfileFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to update a profile", "OK");
            });
        }
    }
}