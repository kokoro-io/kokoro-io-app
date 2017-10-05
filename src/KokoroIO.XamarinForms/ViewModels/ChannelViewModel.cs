using System;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelViewModel : ObservableObject
    {
        internal ChannelViewModel(ApplicationViewModel application, Channel model)
        {
            Application = application;
            Id = model.Id;
            Kind = model.Kind;

            Update(model);
        }

        internal ApplicationViewModel Application { get; }

        #region LastReadId

        internal int? _LastReadId;

        /// <summary>
        /// Gets or sets the id of th most recente message.
        /// </summary>
        internal int? LastReadId
        {
            get => _LastReadId;
            set => SetProperty(ref _LastReadId, value, onChanged: () => BeginWriteRealm(_LastReadId));
        }

        #endregion

        #region Channel

        public string Id { get; }

        #region ChannelName

        private string _ChannelName;

        public string ChannelName
        {
            get => _ChannelName;
            private set => SetProperty(ref _ChannelName, value, onChanged: () =>
            {
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Placeholder));
            });
        }

        public string DisplayName
            => (Kind == ChannelKind.DirectMessage ? '@' : '#') + ChannelName;

        public string Placeholder
            => "Let's talk" + (Kind == ChannelKind.DirectMessage ? " to " : " at ") + ChannelName;

        #endregion ChannelName

        #region Description

        private string _Description;

        public string Description
        {
            get => _Description;
            private set => SetProperty(ref _Description, value);
        }

        #endregion Description

        #region Kind

        public ChannelKind Kind { get; }

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

        #endregion Kind

        #region IsArchived

        private bool _IsArchived;

        public bool IsArchived
        {
            get => _IsArchived;
            private set => SetProperty(ref _IsArchived, value);
        }

        #endregion IsArchived

        internal void Update(Channel model)
        {
            UpdateCore(model);

            if (model.Membership != null)
            {
                UpdateCore(model.Membership);
            }
        }

        private void UpdateCore(Channel model)
        {
            ChannelName = model.ChannelName;
            Description = model.Description;
            IsArchived = model.IsArchived;
        }

        #endregion Channel

        #region Membership

        #region MembershipId

        private string _MembershipId;

        public string MembershipId
        {
            get => _MembershipId;
            private set => SetProperty(ref _MembershipId, value);
        }

        #endregion MembershipId

        #region NotificationDisabled

        private bool _NotificationDisabled;

        public bool NotificationDisabled
        {
            get => _NotificationDisabled;
            private set => SetProperty(ref _NotificationDisabled, value);
        }

        #endregion NotificationDisabled

        internal void Update(Membership model)
        {
            UpdateCore(model);

            if (model.Channel != null)
            {
                UpdateCore(model.Channel);
            }
        }

        private void UpdateCore(Membership model)
        {
            MembershipId = model.Id;
            NotificationDisabled = model.DisableNotification;
        }

        internal void ClearMembership()
        {
            MembershipId = null;
            NotificationDisabled = false;
            Application.Channels.Remove(this);

            PeekMembers()?.Remove(Application.LoginUser);

            if (Application.SelectedChannel == this)
            {
                Application.SelectedChannel = null;
            }
        }

        #endregion Membership

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

        internal ObservableRangeCollection<ProfileViewModel> PeekMembers()
        {
            if (_Members != null
                && _Members.TryGetTarget(out var c))
            {
                return c;
            }
            return null;
        }

        #endregion Members

        #region ChannelDetail

        private WeakReference<ChannelDetailViewModel> _ChannelDetail;

        internal ChannelDetailViewModel GetOrCreateChannelDetail()
        {
            if (_ChannelDetail == null
                || !_ChannelDetail.TryGetTarget(out var d))
            {
                d = new ChannelDetailViewModel(this);
                _ChannelDetail = new WeakReference<ChannelDetailViewModel>(d);
            }

            return d;
        }

        #endregion ChannelDetail

        #region ShowDetailCommand

        private Command _ShowDetailCommand;

        public Command ShowDetailCommand
            => _ShowDetailCommand ?? (_ShowDetailCommand = new Command(async () =>
            {
                var nav = App.Current?.MainPage?.Navigation;

                try
                {
                    await nav.PushModalAsync(new ChannelDetailPage()
                    {
                        BindingContext = GetOrCreateChannelDetail()
                    });
                }
                catch { }
            }));

        #endregion ShowDetailCommand

        #region Member events

        internal void Join(Membership membership)
        {
            var members = PeekMembers();

            if (members != null && membership.Authority != Authority.Invited)
            {
                var p = Application.GetProfileViewModel(membership.Profile);

                if (!members.Contains(p))
                {
                    members.Add(p);
                }
            }
        }

        internal void Leave(Membership membership)
        {
            var members = PeekMembers();
            var p = members?.FirstOrDefault(m => m.Id == membership.Profile.Id);

            if (p != null)
            {
                members.Remove(p);
            }
        }

        #endregion Member events

        internal async void BeginWriteRealm(int? lastReadId)
        {
            try
            {
                var rid = Id;
                using (var realm = await RealmServices.GetInstanceAsync())
                using (var trx = realm.BeginWrite())
                {
                    var rup = realm.All<ChannelUserProperties>().FirstOrDefault(r => r.ChannelId == rid);
                    if (rup == null)
                    {
                        rup = new ChannelUserProperties()
                        {
                            ChannelId = rid,
                            UserId = Application.LoginUser.Id
                        };

                        realm.Add(rup);
                    }

                    rup.LastVisited = DateTimeOffset.Now;
                    rup.LastReadId = lastReadId ?? rup.LastReadId;

                    trx.Commit();
                }
            }
            catch (Exception ex)
            {
                ex.Trace("SavingChannelUserPropertiesFailed");
            }
        }
    }
}