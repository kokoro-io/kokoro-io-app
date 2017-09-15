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
                BindingContext = new MessagesViewModel(item)
            });

            lv.SelectedItem = null;
        }

        //async void AddItem_Clicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(new NewItemPage());
        //}

        //protected override void OnAppearing()
        //{
        //    base.OnAppearing();

        //    viewModel = viewModel ?? BindingContext as RoomsViewModel;

        //    if (viewModel.Items.Count == 0)
        //    {
        //        viewModel.LoadItemsCommand.Execute(null);
        //    }
        //}
    }
}