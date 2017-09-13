using KokoroIO.XamarinForms.Helpers;

namespace KokoroIO.XamarinForms.ViewModels
{
    public class RoomsViewModel : BaseViewModel
    {
        internal ApplicationViewModel Application { get; }

        internal RoomsViewModel(ApplicationViewModel application)
        {
            Application = application;

            Title = "Rooms";
        }

        public ObservableRangeCollection<RoomViewModel> Rooms
            => Application.Rooms;
    }
}