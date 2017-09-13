using System;

using KokoroIO.XamarinForms.Models;
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

        //async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        //{
        //    var item = args.SelectedItem as Item;
        //    if (item == null)
        //        return;

        //    await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)));

        //    // Manually deselect item
        //    ItemsListView.SelectedItem = null;
        //}

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
