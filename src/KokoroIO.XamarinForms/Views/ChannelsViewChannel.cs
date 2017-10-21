using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class ChannelsViewChannel : ChannelsViewNode
    {
        internal ChannelViewModel Channel;

        public ChannelsViewChannel(ChannelsView control, ChannelViewModel channel, ChannelsViewGroup parent, string name)
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

        private string _Discriminator;

        public override string Discriminator => _Discriminator;

        internal override void OnAncestorHasSingleChildFailed()
        {
            base.OnAncestorHasSingleChildFailed();

            SetProperty(ref _Discriminator, string.IsNullOrEmpty(ConcatenatedName) ? "empty" : null, nameof(Discriminator));
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
}