using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelViewModel : ObservableObject
    {
        internal ChannelViewModel(ApplicationViewModel application, Channel model)
        {
            Application = application;
            Kind = model.Kind;

            Update(model);

            // deffer setting id to detect initialization
            Id = model.Id;
        }

        internal ChannelViewModel(ApplicationViewModel application, Membership model)
        {
            Application = application;
            Kind = model.Channel.Kind;

            Update(model);

            // deffer setting id to detect initialization
            Id = model.Channel.Id;
        }

        internal ApplicationViewModel Application { get; }

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

        #region LatestMessageId

        private int? _LatestMessageId;

        /// <summary>
        /// 最新メッセージのIDを取得します。
        /// </summary>
        public int? LatestMessageId
        {
            get => _LatestMessageId;
            private set => SetProperty(ref _LatestMessageId, value, onChanged: UpdateUnreadCount);
        }

        #endregion LatestMessageId

        #region LatestMessagePublishedAt

        private DateTime? _LatestMessagePublishedAt;

        /// <summary>
        /// 最新メッセージ投稿日時を取得します。
        /// </summary>
        public DateTime? LatestMessagePublishedAt
        {
            get => _LatestMessagePublishedAt;
            private set => SetProperty(ref _LatestMessagePublishedAt, value);
        }

        #endregion LatestMessagePublishedAt

        #region MessagesCount

        private int _MessagesCount;

        /// <summary>
        /// メッセージ数を取得します。
        /// </summary>
        public int MessagesCount
        {
            get => _MessagesCount;
            private set => SetProperty(ref _MessagesCount, value);
        }

        #endregion MessagesCount

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
            // TODO: use Profile.ScreenName instead to avoid API bug
            ChannelName = (model.Kind == ChannelKind.DirectMessage
                                    && model.Memberships?.Length == 2
                            ? model.Memberships
                                    .FirstOrDefault(m => m.Profile.Id != Application.LoginUser.Id)
                                    ?.Profile.ScreenName
                            : null)
                        ?? model.ChannelName;
            Description = model.Description;
            IsArchived = model.IsArchived;
            LatestMessageId = model.LatestMessageId;
            LatestMessagePublishedAt = model.LatestMessagePublishedAt;
            MessagesCount = model.MessagesCount;
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

        #region Authority

        private Authority? _Authority;

        public Authority? Authority
        {
            get => _Authority;
            private set => SetProperty(ref _Authority, value);
        }

        #endregion Authority

        #region NotificationDisabled

        private bool _NotificationDisabled;

        [Obsolete]
        public bool NotificationDisabled
        {
            get => _NotificationDisabled;
            private set => SetProperty(ref _NotificationDisabled, value);
        }

        #endregion NotificationDisabled

        #region NotificationPolicy

        private NotificationPolicy _NotificationPolicy;

        /// <summary>
        /// 通知ポリシーを取得します。
        /// </summary>
        public NotificationPolicy NotificationPolicy
        {
            get => _NotificationPolicy;
            private set => SetProperty(ref _NotificationPolicy, value);
        }

        #endregion NotificationPolicy

        #region ReadStateTrackingPolicy

        private ReadStateTrackingPolicy _ReadStateTrackingPolicy;

        /// <summary>
        /// 未読メッセージ表示ポリシーを取得します。
        /// </summary>
        public ReadStateTrackingPolicy ReadStateTrackingPolicy
        {
            get => _ReadStateTrackingPolicy;
            private set => SetProperty(ref _ReadStateTrackingPolicy, value);
        }

        #endregion ReadStateTrackingPolicy

        #region LatestReadMessageId

        private int? _LatestReadMessageId;

        /// <summary>
        /// 一番新しい既読メッセージを取得します。
        /// </summary>
        public int? LatestReadMessageId
        {
            get => _LatestReadMessageId;
            internal set => SetProperty(
                        ref _LatestReadMessageId,
                        _LatestReadMessageId == null || _LatestReadMessageId < value ? value : _LatestReadMessageId,
                        onChanged: () =>
                        {
                            UpdateUnreadCount();
                            BeginUpdateLatestReadId();
                            CancelNotificationsAsync(_LatestReadMessageId).GetHashCode();
                        });
        }

        private void BeginUpdateLatestReadId()
        {
            if (Id == null)
            {
                return;
            }
            var value = _LatestReadMessageId;

            XDevice.BeginInvokeOnMainThread(async () =>
            {
                if (MembershipId != null || value == _LatestReadMessageId)
                {
                    try
                    {
                        await Application.PutMembershipAsync(MembershipId, latestReadMessageId: value).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        TH.Warn(ex, "BeginUpdateLatestReadId");
                        return;
                    }
                }
            });
        }

        #endregion LatestReadMessageId

        #region Visible

        private bool _Visible = true;

        /// <summary>
        /// チャット画面のチャンネル一覧ペインに表示するかどうかを示す値を取得します。
        /// </summary>
        public bool Visible
        {
            get => _Visible;
            private set => SetProperty(ref _Visible, value);
        }

        #endregion Visible

        #region Muted

        private bool _Muted;

        /// <summary>
        /// チャット画面のチャンネル一覧ペインにて未読数表示をオフにし、表示を薄くするかどうか示す値を取得します。
        /// </summary>
        public bool Muted
        {
            get => _Muted;
            private set => SetProperty(ref _Muted, value);
        }

        #endregion Muted

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
            Authority = model.Authority;
            NotificationDisabled = model.DisableNotification;
            NotificationPolicy = model.NotificationPolicy;
            ReadStateTrackingPolicy = model.ReadStateTrackingPolicy;
            LatestReadMessageId = model.LatestReadMessageId;
            Visible = model.Visible;
            Muted = model.Muted;
        }

        internal void ClearMembership()
        {
            MembershipId = null;
            Authority = null;
            NotificationDisabled = false;
            NotificationPolicy = default(NotificationPolicy);
            ReadStateTrackingPolicy = default(ReadStateTrackingPolicy);
            LatestReadMessageId = null;
            Visible = true;
            Muted = false;
            Application.Channels.Remove(this);

            PeekMembers()?.Remove(Application.LoginUser);

            if (Application.SelectedChannel == this)
            {
                Application.SelectedChannel = null;
            }
        }

        #region ToggleNotification

        private Command _ToggleNotificationCommand;

        public Command ToggleNotificationCommand
            => _ToggleNotificationCommand ?? (_ToggleNotificationCommand = new Command(ToggleNotification));

        private async void ToggleNotification()
        {
            var mid = _MembershipId;
            var v = _NotificationDisabled;
            if (mid == null)
            {
                return;
            }

            try
            {
                await Application.PutMembershipAsync(mid, v == false);
            }
            catch (Exception ex)
            {
                ex.Error("PutMembershipFailed");
            }
        }

        #endregion ToggleNotification

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

        #region IsSelected

        private bool _IsSelected;

        public bool IsSelected
        {
            get => _IsSelected;
            internal set => SetProperty(ref _IsSelected, value, onChanged: () =>
            {
                if (_IsSelected)
                {
                    var mp = MessagesPage;

                    if (mp != null)
                    {
                        mp.ClearPopupCommand.Execute(null);

                        if (!mp.MessagesLoaded
                            && mp.Messages.Count == 0
                            && !mp.IsBusy)
                        {
                            mp.BeginAppend();
                        }
                    }
                }
            });
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
                                .Where(m => m.Authority != KokoroIO.Authority.Invited)
                                .Select(m => Application.GetProfileViewModel(m.Profile))
                                .OrderBy(m => m.ScreenName, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(m => m.Id));
                }
                catch (Exception ex)
                {
                    ex.Error("LoadingChannelMemberFailed");
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

            if (members != null && membership.Authority != KokoroIO.Authority.Invited)
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

        private HashSet<int> _Unreads;

        public int UnreadCount
            => _Unreads?.Count ?? 0;

        #region HasUnread

        private bool _HasUnread;

        public bool HasUnread
        {
            get => _HasUnread;
            private set => SetProperty(ref _HasUnread, value);
        }

        #endregion HasUnread

        internal void Receive(Message message)
        {
            if (!(_LatestReadMessageId < message.Id))
            {
                return;
            }

            if (!HasUnread || UnreadCount > 0)
            {
                (_Unreads ?? (_Unreads = new HashSet<int>())).Add(message.Id);
                OnPropertyChanged(nameof(UnreadCount));
            }

            HasUnread = true;

            var shouldNotify = message.Profile.Id != Application.LoginUser.Id
                            && _NotificationDisabled == false;

            switch (NotificationPolicy)
            {
                case NotificationPolicy.Nothing:
                    shouldNotify = false;
                    break;

                case NotificationPolicy.OnlyMentions:
                    shouldNotify &= message.RawContent.Contains($"<@{Application.LoginUser.Id}|");
                    break;
            }

            if (Application.SelectedChannel == this)
            {
                var mp = MessagesPage;
                if (mp?.HasNext == false
                    && mp.Messages.Last().IsShown)
                {
                    mp.BeginAppend();
                }
            }
            else if (shouldNotify)
            {
                SH.Notification.ShowNotificationAndSave(message);
            }

            if (shouldNotify && UserSettings.PlayRingtone)
            {
                SH.Audio.PlayNotification();
            }
        }

        private void UpdateUnreadCount()
        {
            if (LatestMessageId == null || LatestMessageId <= LatestReadMessageId)
            {
                ClearUnreadAsync().GetHashCode();
                return;
            }

            var mp = MessagesPage;

            _Unreads?.Clear();
            if (mp?.Messages.Count >= _MessagesCount
                || (_LatestReadMessageId != null && mp?.Messages.Any(e => e.Id == _LatestReadMessageId) == true))
            {
                (_Unreads ?? (_Unreads = new HashSet<int>())).UnionWith(mp.Messages.Reverse().Select(e => e.Id).OfType<int>().TakeWhile(e => e != _LatestReadMessageId));
            }

            OnPropertyChanged(nameof(UnreadCount));
            HasUnread = true;
        }

        private async Task ClearUnreadAsync()
        {
            var hasUnread = _Unreads?.Count > 0 || _HasUnread;

            if (_Unreads?.Count > 0)
            {
                _Unreads.Clear();
                OnPropertyChanged(nameof(UnreadCount));
            }

            HasUnread = false;

            if (hasUnread)
            {
                TH.Info("Clearing unreads in #{0} ({1})", DisplayName, Id);

                await CancelNotificationsAsync(null).ConfigureAwait(false);
            }
        }

        private async Task CancelNotificationsAsync(int? latestReadMessageId)
        {
            if (Id == null)
            {
                return;
            }

            try
            {
                using (var realm = await RealmServices.GetInstanceAsync())
                using (var trx = realm.BeginWrite())
                {
                    var lid = latestReadMessageId ?? int.MaxValue;
                    var ns = realm.All<MessageNotification>().Where(n => n.ChannelId == Id && n.MessageId <= lid);

                    if (ns.Any())
                    {
                        var notification = SH.Notification;
                        foreach (var n in ns)
                        {
                            try
                            {
                                notification?.CancelNotification(n.ChannelId, n.NotificationId, 0);
                            }
                            catch { }

                            realm.Remove(n);
                        }
                        trx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Error("SavingChannelUserPropertiesFailed");
            }
        }
    }
}