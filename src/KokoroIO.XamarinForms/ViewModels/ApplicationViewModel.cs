using System;
using System.Collections.Generic;
using System.Linq;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ApplicationViewModel
    {
        internal ApplicationViewModel(Client client)
        {
            Client = client;
            OpenUrlCommand = new Command(OpenUrl);
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

        #region Profiles

        private readonly Dictionary<string, ProfileViewModel> _Profiles = new Dictionary<string, ProfileViewModel>();

        internal ProfileViewModel GetProfile(Profile model)
        {
            var key = model.Id + '\0' + model.Avatar + '\0' + model.DisplayName;

            if (!_Profiles.TryGetValue(key, out var p))
            {
                p = new ProfileViewModel(model);
                _Profiles[key] = p;
            }
            return p;
        }

        #endregion Profiles

        public Command OpenUrlCommand { get; }

        private void OpenUrl(object url)
        {
            var u = url as Uri ?? (url is string s ? new Uri(s) : null);

            if (u != null)
            {
                Device.OpenUri(u);
            }
        }
    }
}