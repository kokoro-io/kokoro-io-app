using System;
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
        internal readonly DataTemplate ItemTemplate;

        public ChannelsView()
        {
            InitializeComponent();

            ItemTemplate = (DataTemplate)Resources["channelListViewItemTemplate"];
        }

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
            if (e.NewItems == null || tableView.Root == null)
            {
                return false;
            }

            foreach (ChannelViewModel n in e.NewItems)
            {
                Add(n);
            }

            foreach (var s in tableView.Root)
            {
                SyncCells(s);
            }

            return true;
        }

        private bool OnItemRemoving(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null || tableView.Root == null)
            {
                return false;
            }

            foreach (ChannelViewModel o in e.OldItems)
            {
                Remove(o);
            }

            foreach (var s in tableView.Root)
            {
                SyncCells(s);
            }

            return true;
        }

        private void OnItemSourceReset()
        {
            var root = new TableRoot();

            foreach (var kn in new ChannelsViewKind[]
            {
                new ChannelsViewKind(){ Title="Public Channels", Kind= ChannelKind.PublicChannel },
                new ChannelsViewKind(){ Title="Private Channels", Kind= ChannelKind.PrivateChannel },
                new ChannelsViewKind(){ Title="Direct Messages", Kind= ChannelKind.DirectMessage }
            })
            {
                root.Add(new TableSection(kn.Title) { BindingContext = kn });
            }

            var src = ItemsSource;

            if (src != null)
            {
                foreach (var c in src)
                {
                    Add(c, root);
                }
            }

            if (tableView.Root != null)
            {
                foreach (var s in tableView.Root)
                {
                    foreach (var c in s)
                    {
                        (c.BindingContext as IDisposable)?.Dispose();
                    }
                }
            }

            foreach (var s in root)
            {
                SyncCells(s);
            }

            tableView.Root = root;
        }

        internal void Add(ChannelViewModel item, TableRoot root = null)
        {
            root = root ?? tableView.Root;
            if (root == null)
            {
                return;
            }

            var sec = root.FirstOrDefault(s => (s.BindingContext as ChannelsViewKind)?.Kind == item.Kind);

            if (sec == null)
            {
                var kn = new ChannelsViewKind() { Title = item.Kind.ToString(), Kind = item.Kind };
                root.Add(new TableSection(kn.Title) { BindingContext = kn });
            }
            var kind = (ChannelsViewKind)sec.BindingContext;

            var path = item.ChannelName.Split('/');
            IChannelsViewNodeParent parent = kind;

            var si = 0;

            for (var i = 0; i < path.Length; i++)
            {
                var pg = parent as ChannelsViewGroup;
                var name = path[i];
                bool isGroup = i < path.Length - 1;
                ChannelsViewNode newNode = null;
                if (isGroup)
                {
                    var g = parent.OfType<ChannelsViewGroup>().FirstOrDefault(n => n.Name == name);
                    if (g == null)
                    {
                        newNode = g = new ChannelsViewGroup(this, pg, name);
                    }
                    parent = g;
                }
                else
                {
                    var c = parent.OfType<ChannelsViewChannel>().FirstOrDefault(n => n.Channel == item);
                    if (c == null)
                    {
                        newNode = c = new ChannelsViewChannel(this, item, pg, name);
                    }
                }

                var pd = pg?.Depth ?? -1;
                var siblings = kind.Descendants.Skip(si).TakeWhile(cell => cell.Depth > pd);

                if (newNode == null)
                {
                    if (isGroup)
                    {
                        si = kind.Descendants.IndexOf((ChannelsViewGroup)parent) + 1;
                    }
                }
                else
                {
                    var next = siblings.Where(n => n.Depth == pd + 1
                                                    && Compare(newNode, n) < 0).FirstOrDefault()
                                ?? kind.Descendants.Skip(si).SkipWhile(n => n.Depth > pd).FirstOrDefault();

                    if (next == null)
                    {
                        si = kind.Descendants.Count;
                    }
                    else
                    {
                        si = kind.Descendants.IndexOf(next);
                    }

                    kind.Descendants.Insert(si++, newNode);

                    if (pg == null)
                    {
                        kind.Add(newNode);
                    }
                }
            }
        }

        private static int Compare(ChannelsViewNode a, ChannelsViewNode b)
        {
            if (a.IsGroup != b.IsGroup)
            {
                return a.IsGroup ? -1 : 1;
            }
            if (a.IsArchived != b.IsArchived)
            {
                return a.IsArchived ? 1 : -1;
            }
            var nr = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);

            return nr != 0 || a.IsGroup ? nr : ((ChannelsViewChannel)a).Channel.Id.CompareTo(((ChannelsViewChannel)b).Channel.Id);
        }

        internal bool Remove(ChannelViewModel item)
        {
            var root = tableView.Root;
            if (root == null)
            {
                return true;
            }

            var sec = root.FirstOrDefault(s => (s.BindingContext as ChannelsViewKind)?.Kind == item.Kind);

            if (sec == null)
            {
                return true;
            }

            var kn = (ChannelsViewKind)sec.BindingContext;

            ChannelsViewNode node = kn.Descendants.OfType<ChannelsViewChannel>().FirstOrDefault(c => c.Channel == item);

            var removed = 0;

            while (node != null)
            {
                removed++;

                kn.Descendants.Remove(node);
                if (node.HasCell)
                {
                    sec.Remove(node.Cell);
                }
                node.Dispose();
                kn.Remove(node);

                if (node.Parent == null || node.Parent.Any())
                {
                    break;
                }
                node = node.Parent;
            }

            return removed > 0;
        }

        internal void Cell_Tapped(object sender, System.EventArgs e)
        {
            var lv = sender as Cell;

            if (lv.BindingContext is ChannelsViewChannel c)
            {
                var item = c.Channel;
                item.Application.SelectedChannel = item;
            }
            else if (lv.BindingContext is ChannelsViewGroup g)
            {
                g.SetIsExpanded(!g.IsExpanded);

                foreach (var s in tableView.Root)
                {
                    SyncCells(s);
                }
            }
        }

        private void SyncCells(TableSection s)
        {
            var kn = (ChannelsViewKind)s.BindingContext;

            var j = 0;

            for (int i = 0; i < s.Count; i++)
            {
                var cell = s[i];
                var cellNode = (ChannelsViewNode)cell.BindingContext;
                var cellNodeIndex = kn.Descendants.IndexOf(cellNode, j);

                if (cellNodeIndex < j)
                {
                    s.RemoveAt(i--);
                }
                else
                {
                    while (j <= cellNodeIndex)
                    {
                        var n = kn.Descendants[j++];

                        if (n.IsVisible)
                        {
                            if (n != cellNode)
                            {
                                s.Insert(i++, n.Cell);
                            }
                        }
                        else
                        {
                            if (n == cellNode)
                            {
                                s.RemoveAt(i--);
                            }
                        }
                    }
                }
            }

            for (; j < kn.Descendants.Count; j++)
            {
                var n = kn.Descendants[j];
                if (n.IsVisible)
                {
                    s.Add(n.Cell);
                }
            }
        }

        #endregion ItemsSource
    }
}