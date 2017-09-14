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
                else if (maxId < _Messages.First().Id)
                {
                    var mvms = messages.OrderBy(m => m.Id).Select(m => new MessageInfo(this, m)).ToList();
                    for (var i = 1; i < mvms.Count; i++)
                    {
                        mvms[i].SetIsMerged(mvms[i - 1]);
                    }

                    _Messages[0].SetIsMerged(mvms.LastOrDefault());

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
                            var prev = i == 0 ? null : _Messages[i - 1];
                            var next = i >= _Messages.Count ? null : _Messages[i];

                            if (!(prev?.Id > vm.Id)
                                || !(vm.Id >= next?.Id))
                            {
                                _Messages.Insert(i, vm);
                                i++;

                                vm.SetIsMerged(prev);
                                next?.SetIsMerged(vm);
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
    }
}