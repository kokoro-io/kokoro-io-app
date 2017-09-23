using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploadToRoomViewModel : BaseViewModel
    {
        internal UploadToRoomViewModel(ApplicationViewModel application, Func<Stream> streamCreator)
        {
            Application = application;
            StreamCreator = streamCreator;
            if (application.Rooms.Any())
            {
                _Rooms = new ObservableCollection<RoomViewModel>(application.Rooms.Where(r => !r.IsArchived));
            }
        }

        public ApplicationViewModel Application { get; }

        internal Func<Stream> StreamCreator { get; }

        private ImageSource _Image;

        public ImageSource Image
            => _Image ?? (_Image = ImageSource.FromStream(StreamCreator));

        private ObservableCollection<RoomViewModel> _Rooms;

        public ObservableCollection<RoomViewModel> Rooms
        {
            get
            {
                InitRooms();
                return _Rooms;
            }
        }

        private async void InitRooms()
        {
            if (_Rooms == null)
            {
                _Rooms = new ObservableCollection<RoomViewModel>();

                var rooms = await Application.GetRoomsAsync(false);

                foreach (var r in rooms)
                {
                    var rvm = Application.Rooms.FirstOrDefault(rm => rm.Id == r.Id);
                    if (rvm != null)
                    {
                        _Rooms.Add(rvm);
                    }
                }
            }
        }
    }
}