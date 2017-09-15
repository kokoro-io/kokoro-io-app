using KokoroIO.XamarinForms.ViewModels;
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

        private async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var lv = sender as ListView;

            var item = lv?.SelectedItem as RoomViewModel;
            if (item == null)
            {
                return;
            }

            await Navigation.PushAsync(new MessagesPage()
            {
                BindingContext = item.GetOrCreateMessagesPage()
            });

            lv.SelectedItem = null;
        }
    }
}