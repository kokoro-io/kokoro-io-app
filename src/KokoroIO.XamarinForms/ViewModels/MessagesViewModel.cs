using System.Collections.ObjectModel;
using System.Linq;
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

        private ObservableCollection<MessageInfo> _Messages;

        public ObservableCollection<MessageInfo> Messages
        {
            get
            {
                if (_Messages == null)
                {
                    _Messages = new ObservableCollection<MessageInfo>();
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