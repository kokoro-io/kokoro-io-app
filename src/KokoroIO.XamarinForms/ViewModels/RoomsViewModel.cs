using KokoroIO.XamarinForms.Helpers;

namespace KokoroIO.XamarinForms.ViewModels
{
    public class RoomsViewModel : BaseViewModel
    {
        internal RoomsViewModel(ApplicationViewModel application)
        {
            Application = application;

            Title = "Rooms";
        }

        public ApplicationViewModel Application { get; }

        public ObservableRangeCollection<RoomViewModel> Rooms
            => Application.Rooms;
    }
}