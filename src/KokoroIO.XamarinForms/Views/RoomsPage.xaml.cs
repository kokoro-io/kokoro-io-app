using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Shipwreck.KokoroIO;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public partial class RoomsPage : ContentPage
    {
        // RoomsViewModel viewModel;

        public RoomsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            while (Navigation.NavigationStack.Last() != this)
            {
                await Navigation.PopAsync();
            }
        }

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            try
            {
                await (BindingContext as RoomsViewModel)?.Application.CloseAsync();
            }
            catch { }

            App.Current.Properties.Remove(nameof(LoginViewModel.Password));
            App.Current.Properties.Remove(nameof(AccessToken));
            await App.Current.SavePropertiesAsync();

            App.Current.MainPage = new LoginPage();
        }

        private async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var lv = sender as ListView;

            var item = lv?.SelectedItem as RoomViewModel;
            if (item == null)
            {
                return;
            }

            var mp = new MessagesPage()
            {
                BindingContext = item.GetOrCreateMessagesPage()
            };
            await Navigation.PushAsync(mp);

            lv.SelectedItem = null;
        }
    }
}