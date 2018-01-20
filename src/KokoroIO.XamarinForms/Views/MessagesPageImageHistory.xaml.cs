using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XLabs.Forms.Controls;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessagesPageImageHistory : PopupLayout
    {
        public MessagesPageImageHistory()
        {
            InitializeComponent();
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ((sender as ListView)).SelectedItem = null;
            (e.SelectedItem as ImageHistoryViewModel)?.Select();
        }
    }
}