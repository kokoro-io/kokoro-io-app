using System;
using System.Collections.Generic;
using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class MessagesViewMessage
    {
        public MessagesViewMessage(MessageInfo message)
        {
            Id = message.Id;
            IdempotentKey = message.IdempotentKey;

            ProfileId = message.Profile.Id;
            Avatar = message.DisplayAvatar;
            ScreenName = message.Profile.ScreenName;
            DisplayName = message.Profile.DisplayName;
            IsBot = message.Profile.Type == ProfileType.Bot;
            PublishedAt = message.PublishedAt;
            IsNsfw = message.IsNsfw;
            Content = message.Content;
            IsMerged = message.IsMerged;
            EmbedContents = message.EmbedContents;
            IsDeleted = message.IsDeleted;
            CanDelete = message.CanDelete;
        }

        public int? Id { get; }
        public Guid? IdempotentKey { get; }
        public string ProfileId { get; }
        public string Avatar { get; }
        public string ScreenName { get; }
        public string DisplayName { get; }
        public bool IsBot { get; }
        public DateTime? PublishedAt { get; }

        public bool IsNsfw { get; }
        public string Content { get; }
        public bool IsMerged { get; }
        public IList<EmbedContent> EmbedContents { get; }
        public bool CanDelete { get; }
        public bool IsDeleted { get; }
    }
}