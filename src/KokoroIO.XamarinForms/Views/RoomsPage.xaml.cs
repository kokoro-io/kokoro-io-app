using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public partial class RoomsPage : ContentPage
    {
        // RoomsViewModel viewModel;

        public RoomsPage()
        {
            InitializeComponent();
            LogoImage.IsVisible = XDevice.Idiom != TargetIdiom.Phone;
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var lv = sender as ListView;

            var item = lv?.SelectedItem as RoomViewModel;
            if (item == null)
            {
                return;
            }
            item.Application.SelectedRoom = item;

            lv.SelectedItem = null;
        }
    }
}