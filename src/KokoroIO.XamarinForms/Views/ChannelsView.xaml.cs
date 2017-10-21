using System;
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
        private interface ITreeNodeParent : IEnumerable<TreeNode>
        {
            void Add(TreeNode item);

            void Remove(TreeNode item);
        }

        private sealed class KindNode : ITreeNodeParent
        {
            private readonly List<TreeNode> _Children = new List<TreeNode>();

            public string Title { get; set; }
            public ChannelKind Kind { get; set; }

            internal List<TreeNode> Descendants { get; } = new List<TreeNode>();

            public void Add(TreeNode item)
            {
                _Children.Add(item);
            }

            public void Remove(TreeNode item)
            {
                _Children.Remove(item);
            }

            public IEnumerator<TreeNode> GetEnumerator()
                => _Children.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => _Children.GetEnumerator();
        }

        private abstract class TreeNode : ObservableObject, IDisposable
        {
            protected readonly ChannelsView Control;

            protected TreeNode(ChannelsView control, GroupNode parent, string name)
            {
                Control = control;
                Parent = parent;
                Name = name;
            }

            internal GroupNode Parent { get; }

            public int Depth => (Parent?.Depth ?? -1) + 1;

            public string Name { get; }
            public abstract string FullName { get; }

            public abstract bool IsGroup { get; }

            public abstract bool IsSelected { get; }

            public abstract bool IsArchived { get; }

            public abstract int UnreadCount { get; }

            private bool _IsVisible = true;

            public bool IsVisible
            {
                get => _IsVisible;
                protected set => SetProperty(ref _IsVisible, value);
            }

            internal abstract void SetIsVisible();

            private bool _IsUnreadCountVisible;

            public bool IsUnreadCountVisible => _IsUnreadCountVisible;

            protected void SetIsUnreadCountVisible()
                => SetProperty(ref _IsUnreadCountVisible, UnreadCount > 0 && (!IsGroup || !IsExpanded), nameof(IsUnreadCountVisible));

            public abstract bool IsExpanded { get; }

            private Cell _Cell;

            internal Cell Cell
            {
                get
                {
                    if (_Cell == null)
                    {
                        _Cell = Control.ItemTemplate.CreateContent() as Cell;
                        _Cell.BindingContext = this;
                        _Cell.Tapped += Control.Cell_Tapped;
                    }

                    return _Cell;
                }
            }

            internal bool HasCell => _Cell != null;

            public virtual void Dispose()
            {
                Parent?.Remove(this);

                if (_Cell != null)
                {
                    _Cell.Tapped -= Control.Cell_Tapped;
                    _Cell = null;
                }
            }
        }

        private sealed class ChannelNode : TreeNode
        {
            internal ChannelViewModel Channel;

            public ChannelNode(ChannelsView control, ChannelViewModel channel, GroupNode parent, string name)
                : base(control, parent, name)
            {
                Channel = channel;
                Channel.PropertyChanged += Channel_PropertyChanged;

                Parent?.Add(this);
            }

            public override bool IsGroup => false;

            public override bool IsSelected => Channel.IsSelected;
            public override bool IsArchived => Channel.IsArchived;
            public override bool IsExpanded => true;

            public override int UnreadCount => Channel.UnreadCount;

            public override string FullName => Parent?.FullName + Name;

            internal override void SetIsVisible()
            {
                var p = Parent;
                while (p != null)
                {
                    if (!p.IsExpanded)
                    {
                        IsVisible = false;
                        return;
                    }
                    p = p.Parent;
                }
                IsVisible = true;
            }

            private void Channel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(Channel.IsArchived):
                    case nameof(Channel.DisplayName):
                        Control.Remove(Channel);
                        Control.Add(Channel);
                        break;

                    case nameof(Channel.IsSelected):
                        OnPropertyChanged(e.PropertyName);
                        break;

                    case nameof(Channel.UnreadCount):
                        OnPropertyChanged(e.PropertyName);
                        SetIsUnreadCountVisible();
                        break;
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                Channel.PropertyChanged -= Channel_PropertyChanged;
            }
        }

        private sealed class GroupNode : TreeNode, ITreeNodeParent
        {
            private readonly List<TreeNode> _Children;

            public GroupNode(ChannelsView control, GroupNode parent, string name)
                : base(control, parent, name)
            {
                _Children = new List<TreeNode>(4);

                Parent?.Add(this);
            }

            public override string FullName => Parent?.FullName + Name + "/";

            public override bool IsGroup => true;

            public override bool IsSelected => false;

            private int _UnreadCount;

            public override int UnreadCount => _UnreadCount;

            private void SetUnreadCount()
                => SetProperty(ref _UnreadCount, _Children.Sum(c => c.UnreadCount), nameof(UnreadCount), onChanged: () => SetIsUnreadCountVisible());

            private bool _IsArchived;

            public override bool IsArchived => _IsArchived;

            private void SetIsArchived()
                => SetProperty(ref _IsArchived, _Children.All(c => c.IsArchived), nameof(IsArchived));

            private bool _IsExpanded = true;
            public override bool IsExpanded => _IsExpanded;

            internal void SetIsExpanded(bool value)
            {
                if (value != _IsExpanded)
                {
                    _IsExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));

                    SetIsUnreadCountVisible();

                    foreach (var c in _Children)
                    {
                        c.SetIsVisible();
                    }
                }
            }

            internal override void SetIsVisible()
            {
                var p = Parent;
                while (p != null)
                {
                    if (!p.IsExpanded)
                    {
                        IsVisible = false;

                        foreach (var c in _Children)
                        {
                            c.SetIsVisible();
                        }

                        return;
                    }
                    p = p.Parent;
                }
                IsVisible = true;

                foreach (var c in _Children)
                {
                    c.SetIsVisible();
                }
            }

            public void Add(TreeNode item)
            {
                _Children.Add(item);
                SetUnreadCount();
                SetIsArchived();
                item.PropertyChanged += Item_PropertyChanged;
            }

            public void Remove(TreeNode item)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                _Children.Remove(item);
                SetUnreadCount();
                SetIsArchived();
            }

            private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(UnreadCount):
                        SetUnreadCount();
                        break;

                    case nameof(IsArchived):
                        SetIsArchived();
                        break;
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                foreach (var e in _Children)
                {
                    e.PropertyChanged -= Item_PropertyChanged;
                }

                _Children.Clear();
            }

            public IEnumerator<TreeNode> GetEnumerator()
                => _Children.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => _Children.GetEnumerator();
        }

        private DataTemplate ItemTemplate;

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

            foreach (var kn in new KindNode[]
            {
                new KindNode(){ Title="Public Channels", Kind= ChannelKind.PublicChannel },
                new KindNode(){ Title="Private Channels", Kind= ChannelKind.PrivateChannel },
                new KindNode(){ Title="Direct Messages", Kind= ChannelKind.DirectMessage }
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
                        c.Tapped -= Cell_Tapped;
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

        private void Add(ChannelViewModel item, TableRoot root = null)
        {
            root = root ?? tableView.Root;
            if (root == null)
            {
                return;
            }

            var sec = root.FirstOrDefault(s => (s.BindingContext as KindNode)?.Kind == item.Kind);

            if (sec == null)
            {
                var kn = new KindNode() { Title = item.Kind.ToString(), Kind = item.Kind };
                root.Add(new TableSection(kn.Title) { BindingContext = kn });
            }
            var kind = (KindNode)sec.BindingContext;

            var path = item.ChannelName.Split('/');
            ITreeNodeParent parent = kind;

            var si = 0;

            for (var i = 0; i < path.Length; i++)
            {
                var pg = parent as GroupNode;
                var name = path[i];
                bool isGroup = i < path.Length - 1;
                TreeNode newNode = null;
                if (isGroup)
                {
                    var g = parent.OfType<GroupNode>().FirstOrDefault(n => n.Name == name);
                    if (g == null)
                    {
                        newNode = g = new GroupNode(this, pg, name);
                    }
                    parent = g;
                }
                else
                {
                    var c = parent.OfType<ChannelNode>().FirstOrDefault(n => n.Channel == item);
                    if (c == null)
                    {
                        newNode = c = new ChannelNode(this, item, pg, name);
                    }
                }

                var pd = pg?.Depth ?? -1;
                var siblings = kind.Descendants.Skip(si).TakeWhile(cell => cell.Depth > pd);

                if (newNode == null)
                {
                    if (isGroup)
                    {
                        si = kind.Descendants.IndexOf((GroupNode)parent) + 1;
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

        private static int Compare(TreeNode a, TreeNode b)
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

            return nr != 0 || a.IsGroup ? nr : ((ChannelNode)a).Channel.Id.CompareTo(((ChannelNode)b).Channel.Id);
        }

        private void Remove(ChannelViewModel item)
        {
            var root = tableView.Root;
            if (root == null)
            {
                return;
            }

            var sec = root.FirstOrDefault(s => (s.BindingContext as KindNode)?.Kind == item.Kind);

            if (sec == null)
            {
                return;
            }

            var kn = (KindNode)sec.BindingContext;

            TreeNode node = kn.Descendants.OfType<ChannelNode>().FirstOrDefault(c => c.Channel == item);
            while (node != null)
            {
                kn.Descendants.Remove(node);
                node.Dispose();

                if (node.Parent == null || node.Parent.Any())
                {
                    break;
                }
                node = node.Parent;
            }
        }

        private void Cell_Tapped(object sender, System.EventArgs e)
        {
            var lv = sender as Cell;

            if (lv.BindingContext is ChannelNode c)
            {
                var item = c.Channel;
                item.Application.SelectedChannel = item;
            }
            else if (lv.BindingContext is GroupNode g)
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
            var kn = (KindNode)s.BindingContext;

            var j = 0;

            for (int i = 0; i < s.Count; i++)
            {
                var cell = s[i];
                var cellNode = (TreeNode)cell.BindingContext;
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