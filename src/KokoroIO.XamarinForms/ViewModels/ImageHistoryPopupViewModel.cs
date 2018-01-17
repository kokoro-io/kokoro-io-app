using System.Linq;
using KokoroIO.XamarinForms.Models.Data;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ImageHistoryPopupViewModel : BindableObject
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
                    _Images = new ObservableRangeCollection<ImageHistoryViewModel>();
                    BeginLoad();
                }
                return _Images;
            }
        }

        private async void BeginLoad()
        {
            using (var r = await RealmServices.GetInstanceAsync())
            {
                var his = r.All<ImageHistory>().ToList();
                _Images.ReplaceRange(his.OrderBy(e => e.IsFavored ? 0 : 1).ThenByDescending(e => e.LastUsed).Select(e => new ImageHistoryViewModel(this, e)));
            }
        }
    }
}