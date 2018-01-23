using KokoroIO.XamarinForms.Views;

namespace KokoroIO.XamarinForms.ViewModels
{
    internal static class MessageInfoHelper
    {
        public static MessagesViewMergeInfo ToMergeInfo(this MessageInfo message)
            => new MessagesViewMergeInfo()
            {
                Id = message.Id,
                IsMerged = message.IsMerged
            };

        public static MessagesViewMessage ToJson(this MessageInfo message)
            => new MessagesViewMessage()
            {
                Id = message.Id,
                IdempotentKey = message.IdempotentKey,

                ProfileId = message.Profile.Id,
                Avatar = message.DisplayAvatar,
                ScreenName = message.Profile.ScreenName,
                DisplayName = message.Profile.DisplayName,
                IsBot = message.Profile.Type == ProfileType.Bot,
                PublishedAt = message.PublishedAt,
                IsNsfw = message.IsNsfw,
                HtmlContent = message.HtmlContent,
                IsMerged = message.IsMerged,
                EmbedContents = message.ExpandEmbedContents ? message.EmbedContents : null,
                IsDeleted = message.IsDeleted,
                CanDelete = message.CanDelete
            };
    }
}