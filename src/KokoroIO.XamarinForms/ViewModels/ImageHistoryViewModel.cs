using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.Models.Data;
using System;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ImageHistoryViewModel : ObservableObject
    {
        private readonly ImageHistoryPopupViewModel _Popup;

        internal ImageHistoryViewModel(ImageHistoryPopupViewModel popup, ImageHistory model)
        {
            _Popup = popup;
            Title = model.Title;
            RawUrl = model.RawUrl;
            ThumbnailUrl = model.ThumbnailUrl;
        }

        public string Title { get; }
        public string RawUrl { get; }
        public string ThumbnailUrl { get; }
        public string ThumbnailOrRawUrl => ThumbnailUrl ?? RawUrl;

        public void Select()
        {
            var mp = _Popup._Parent;
            if (mp.IsImageHistoryVisible)
            {
                var tag = $"[]({RawUrl})";
                mp.AppendMessage(tag);
                mp.IsImageHistoryVisible = false;
                mp.ExpandsContents = true;

                var imgs = UserSettings.GetImageHistories();
                var ih = imgs.FirstOrDefault(e => e.RawUrl == RawUrl);
                if (ih != null)
                {
                    ih.LastUsed = DateTimeOffset.Now;
                    UserSettings.SetImageHistories(imgs);
                }
            }
        }
    }
}