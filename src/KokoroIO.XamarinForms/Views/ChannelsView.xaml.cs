using System.Collections;
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
            if (tableView.Root != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (OnItemAdding(e))
                        {
                            return;
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (OnItemRemoving(e))
                        {
                            return;
                        }

                        break;
                }
            }
            OnItemSourceReset();
        }

        private bool OnItemAdding(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
            {
                return false;
            }
            var nsi = e.NewStartingIndex >= 0 ? e.NewStartingIndex : ((IList)ItemsSource).IndexOf(e.NewItems[0]);
            if (nsi < 0)
            {
                return false;
            }
            TableSection s = null;
            int ni = 0;
            var prev = ItemsSource.ElementAtOrDefault(nsi - 1);

            foreach (ChannelViewModel n in e.NewItems)
            {
                if (prev?.Kind == n.Kind
                    && prev?.IsArchived == n.IsArchived)
                {
                    if (s == null)
                    {
                        s = tableView.Root.FirstOrDefault(ts => ts.Any(c => c.BindingContext == prev));
                        if (s == null)
                        {
                            s = new TableSection(n.KindName);
                            tableView.Root.Add(s);
                            ni = 0;
                        }
                        else
                        {
                            ni = s.IndexOf(s.FirstOrDefault(c => c.BindingContext == prev));
                            if (ni < 0)
                            {
                                ni = s.Count;
                            }
                        }
                    }
                }
                else
                {
                    var os = s;

                    s = new TableSection(n.KindName);

                    tableView.Root.Insert(os == null ? tableView.Root.Count : tableView.Root.IndexOf(os) + 1, s);
                    ni = 0;
                }

                var cell = ItemTemplate.CreateContent() as Cell;
                cell.BindingContext = n;
                cell.Tapped += Cell_Tapped;

                s.Insert(ni++, cell);

                prev = n;
            }

            return true;
        }

        private bool OnItemRemoving(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null)
            {
                return false;
            }

            foreach (ChannelViewModel o in e.OldItems)
            {
                var removed = false;
                foreach (var s in tableView.Root)
                {
                    foreach (var c in s)
                    {
                        if (c.BindingContext == o)
                        {
                            c.Tapped -= Cell_Tapped;
                            s.Remove(c);
                            if (!s.Any())
                            {
                                tableView.Root.Remove(s);
                            }
                            removed = true;
                            break;
                        }
                    }
                    if (removed)
                    {
                        break;
                    }
                }

                if (!removed)
                {
                    return false;
                }
            }

            return true;
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