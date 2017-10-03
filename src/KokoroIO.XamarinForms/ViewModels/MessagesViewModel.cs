using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Models.Data;
using Realms;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesViewModel : BaseViewModel
    {
        internal MessagesViewModel(ChannelViewModel channel)
        {
            Channel = channel;
            Title = channel.DisplayName;
            _IsArchiveBannerShown = channel.IsArchived;
            _HasUnread = Channel.UnreadCount > 0;

            PrependCommand = new Command(BeginPrepend);
            RefreshCommand = new Command(BeginAppend);

            Channel.PropertyChanged += Channel_PropertyChanged;
        }

        #region Channel

        public ChannelViewModel Channel { get; }

        private void Channel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Channel.IsArchived):
                    OnPropertyChanged(nameof(CanPost));
                    break;
            }
        }

        private ObservableRangeCollection<ProfileViewModel> _Members;

        public ObservableRangeCollection<ProfileViewModel> Members
        {
            get
            {
                if (_Members == null)
                {
                    _Members = Channel.Members;
                    _Members.CollectionChanged += (_, __) => ProfileCandicates.UpdateResult();
                }
                return _Members;
            }
        }

        #endregion Channel

        public ApplicationViewModel Application => Channel.Application;

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
            if (IsBusy || (Channel.IsArchived && Channel.UnreadCount <= 0 && !prepend && _Messages?.Count > 0))
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
                    bid = _Messages.Min(m => m.Id);
                    aid = null;
                }
                else
                {
                    bid = null;
                    aid = _Messages.Max(m => m.Id);
                }

                var messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, beforeId: bid, afterId: aid);

                HasPrevious &= aid != null || messages.Length >= PAGE_SIZE;

                if (aid != null)
                {
                    if (messages.Length >= PAGE_SIZE)
                    {
                        Channel.UnreadCount = Math.Max(0, Channel.UnreadCount - messages.Length);
                    }
                    else
                    {
                        Channel.UnreadCount = 0;
                    }
                }
                else if (bid == null && messages.Length < PAGE_SIZE)
                {
                    Channel.UnreadCount = 0;
                }
                HasUnread = Channel.UnreadCount > 0;

                if (messages.Any())
                {
                    InsertMessages(messages);
                }

                try
                {
                    var rid = Channel.Id;
                    using (var realm = Realm.GetInstance())
                    {
                        using (var trx = realm.BeginWrite())
                        {
                            var rup = realm.All<ChannelUserProperties>().FirstOrDefault(r => r.ChannelId == rid);
                            if (rup == null)
                            {
                                rup = new ChannelUserProperties()
                                {
                                    ChannelId = rid,
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
                    ex.Trace("SavingChannelUserPropertiesFailed");
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
                    if (m.IdempotentKey != null)
                    {
                        var cur = Messages.FirstOrDefault(cm => cm.IdempotentKey == m.IdempotentKey);

                        if (cur != null && (cur.Id == null || cur.Id == m.Id))
                        {
                            cur.Update(m);
                            var p = Messages.IndexOf(cur);
                            cur.SetIsMerged(Messages.ElementAtOrDefault(p - 1));

                            continue;
                        }
                    }
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

        #region SelectedProfile

        private ProfileViewModel _SelectedProfile;

        public ProfileViewModel SelectedProfile
        {
            get => _SelectedProfile;
            set => SetProperty(ref _SelectedProfile, value, onChanged: () => OnPropertyChanged(nameof(HasProfile)));
        }

        public bool HasProfile => _SelectedProfile != null;

        internal async void SelectProfile(string screenName)
        {
            Members.GetHashCode();
            await Channel.LoadMembersTask;

            SelectedProfile = Members.FirstOrDefault(p => p.ScreenName.Equals(screenName, StringComparison.OrdinalIgnoreCase));
        }

        private Command _ClearProfileCommand;

        public Command ClearProfileCommand
            => _ClearProfileCommand ?? (_ClearProfileCommand = new Command(() => SelectedProfile = null));

        #endregion SelectedProfile

        #endregion Showing Messages

        #region Post Message

        #region NewMessage

        private string _NewMessage = string.Empty;

        public string NewMessage
        {
            get => _NewMessage;
            set => SetProperty(ref _NewMessage, value, onChanged: UpdateCandicates);
        }

        #endregion NewMessage

        #region Candicates

        #region SelectionStart

        private int _SelectionStart;

        public int SelectionStart
        {
            get => _SelectionStart;
            set => SetProperty(ref _SelectionStart, value, onChanged: UpdateCandicates);
        }

        #endregion SelectionStart

        #region SelectionLength

        private int _SelectionLength;

        public int SelectionLength
        {
            get => _SelectionLength;
            set => SetProperty(ref _SelectionLength, value, onChanged: UpdateCandicates);
        }

        private void UpdateCandicates()
        {
            ProfileCandicates.UpdateResult();
            ChannelCandicates.UpdateResult();
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

        private MessagesProfileCandicates _ProfileCandicates;

        public MessagesProfileCandicates ProfileCandicates
            => _ProfileCandicates ?? (_ProfileCandicates = new MessagesProfileCandicates(this));

        private MessagesChannelCandicates _ChannelCandicates;

        public MessagesChannelCandicates ChannelCandicates
            => _ChannelCandicates ?? (_ChannelCandicates = new MessagesChannelCandicates(this));

        internal DateTime? CandicateClicked { get; set; }

        #endregion Candicates

        #region Post Message

        public bool CanPost
            => !Channel.IsArchived;

        #region PostCommand

        private Command _PostCommand;

        public Command PostCommand
            => _PostCommand ?? (_PostCommand = new Command(BeginPost));

        public async void BeginPost()
        {
            var m = _NewMessage;

            var succeeded = false;

            if (IsBusy || string.IsNullOrEmpty(m))
            {
                return;
            }

            MessageInfo tempMessage = null;
            try
            {
                IsBusy = true;
                tempMessage = new MessageInfo(this, m);
                NewMessage = string.Empty;
                var prev = Messages.LastOrDefault();
                tempMessage.SetIsMerged(prev);
                Messages.Add(tempMessage);
                await Application.PostMessageAsync(Channel.Id, m, _IsNsfw, idempotentKey: tempMessage.IdempotentKey.Value);
                succeeded = true;
            }
            catch (Exception ex)
            {
                if (tempMessage != null)
                {
                    Messages.Remove(tempMessage);

                    if (string.IsNullOrEmpty(NewMessage))
                    {
                        NewMessage = m;
                    }
                }

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

                // TODO: reset nsfw
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

        #region IsNsfw

        private bool _IsNsfw; // TODo: user default value

        public bool IsNsfw
        {
            get => _IsNsfw;
            set => SetProperty(ref _IsNsfw, value);
        }

        #endregion IsNsfw

        #region ToggleNsfwCommand

        private Command _ToggleNsfwCommand;

        public Command ToggleNsfwCommand
            => _ToggleNsfwCommand ?? (_ToggleNsfwCommand = new Command(() => IsNsfw = !_IsNsfw));

        #endregion ToggleNsfwCommand

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
            HasUnread = Channel.UnreadCount > 0;
            if (message.Channel.Id == Channel.Id)
            {
                return;
            }

            XDevice.BeginInvokeOnMainThread(() =>
            {
                var r = app.Channels.FirstOrDefault(rm => rm.Id == message.Channel.Id);

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

            public Notification(ApplicationViewModel application, ChannelViewModel channel, Message message)
            {
                _Application = application;
                Channel = channel;
                Profile = application.GetProfile(message);
                AddedAt = DateTime.Now;
            }

            public Profile Profile { get; }

            public ChannelViewModel Channel { get; }

            public DateTime AddedAt { get; }

            public override string ToString()
                => $"@{Profile.DisplayName} posted messages to {Channel.DisplayName}";
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