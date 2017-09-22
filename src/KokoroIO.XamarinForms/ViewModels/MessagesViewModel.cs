using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Helpers;
using KokoroIO.XamarinForms.Models.Data;
using Realms;
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesViewModel : BaseViewModel
    {
        internal MessagesViewModel(RoomViewModel room)
        {
            Room = room;
            Title = room.DisplayName;
            _IsArchiveBannerShown = room.IsArchived;
            _HasUnread = Room.UnreadCount > 0;

            PrependCommand = new Command(BeginPrepend);
            RefreshCommand = new Command(BeginAppend);

            Room.PropertyChanged += Room_PropertyChanged;
        }

        #region Room

        public RoomViewModel Room { get; }

        private void Room_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Room.IsArchived):
                    OnPropertyChanged(nameof(CanPost));
                    break;
            }
        }

        private ObservableRangeCollection<ProfileViewModel> _AllMembers;

        public ObservableRangeCollection<ProfileViewModel> AllMembers
        {
            get
            {
                if (_AllMembers == null)
                {
                    _AllMembers = new ObservableRangeCollection<ProfileViewModel>();

                    BeginLoadMembers();
                }
                return _AllMembers;
            }
        }

        private async void BeginLoadMembers()
        {
            try
            {
                // TODO: Get room members
                var ps = await Application.Client.GetProfilesAsync();

                _AllMembers.AddRange(ps.Select(p => Application.GetProfile(p)));

                UpdateMemberCandicates();
            }
            catch (Exception ex)
            {
                ex.Trace("LoadingRoomMemberFailed");
            }
        }

        #endregion Room

        public ApplicationViewModel Application => Room.Application;

        public Command OpenUrlCommand => Application.OpenUrlCommand;

        #region Showing Messages

        #region Messages

        private ObservableRangeCollection<MessageInfo> _Messages;

        public ObservableRangeCollection<MessageInfo> Messages
        {
            get
            {
                if (_Messages == null)
                {
                    _Messages = new ObservableRangeCollection<MessageInfo>();
                    BeginLoadMessages().GetHashCode();
                }
                return _Messages;
            }
        }

        #endregion Messages

        #region HasPrevious

        private bool _HasPrevious = true;

        public bool HasPrevious
        {
            get => _HasPrevious;
            private set => SetProperty(ref _HasPrevious, value);
        }

        #endregion HasPrevious

        public void BeginPrepend()
            => BeginLoadMessages(true).GetHashCode();

        public void BeginAppend()
            => BeginLoadMessages(false).GetHashCode();

        private async Task BeginLoadMessages(bool prepend = false)
        {
            if (IsBusy || (Room.IsArchived && Room.UnreadCount <= 0 && !prepend && _Messages?.Count > 0))
            {
                return;
            }
            try
            {
                IsBusy = true;

                // TODO: increase page size by depth
                const int PAGE_SIZE = 30;

                int? bid, aid;

                if (Messages.Count == 0)
                {
                    aid = bid = null;
                }
                else if (prepend)
                {
                    if (!HasPrevious)
                    {
                        IsBusy = false;
                        return;
                    }
                    bid = _Messages.First().Id;
                    aid = null;
                }
                else
                {
                    bid = null;
                    aid = _Messages.Last().Id;
                }

                var messages = await Application.Client.GetMessagesAsync(Room.Id, PAGE_SIZE, beforeId: bid, afterId: aid);

                HasPrevious &= aid != null || messages.Length >= PAGE_SIZE;

                if (aid != null)
                {
                    if (messages.Length >= PAGE_SIZE)
                    {
                        Room.UnreadCount = Math.Max(0, Room.UnreadCount - messages.Length);
                    }
                    else
                    {
                        Room.UnreadCount = 0;
                    }
                }
                else if (bid == null && messages.Length < PAGE_SIZE)
                {
                    Room.UnreadCount = 0;
                }
                HasUnread = Room.UnreadCount > 0;

                if (messages.Any())
                {
                    InsertMessages(messages);
                }

                try
                {
                    var rid = Room.Id;
                    using (var realm = Realm.GetInstance())
                    {
                        using (var trx = realm.BeginWrite())
                        {
                            var rup = realm.All<RoomUserProperties>().FirstOrDefault(r => r.RoomId == rid);
                            if (rup == null)
                            {
                                rup = new RoomUserProperties()
                                {
                                    RoomId = rid,
                                    UserId = Application.LoginUser.Id,
                                    LastVisited = DateTimeOffset.Now
                                };

                                realm.Add(rup);
                            }
                            else
                            {
                                rup.LastVisited = DateTimeOffset.Now;
                            }

                            trx.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Trace("SavingRoomUserPropertiesFailed");
                }
            }
            catch (Exception ex)
            {
                ex.Trace("Load message failed");

                MessagingCenter.Send(this, "LoadMessageFailed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InsertMessages(Message[] messages)
        {
            var minId = messages.Min(m => m.Id);
            var maxId = messages.Max(m => m.Id);

            if (!_Messages.Any() || _Messages.Last().Id < minId)
            {
                var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                for (var i = 0; i < mvms.Count; i++)
                {
                    mvms[i].SetIsMerged(i == 0 ? _Messages.LastOrDefault() : mvms[i - 1]);
                }

                _Messages.AddRange(mvms);
            }
            else
            {
                var i = 0;

                foreach (var m in messages.OrderBy(e => e.Id))
                {
                    var vm = new MessageInfo(this, m);
                    for (; ; i++)
                    {
                        var prev = _Messages[i];

                        if (m.Id < prev.Id)
                        {
                            _Messages.Insert(i, vm);
                            break;
                        }
                        else if (m.Id == prev.Id)
                        {
                            prev.Update(m);
                            break;
                        }
                        else if (i + 1 >= _Messages.Count)
                        {
                            _Messages.Add(vm);
                            break;
                        }
                        else if (m.Id < _Messages[i + 1].Id)
                        {
                            _Messages.Insert(i, vm);
                            break;
                        }
                    }
                }
            }
        }

        internal void UpdateMessage(Message message)
            => _Messages?.FirstOrDefault(m => m.Id == message.Id)?.Update(message);

        public Command PrependCommand { get; set; }
        public Command RefreshCommand { get; set; }

        #region SelectedMessage

        private MessageInfo _SelectedMessage;

        public MessageInfo SelectedMessage
        {
            get => _SelectedMessage;
            set => SetProperty(ref _SelectedMessage, value);
        }

        #endregion SelectedMessage

        #endregion Showing Messages

        #region Post Message

        #region NewMessage

        private string _NewMessage = string.Empty;

        public string NewMessage
        {
            get => _NewMessage;
            set => SetProperty(ref _NewMessage, value, onChanged: UpdateMemberCandicates);
        }

        #endregion NewMessage

        #region Candicates

        #region SelectionStart

        private int _SelectionStart;

        public int SelectionStart
        {
            get => _SelectionStart;
            set => SetProperty(ref _SelectionStart, value, onChanged: UpdateMemberCandicates);
        }

        #endregion SelectionStart

        #region SelectionLength

        private int _SelectionLength;

        public int SelectionLength
        {
            get => _SelectionLength;
            set => SetProperty(ref _SelectionLength, value, onChanged: UpdateMemberCandicates);
        }

        #endregion SelectionLength

        #region NewMessageFocused

        private bool _NewMessageFocused;

        public bool NewMessageFocused
        {
            get => _NewMessageFocused;
            set => SetProperty(ref _NewMessageFocused, value);
        }

        #endregion NewMessageFocused

        #region ShowMemberCandicates

        private bool _ShowMemberCandicates;

        public bool ShowMemberCandicates
        {
            get => _ShowMemberCandicates;
            private set => SetProperty(ref _ShowMemberCandicates, value);
        }

        #endregion ShowMemberCandicates

        #region MemberCandicates

        private ObservableRangeCollection<ProfileViewModel> _MemberCandicates;

        public ObservableRangeCollection<ProfileViewModel> MemberCandicates
            => _MemberCandicates ?? (_MemberCandicates = new ObservableRangeCollection<ProfileViewModel>());

        #endregion MemberCandicates

        private void UpdateMemberCandicates()
        {
            var t = _NewMessage;
            var s = _SelectionStart;
            var l = _SelectionLength;

            if (t != null && 0 < s && l >= 0 && AllMembers.Count > 0)
            {
                var i = s + l;
                if (i == t.Length || (i < t.Length && char.IsWhiteSpace(t[i])))
                {
                    for (i--; i >= 0; i--)
                    {
                        var c = t[i];
                        if (c == '@')
                        {
                            var pref = t.Substring(i + 1, s + l - i - 1);

                            MemberCandicates.ReplaceRange(AllMembers.Where(p => p.ScreenName.StartsWith(pref, StringComparison.OrdinalIgnoreCase)));
                            ShowMemberCandicates = MemberCandicates.Any();
                            return;
                        }
                        else if (char.IsDigit(c) || char.IsLetter(c))
                        {
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            ShowMemberCandicates = false;
            MemberCandicates.Clear();
        }

        private Command _SelectMemberCommand;

        public Command SelectMemberCommand
            => _SelectMemberCommand ?? (_SelectMemberCommand = new Command(OnSelectMember));

        private void OnSelectMember(object parameter)
        {
            if (!(parameter is ProfileViewModel p))
            {
                return;
            }
            var t = _NewMessage;
            var s = _SelectionStart;
            var l = _SelectionLength;

            if (t != null && 0 < s && l >= 0)
            {
                var i = s + l;
                if (i == t.Length || (i < t.Length && char.IsWhiteSpace(t[i])))
                {
                    for (i--; i >= 0; i--)
                    {
                        var c = t[i];
                        if (c == '@')
                        {
                            NewMessage = t.Substring(0, i) + "@" + p.ScreenName + (s + l == t.Length ? " " : (" " + t.Substring(s + l + 1)));
                            SelectionStart = i + p.ScreenName.Length + 2;
                            SelectionLength = 0;
                            NewMessageFocused = true;
                            CandicateClicked = DateTime.Now;
                            return;
                        }
                        else if (char.IsDigit(c) || char.IsLetter(c))
                        {
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        internal DateTime? CandicateClicked { get; set; }

        #endregion Candicates

        #region Post Message

        public bool CanPost
            => !Room.IsArchived;

        #region PostCommand

        private Command _PostCommand;

        public Command PostCommand
            => _PostCommand ?? (_PostCommand = new Command(BeginPost));

        public async void BeginPost()
        {
            var m = _NewMessage;

            var nsfw = false;// TODO: NSFW UI
            var succeeded = false;

            if (IsBusy || string.IsNullOrEmpty(m))
            {
                return;
            }
            try
            {
                IsBusy = true;
                await Application.Client.PostMessageAsync(Room.Id, m, nsfw);
                NewMessage = string.Empty;
                succeeded = true;
            }
            catch (Exception ex)
            {
                ex.Trace("Post message failed");

                MessagingCenter.Send(this, "PostMessageFailed");
            }
            finally
            {
                IsBusy = false;
            }

            if (succeeded)
            {
                BeginAppend();
                // TODO: scroll to new message
            }
        }

        #endregion PostCommand

        #region UploadImageCommand

        public bool SupportsImageUpload
            => Application.MediaPicker?.IsPhotosSupported == true;

        private Command _UploadImageCommand;

        public Command UploadImageCommand
            => _UploadImageCommand ?? (_UploadImageCommand = new Command(p => BeginUploadImage(p as Stream)));

        public void BeginUploadImage(Stream data = null)
        {
            Application.BeginUpload(new UploadParameter(AppendUrl, error =>
            {
                data?.Dispose();

                if (error != null)
                {
                    MessagingCenter.Send(this, "UploadImageFailed");
                }
            }, data: data));
        }

        #endregion UploadImageCommand

        #region TakePhotoCommand

        public bool SupportsTakePhoto
            => Application.MediaPicker?.IsPhotosSupported == true
                && Application.MediaPicker?.IsCameraAvailable == true;

        private Command _TakePhotoCommand;

        public Command TakePhotoCommand
            => _TakePhotoCommand ?? (_TakePhotoCommand = new Command(BeginTakePhoto));

        public void BeginTakePhoto()
        {
            Application.BeginUpload(new UploadParameter(AppendUrl, error =>
            {
                if (error != null)
                {
                    MessagingCenter.Send(this, "TakePhotoFailed");
                }
            }, useCamera: true));
        }

        #endregion TakePhotoCommand

        #endregion Post Message

        private void AppendUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(_NewMessage))
            {
                NewMessage = url;
            }
            else
            {
                NewMessage += " " + url;
            }
        }

        #endregion Post Message

        #region ShowUnreadCommand

        private Command _ShowUnreadCommand;

        public Command ShowUnreadCommand
            => _ShowUnreadCommand ?? (_ShowUnreadCommand = new Command(ShowUnread));

        public async void ShowUnread()
        {
            var msg = _Messages?.OrderBy(m => m.Id).LastOrDefault();
            await BeginLoadMessages();
            SelectedMessage = msg;
        }

        #endregion ShowUnreadCommand

        #region ClearArchiveBannerCommand

        private bool _IsArchiveBannerShown;

        public bool IsArchiveBannerShown
        {
            get => _IsArchiveBannerShown;
            private set => SetProperty(ref _IsArchiveBannerShown, value);
        }

        private Command _ClearArchiveBannerCommand;

        public Command ClearArchiveBannerCommand
            => _ClearArchiveBannerCommand ?? (_ClearArchiveBannerCommand = new Command(ClearArchiveBanner));

        public void ClearArchiveBanner()
            => IsArchiveBannerShown = false;

        #endregion ClearArchiveBannerCommand

        #region Notification

        private bool _IsSubscribing;

        public bool IsSubscribing
        {
            get => _IsSubscribing;
            set => SetProperty(ref _IsSubscribing, value, onChanged: () =>
            {
                if (_IsSubscribing)
                {
                    MessagingCenter.Subscribe<ApplicationViewModel, Message>(this, "MessageCreated", OnMessageCreated, Application);
                }
                else
                {
                    MessagingCenter.Unsubscribe<ApplicationViewModel, Message>(this, "MessageCreated");
                    _Notifications?.Clear();
                }
            });
        }

        private void OnMessageCreated(ApplicationViewModel app, Message message)
        {
            HasUnread = Room.UnreadCount > 0;
            if (message.Room.Id == Room.Id)
            {
                return;
            }

            XDevice.BeginInvokeOnMainThread(() =>
            {
                var r = app.Rooms.FirstOrDefault(rm => rm.Id == message.Room.Id);

                if (r != null)
                {
                    if (Notifications.Count == 0)
                    {
                        XDevice.StartTimer(TimeSpan.FromSeconds(1), () =>
                        {
                            var dt = DateTime.Now.AddSeconds(-5);

                            for (int i = Notifications.Count - 1; i >= 0; i--)
                            {
                                if (Notifications[i].AddedAt < dt)
                                {
                                    Notifications.RemoveAt(i);
                                }
                            }

                            return Notifications.Count > 0;
                        });
                    }
                    Notifications.Insert(0, new Notification(Application, r, message));
                }
            });
        }

        public sealed class Notification
        {
            private ApplicationViewModel _Application;

            public Notification(ApplicationViewModel application, RoomViewModel room, Message message)
            {
                _Application = application;
                Room = room;
                Profile = application.GetProfile(message);
                AddedAt = DateTime.Now;
            }

            public ProfileViewModel Profile { get; }
            public RoomViewModel Room { get; }

            public DateTime AddedAt { get; }

            public override string ToString()
                => $"@{Profile.DisplayName} posted messages to {Room.DisplayName}";
        }

        private ObservableCollection<Notification> _Notifications;

        public ObservableCollection<Notification> Notifications
            => _Notifications ?? (_Notifications = new ObservableCollection<Notification>());

        private bool _HasUnread;

        public bool HasUnread
        {
            get => _HasUnread;
            private set => SetProperty(ref _HasUnread, value);
        }

        #endregion Notification
    }
}