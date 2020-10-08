using System;

namespace KokoroIO.XamarinForms.Models.Data
{
    public class ImageHistory
    {
        public string RawUrl { get; set; }

        public string Group { get; set; }

        public int DisplayIndex { get; set; }

        public string Title { get; set; }

        public string ThumbnailUrl { get; set; }

        public DateTimeOffset LastUsed { get; set; }

        public bool IsFavored { get; set; }
    }
}