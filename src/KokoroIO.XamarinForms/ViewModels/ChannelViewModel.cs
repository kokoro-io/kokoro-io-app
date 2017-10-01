using System;
using System.Linq;
using System.Threading.Tasks;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelViewModel : ObservableObject
    {
        internal ChannelViewModel(ApplicationViewModel application, Channel model)
        {
            Application = application;
            Id = model.Id;
            ChannelName = model.ChannelName;
            Kind = model.Kind;
            IsArchived = model.IsArchived;
            NotificationDisabled = model.Membership?.DisableNotification ?? false;
        }

        internal ApplicationViewModel Application { get; }

        public string Id { get; }
        public string ChannelName { get; }

        public string DisplayName
            => (Kind == ChannelKind.DirectMessage ? '@' : '#') + ChannelName;

        public string Placeholder
            => "Let's talk" + (Kind == ChannelKind.DirectMessage ? " to " : " at ") + ChannelName;

        public ChannelKind Kind { get; }
        public bool IsArchived { get; }
        public bool NotificationDisabled { get; }

        public string KindName
        {
            get
            {
                switch (Kind)
                {
                    case ChannelKind.PublicChannel:
                        return "Public Channels";

                    case ChannelKind.PrivateChannel:
                        return "Private Channels";

                    case ChannelKind.DirectMessage:
                        return "Direct Messages";
                }

                return Kind.ToString();
            }
        }

        #region MessagesPage

        private WeakReference<MessagesViewModel> _MessagesPage;

        public MessagesViewModel MessagesPage
            => _MessagesPage != null && _MessagesPage.TryGetTarget(out var mp) ? mp : null;

        public MessagesViewModel GetOrCreateMessagesPage()
        {
            var r = MessagesPage;
            if (r == null)
            {
                r = new MessagesViewModel(this);
                _MessagesPage = new WeakReference<MessagesViewModel>(r);
            }
            return r;
        }

        #endregion MessagesPage

        #region UnreadCount

        private int _UnreadCount;

        public int UnreadCount
        {
            get => _UnreadCount;
            set => SetProperty(ref _UnreadCount, value, onChanged: () => Application.OnUnreadCountChanged());
        }

        #endregion UnreadCount

        #region IsSelected

        private bool _IsSelected;

        public bool IsSelected
        {
            get => _IsSelected;
            internal set => SetProperty(ref _IsSelected, value);
        }

        #endregion IsSelected

        #region Members

        private WeakReference<ObservableRangeCollection<ProfileViewModel>> _Members;

        public ObservableRangeCollection<ProfileViewModel> Members
        {
            get
            {
                if (_Members != null
                    && _Members.TryGetTarget(out var c))
                {
                    return c;
                }
                c = new ObservableRangeCollection<ProfileViewModel>();
                _Members = new WeakReference<ObservableRangeCollection<ProfileViewModel>>(c);

                LoadMembersTask = BeginLoadMembers();

                return c;
            }
        }

        internal Task LoadMembersTask { get; private set; }

        private async Task BeginLoadMembers()
        {
            if (_Members != null
                && _Members.TryGetTarget(out var c))
            {
                try
                {
                    var channel = await Application.GetChannelMembershipsAsync(Id);

                    c.ReplaceRange(
                        channel.Memberships
                                .Where(m => m.Authority != Authority.Invited)
                                .Select(m => Application.GetProfileViewModel(m.Profile))
                                .OrderBy(m => m.ScreenName, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(m => m.Id));
                }
                catch (Exception ex)
                {
                    ex.Trace("LoadingChannelMemberFailed");
                }
            }
        }

        #endregion Members
    }
}