using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChannelListPage : ContentPage
    {
        public ChannelListPage()
        {
            InitializeComponent();
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var ch = e.SelectedItem as ChannelViewModel;
            if (ch != null)
            {
                ch.ShowDetailCommand.Execute(null);
            }
            var lv = sender as ListView;
            if (lv != null)
            {
                lv.SelectedItem = null;
            }
        }
    }
}