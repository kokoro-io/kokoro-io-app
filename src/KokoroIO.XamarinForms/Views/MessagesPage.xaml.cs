using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
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

        private bool _FirstTime = true;

        private void ListView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            var lv = (ListView)sender;
            var vm = (MessagesViewModel)BindingContext;
            if (_FirstTime)
            {
                lv.ScrollTo(vm.Messages.Last(), ScrollToPosition.End, false);
                _FirstTime = false;
            }
        }
    }
}