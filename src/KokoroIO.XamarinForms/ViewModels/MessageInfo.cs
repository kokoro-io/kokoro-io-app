using System;
using System.Collections.Generic;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageInfo : ObservableObject
    {
        internal MessageInfo(MessagesViewModel page, Message message)
        {
            Page = page;
            Id = message.Id;
            Profile = page.Application.GetProfile(message);
            PublishedAt = message.PublishedAt;

            Content = message.Content;

            EmbedContents = message.EmbedContents;
        }

        private MessagesViewModel Page { get; }

        public int Id { get; }

        public ProfileViewModel Profile { get; }

        public DateTime PublishedAt { get; }

        public string Content { get; }
        public IList<EmbedContent> EmbedContents { get; }

        #region IsMerged

        private bool _IsMerged;

        public bool IsMerged
        {
            get => _IsMerged;
            private set => SetProperty(ref _IsMerged, value);
        }

        internal void SetIsMerged(MessageInfo prev)
        {
            IsMerged = prev?.Profile == Profile
                        && PublishedAt < prev.PublishedAt.AddMinutes(3);
        }

        #endregion IsMerged
    }
}