using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class ChannelsViewGroup : ChannelsViewNode, IChannelsViewNodeParent
    {
        private readonly List<ChannelsViewNode> _Children;

        public ChannelsViewGroup(ChannelsView control, ChannelsViewGroup parent, string name)
            : base(control, parent, name)
        {
            _Children = new List<ChannelsViewNode>(4);

            Parent?.Add(this);
        }

        public override string FullName => Parent?.FullName + Name + "/";

        public override bool IsGroup => true;

        public override bool IsSelected => false;

        public override string Discriminator => null;

        #region UnreadCount

        private int? _UnreadCount;

        public override int? UnreadCount => _UnreadCount;

        private void SetUnreadCount()
            => SetProperty(
                ref _UnreadCount,
                _Children.Any(c => c.UnreadCount == null) ? (int?)null
                : _Children.Where(c => c.UnreadCount > 0).Sum(c => c.UnreadCount),
                nameof(UnreadCount),
                onChanged: () => SetIsUnreadCountVisible());

        protected override void SetIsUnreadCountVisible()
            => IsUnreadCountVisible = !IsExpanded && UnreadCount != 0;

        #endregion UnreadCount

        #region IsArchived

        private bool _IsArchived;

        public override bool IsArchived => _IsArchived;

        private void SetIsArchived()
            => SetProperty(ref _IsArchived, _Children.All(c => c.IsArchived), nameof(IsArchived));

        #endregion IsArchived

        #region IsExpanded

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

        #endregion IsExpanded

        internal bool HasSingleChild => _Children.Count == 1;

        #region SetIsVisible

        internal override void SetIsVisible()
        {
            SetIsVisibleCore();

            foreach (var c in _Children)
            {
                c.SetIsVisible();
            }
        }

        private void SetIsVisibleCore()
        {
            if (HasSingleChild)
            {
                IsVisible = false;
                return;
            }

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

        #endregion SetIsVisible

        public void Add(ChannelsViewNode item)
        {
            var single = HasSingleChild;

            _Children.Add(item);
            SetUnreadCount();
            SetIsArchived();
            item.PropertyChanged += Item_PropertyChanged;

            if (single != HasSingleChild)
            {
                HasSingleChildChanged();
            }
        }

        public void Remove(ChannelsViewNode item)
        {
            var single = HasSingleChild;

            item.PropertyChanged -= Item_PropertyChanged;
            _Children.Remove(item);
            SetUnreadCount();
            SetIsArchived();

            if (single != HasSingleChild)
            {
                HasSingleChildChanged();
            }
        }

        private void HasSingleChildChanged()
        {
            SetIsVisibleCore();

            foreach (var c in _Children)
            {
                c.OnAncestorHasSingleChildFailed();
            }
        }

        internal override void OnAncestorHasSingleChildFailed()
        {
            base.OnAncestorHasSingleChildFailed();

            foreach (var c in _Children)
            {
                c.OnAncestorHasSingleChildFailed();
            }
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

        public IEnumerator<ChannelsViewNode> GetEnumerator()
            => _Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _Children.GetEnumerator();
    }
}