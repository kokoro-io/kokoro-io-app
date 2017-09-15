using System;
using System.Diagnostics;
using System.Linq;
using KokoroIO.XamarinForms.Helpers;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesViewModel : BaseViewModel
    {
        internal MessagesViewModel(RoomViewModel room)
        {
            Room = room;
            Title = room.DisplayName;

            PrependCommand = new Command(BeginPrepend);
            RefreshCommand = new Command(BeginAppend);
        }

        internal RoomViewModel Room { get; }

        internal ApplicationViewModel Application => Room.Application;

        public Command OpenUrlCommand => Application.OpenUrlCommand;

        private ObservableRangeCollection<MessageInfo> _Messages;

        public ObservableRangeCollection<MessageInfo> Messages
        {
            get
            {
                if (_Messages == null)
                {
                    _Messages = new ObservableRangeCollection<MessageInfo>();
                    BeginLoadMessages();
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
            => BeginLoadMessages(true);

        public void BeginAppend()
            => BeginLoadMessages(false);

        private async void BeginLoadMessages(bool prepend = false)
        {
            if (IsBusy)
            {
                return;
            }
            try
            {
                IsBusy = true;

                const int PAGE_SIZE = 60;

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
                                // TODO: update
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
            catch { }
            finally
            {
                IsBusy = false;
            }
        }

        public Command PrependCommand { get; set; }
        public Command RefreshCommand { get; set; }

        #region Post

        private string _NewMessage = string.Empty;

        public string NewMessage
        {
            get => _NewMessage;
            set => SetProperty(ref _NewMessage, value);
        }

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
                Debug.WriteLine(ex);
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

        #endregion Post
    }
}