using System;
using KokoroIO.XamarinForms.Models.Data;

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

        public async void Select()
        {
            var mp = _Popup._Parent;
            if (mp.IsImageHistoryVisible)
            {
                var tag = $"![]({ThumbnailOrRawUrl})";
                mp.AppendMessage(tag);
                mp.IsImageHistoryVisible = false;
                mp.ExpandsContents = false;

                using (var r = await RealmServices.GetInstanceAsync())
                using (var c = r.BeginWrite())
                {
                    var ih = r.Find<ImageHistory>(RawUrl);
                    if (ih != null)
                    {
                        ih.LastUsed = DateTimeOffset.Now;
                        c.Commit();
                    }
                }
            }
        }
    }
}