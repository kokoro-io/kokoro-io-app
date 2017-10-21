using System;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    internal abstract class ChannelsViewNode : ObservableObject, IDisposable
    {
        protected readonly ChannelsView Control;

        protected ChannelsViewNode(ChannelsView control, ChannelsViewGroup parent, string name)
        {
            Control = control;
            Parent = parent;
            Name = name;
        }

        internal ChannelsViewGroup Parent { get; }

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
        public abstract string Discriminator { get; }

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

        internal virtual void OnAncestorHasSingleChildFailed()
        {
            var diff = 0;

            var p = Parent;

            var name = Name + (IsGroup ? "/" : null);

            while (p?.HasSingleChild == true)
            {
                diff--;
                name = p.Name + "/" + name;
                p = p.Parent;
            }

            var hasValue = _ConcatenatedName != null;

            _ConcatenatedName = name;
            _ConcatenationDepth = Depth + diff;

            if (hasValue)
            {
                OnPropertyChanged(nameof(ConcatenatedName));
                OnPropertyChanged(nameof(ConcatenationDepth));
            }
        }

        private string _ConcatenatedName;

        public string ConcatenatedName
        {
            get
            {
                if (_ConcatenatedName == null
                    && Discriminator == null)
                {
                    OnAncestorHasSingleChildFailed();
                }
                return _ConcatenatedName;
            }
        }

        private int _ConcatenationDepth;

        public int ConcatenationDepth
        {
            get
            {
                if (_ConcatenatedName == null
                    && Discriminator == null)
                {
                    OnAncestorHasSingleChildFailed();
                }
                return _ConcatenationDepth;
            }
        }
    }
}