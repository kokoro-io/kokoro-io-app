using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UploadersPage : ContentPage
    {
        public UploadersPage()
        {
            InitializeComponent();
        }
        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var vm = BindingContext as UploadersViewModel;
            var up = args.SelectedItem as IImageUploader;

            if (vm != null && up != null)
            {
                Navigation.PushModalAsync(new UploaderAuthorizationPage(vm.Application, up, vm.Callback));
            }
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}