using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.Helpers;
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
        }

        public RoomViewModel Room { get; }

        public ApplicationViewModel Application => Room.Application;

        public Command OpenUrlCommand => Application.OpenUrlCommand;

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

                if (!messages.Any())
                {
                    return;
                }

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

        internal void UpdateMessage(Message message)
            => _Messages?.FirstOrDefault(m => m.Id == message.Id)?.Update(message);

        public Command PrependCommand { get; set; }
        public Command RefreshCommand { get; set; }

        #region Post

        private string _NewMessage = string.Empty;

        public string NewMessage
        {
            get => _NewMessage;
            set => SetProperty(ref _NewMessage, value);
        }

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
            => Application.MediaPicker != null;

        private Command _UploadImageCommand;

        public Command UploadImageCommand
            => _UploadImageCommand ?? (_UploadImageCommand = new Command(p => BeginUploadImage(p as Stream)));

        public void BeginUploadImage(Stream data = null)
        {
            Application.BeginUpload(new UploadParameter(url =>
            {
                if (string.IsNullOrWhiteSpace(_NewMessage))
                {
                    NewMessage = url;
                }
                else
                {
                    NewMessage += " " + url;
                }
            }, error =>
            {
                data?.Dispose();

                if (error != null)
                {
                    MessagingCenter.Send(this, "UploadImageFailed");
                }
            }, data: data));
        }

        #endregion UploadImageCommand

        #endregion Post

        #region SelectedMessage

        private MessageInfo _SelectedMessage;

        public MessageInfo SelectedMessage
        {
            get => _SelectedMessage;
            set => SetProperty(ref _SelectedMessage, value);
        }

        #endregion SelectedMessage

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