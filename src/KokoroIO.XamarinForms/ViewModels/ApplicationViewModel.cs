using System.Linq;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ApplicationViewModel
    {
        internal ApplicationViewModel(Client client)
        {
            Client = client;
        }

        internal Client Client { get; }

        #region Rooms

        private bool _RoomsLoaded;

        private ObservableRangeCollection<RoomViewModel> _Rooms;

        public ObservableRangeCollection<RoomViewModel> Rooms
            => InitRooms()._Rooms;

        private ApplicationViewModel InitRooms()
        {
            if (_Rooms == null)
            {
                _Rooms = new ObservableRangeCollection<RoomViewModel>();
            }

            if (!_RoomsLoaded)
            {
                _RoomsLoaded = true;
                LoadRooms();
            }

            return this;
        }

        private async void LoadRooms()
        {
            var rooms = await Client.GetRoomsAsync();
            foreach (var r in rooms.OrderBy(e => (int)e.Kind).ThenBy(e => e.ChannelName).ThenBy(e => e.Id))
            {
                var rvm = new RoomViewModel(this, r);

                _Rooms.Add(rvm);
            }
        }

        #endregion Rooms
    }

}