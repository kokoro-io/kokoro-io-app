using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChannelsView : ContentView
    {
        public ChannelsView()
        {
            InitializeComponent();
        }

        public DataTemplate ItemTemplate { get; set; }

        #region ItemsSource

        public static readonly BindableProperty ItemsSourceProperty
            = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<ChannelViewModel>), typeof(ChannelsView), propertyChanged: OnItemsSourceChanged);

        public IEnumerable<ChannelViewModel> ItemsSource
        {
            get => (IEnumerable<ChannelViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
            => (bindable as ChannelsView)?.OnItemsSourceChanged(oldValue, newValue);

        private void OnItemsSourceChanged(object oldValue, object newValue)
        {
            if (oldValue is INotifyCollectionChanged oncc)
            {
                oncc.CollectionChanged -= ItemsSource_CollectionChanged;
            }

            if (newValue is INotifyCollectionChanged nncc)
            {
                nncc.CollectionChanged += ItemsSource_CollectionChanged;
            }

            OnItemSourceReset();
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemSourceReset();
        }

        private void OnItemSourceReset()
        {
            var root = new TableRoot();

            var src = ItemsSource;

            if (src != null)
            {
                foreach (var c in src)
                {
                    var sec = root.LastOrDefault() as TableSection;
                    if (sec?.Title != c.KindName)
                    {
                        sec = new TableSection(c.KindName);
                        root.Add(sec);
                    }

                    var cell = ItemTemplate.CreateContent() as Cell;
                    cell.BindingContext = c;
                    cell.Tapped += Cell_Tapped;
                    sec.Add(cell);
                }
            }

            if (tableView.Root != null)
            {
                foreach (var s in tableView.Root)
                {
                    foreach (var c in s)
                    {
                        c.Tapped -= Cell_Tapped;
                    }
                }
            }

            tableView.Root = root;
        }

        private void Cell_Tapped(object sender, System.EventArgs e)
        {
            var lv = sender as Cell;

            var item = lv?.BindingContext as ChannelViewModel;
            if (item == null)
            {
                return;
            }

            item.Application.SelectedChannel = item;
        }

        #endregion ItemsSource
    }
}