using System.IO;
using KokoroIO.XamarinForms.ViewModels;
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessagesPage : ContentPage
    {
        public MessagesPage()
        {
            InitializeComponent();
        }

        private void ExpandableEditor_FilePasted(object sender, EventArgs<Stream> e)
        {
            var vm = BindingContext as MessagesViewModel;

            if (vm == null)
            {
                e.Data.Dispose();
                return;
            }

            vm.BeginUploadImage(e.Data);
        }
    }
}