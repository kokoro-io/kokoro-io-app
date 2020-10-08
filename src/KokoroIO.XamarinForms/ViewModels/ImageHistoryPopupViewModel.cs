using KokoroIO.XamarinForms.Models;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ImageHistoryPopupViewModel : ObservableObject
    {
        internal readonly MessagesViewModel _Parent;

        internal ImageHistoryPopupViewModel(MessagesViewModel parent)
        {
            _Parent = parent;
        }

        private ObservableRangeCollection<ImageHistoryViewModel> _Images;

        public ObservableRangeCollection<ImageHistoryViewModel> Images
        {
            get
            {
                if (_Images == null)
                {
                    _Images = new ObservableRangeCollection<ImageHistoryViewModel>(
                        UserSettings.GetImageHistories()
                                       .OrderBy(e => e.IsFavored ? 0 : 1)
                                       .ThenByDescending(e => e.LastUsed)
                                       .Select(e => new ImageHistoryViewModel(this, e)));
                }
                return _Images;
            }
        }
    }
}