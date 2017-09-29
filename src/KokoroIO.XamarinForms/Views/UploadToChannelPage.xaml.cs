using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UploadToChannelPage : ContentPage
    {
        public UploadToChannelPage()
        {
            InitializeComponent();
        }

        private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var vm = BindingContext as UploadToChannelViewModel;
            var lv = sender as ListView;

            var item = lv?.SelectedItem as ChannelViewModel;

            if (item != null)
            {
                var app = item.Application;

                app.SelectedChannel = item;

                while (Navigation.ModalStack.Any())
                {
                    await Navigation.PopModalAsync();
                }

                item.GetOrCreateMessagesPage().BeginUploadImage(vm.StreamCreator());
            }
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            while (Navigation.ModalStack.Any())
            {
                await Navigation.PopModalAsync();
            }
        }
    }
}