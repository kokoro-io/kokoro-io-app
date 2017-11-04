using System;
using System.Collections.Generic;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class MessagesViewMessage
    {
        public int? Id { get; set; }
        public Guid? IdempotentKey { get; set; }
        public string ProfileId { get; set; }
        public string Avatar { get; set; }
        public string ScreenName { get; set; }
        public string DisplayName { get; set; }
        public bool IsBot { get; set; }
        public DateTime? PublishedAt { get; set; }

        public bool IsNsfw { get; set; }
        public string HtmlContent { get; set; }
        public bool IsMerged { get; set; }
        public IList<EmbedContent> EmbedContents { get; set; }
        public bool CanDelete { get; set; }
        public bool IsDeleted { get; set; }
    }
}