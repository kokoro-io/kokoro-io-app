using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public partial class MenuPage : ContentPage
    {
        public MenuPage()
        {
            InitializeComponent();
            LogoImage.IsVisible = XDevice.Idiom != TargetIdiom.Phone;
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var lv = sender as ListView;

            var item = lv?.SelectedItem as ChannelViewModel;
            if (item == null)
            {
                return;
            }
            item.Application.SelectedChannel = item;

            lv.SelectedItem = null;
        }
    }
}