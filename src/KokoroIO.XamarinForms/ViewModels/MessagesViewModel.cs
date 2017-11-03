using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesViewModel : BaseViewModel
    {
        internal MessagesViewModel(ChannelViewModel channel)
        {
            Channel = channel;
            Title = channel.DisplayName;
            _IsArchiveBannerShown = channel.IsArchived;

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

        private int? _MaxId;
        private int? _MinId;

        #region Messages

        private InsertableObservableRangeCollection<MessageInfo> _Messages;

        public ObservableRangeCollection<MessageInfo> Messages
        {
            get
            {
                if (_Messages == null)
                {
                    _Messages = new InsertableObservableRangeCollection<MessageInfo>();
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

        #region HasNext

        private bool _HasNext = true;

        public bool HasNext
        {
            get => _HasNext;
            private set => SetProperty(ref _HasNext, value);
        }

        #endregion HasNext

        public void BeginPrepend()
            => BeginLoadMessages(true).GetHashCode();

        public void BeginAppend()
            => BeginLoadMessages(false).GetHashCode();

        internal bool MessagesLoaded { get; private set; }

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

                // int? bid, aid;
                Message[] messages;

                if (Messages.Count == 0)
                {
                    // initlial loading

                    messages = null;

                    // TODO: get first message id from argument

                    var tid = _SelectedMessageId ?? Channel.LastReadId;

                    if (tid > 0)
                    {
                        var afts = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, afterId: tid);

                        if (afts.Any())
                        {
                            var befores = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, beforeId: afts.Min(m => m.Id));

                            messages = befores.Any() ? befores.Concat(afts).ToArray() : afts;
                            HasNext = befores.Length == PAGE_SIZE;
                            HasPrevious = befores.Length == PAGE_SIZE;
                        }
                    }

                    if (messages == null)
                    {
                        messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE);
                    }
                }
                else if (prepend)
                {
                    if (!HasPrevious)
                    {
                        return;
                    }

                    messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, beforeId: _MinId);
                    HasPrevious = messages.Length == PAGE_SIZE;
                }
                else
                {
                    messages = await Application.GetMessagesAsync(Channel.Id, PAGE_SIZE, afterId: _MaxId);

                    HasNext = messages.Length == PAGE_SIZE;
                }

                MessagesLoaded = true;

                if (messages.Any())
                {
                    Channel.Members.GetHashCode();
                    await Channel.LoadMembersTask;

                    _MinId = Math.Min(messages.Min(m => m.Id), _MinId ?? int.MaxValue);
                    _MaxId = Math.Max(messages.Max(m => m.Id), _MaxId ?? int.MinValue);
                    InsertMessages(messages);

                    if (_SelectedMessageId != null)
                    {
                        SelectedMessage = Messages.FirstOrDefault(m => m.Id == _SelectedMessageId) ?? _SelectedMessage;
                    }
                }
                _SelectedMessageId = null;

                if (!HasNext && _Messages?.LastOrDefault().IsShown != false)
                {
                    await Channel.ClearUnreadAsync().ConfigureAwait(false);
                }

                Channel.BeginWriteRealm(null);
            }
            catch (Exception ex)
            {
                ex.Error("Load message failed");

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

            if (!_Messages.Any() || minId > _Messages.Last().Id)
            {
                var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                for (var i = 0; i < mvms.Count; i++)
                {
                    mvms[i].SetIsMerged(i == 0 ? _Messages.LastOrDefault() : mvms[i - 1]);
                }

                _Messages.AddRange(mvms);
            }
            else if (maxId < _Messages.First().Id)
            {
                var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                MessageInfo prev = null;
                for (var i = 0; i < mvms.Count; i++)
                {
                    mvms[i].SetIsMerged(prev);
                    prev = mvms[i];
                }
                _Messages[0].SetIsMerged(prev);
                _Messages.InsertRange(0, mvms);
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
                            vm.SetIsMerged(Messages.ElementAtOrDefault(i - 1));
                            _Messages.Insert(i, vm);
                            prev.SetIsMerged(vm);
                            break;
                        }
                        else if (m.Id == prev.Id)
                        {
                            prev.Update(m);
                            break;
                        }
                        else if (i + 1 >= _Messages.Count)
                        {
                            vm.SetIsMerged(_Messages.LastOrDefault());
                            _Messages.Add(vm);
                            break;
                        }
                        else
                        {
                            var next = _Messages[i + 1];

                            if (m.Id < next.Id)
                            {
                                vm.SetIsMerged(prev);
                                _Messages.Insert(i + 1, vm);
                                next.SetIsMerged(vm);

                                break;
                            }
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
            set => SetProperty(ref _SelectedMessage, value, onChanged: () => _SelectedMessageId = null);
        }

        private int? _SelectedMessageId;

        internal int? SelectedMessageId
        {
            get => _SelectedMessageId ?? _SelectedMessage?.Id;
            set
            {
                if (value == SelectedMessageId)
                {
                    return;
                }

                if (value == null)
                {
                    _SelectedMessageId = null;
                    SelectedMessage = null;
                }
                else
                {
                    var m = _Messages.FirstOrDefault(e => e.Id == value);
                    if (m == null)
                    {
                        _SelectedMessageId = value;
                        BeginLoadMessages().GetHashCode();
                    }
                    else
                    {
                        _SelectedMessageId = null;
                        SelectedMessage = m;
                    }
                }
            }
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

        internal async void SelectProfile(string screenName, string profileId)
        {
            Members.GetHashCode();
            await Channel.LoadMembersTask;

            SelectedProfile = Members.FirstOrDefault(p => p.Id == profileId)
                            ?? Members.FirstOrDefault(p => p.ScreenName.Equals(screenName, StringComparison.OrdinalIgnoreCase));

            // TODO: load from API
        }

        private Command _ClearPopupCommand;

        public Command ClearPopupCommand
            => _ClearPopupCommand ?? (_ClearPopupCommand = new Command(() =>
            {
                SelectedProfile = null;
                PopupUrl = null;
            }));

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

                ex.Error("Post message failed");

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
                IsBusy = false;

                data?.Dispose();

                if (error != null)
                {
                    MessagingCenter.Send(this, "UploadImageFailed");
                }
            }, onUploading: () => IsBusy = true, data: data));
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
                IsBusy = false;

                if (error != null)
                {
                    MessagingCenter.Send(this, "TakePhotoFailed");
                }
            }, onUploading: () => IsBusy = true, useCamera: true));
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
            IsBusy = false;

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
            SelectedMessage = msg == null ? _Messages.FirstOrDefault()
                            : (_Messages.SkipWhile(m => m != msg).Skip(1).FirstOrDefault() ?? msg);
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

        private string _PopupUrl;

        public string PopupUrl
        {
            get => _PopupUrl;
            private set => SetProperty(ref _PopupUrl, value);
        }

        public bool OpenUrl(Uri u)
        {
            // TODO: support dedicated urls

            //if (u.Host == "www.youtube.com"
            //    && u.AbsolutePath == "/watch")
            //{
            //    if (u.Query.ParseQueryString().TryGetValue("v", out var v))
            //    {
            //        PopupUrl = $"https://www.youtube.com/embed/{v}";

            //        return true;
            //    }
            //}

            return false;
        }
    }
}