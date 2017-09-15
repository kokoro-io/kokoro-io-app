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

            _Content = message.Content;

            _EmbedContents = message.EmbedContents;
        }

        internal void Update(Message message)
        {
            Content = message.Content;
            EmbedContents = message.EmbedContents;
        }

        private MessagesViewModel Page { get; }

        public int Id { get; }

        public ProfileViewModel Profile { get; }

        public DateTime PublishedAt { get; }

        #region Content

        private string _Content;

        public string Content
        {
            get => _Content;
            private set => SetProperty(ref _Content, value);
        }

        #endregion Content

        #region Content

        private IList<EmbedContent> _EmbedContents;

        public IList<EmbedContent> EmbedContents
        {
            get => _EmbedContents;
            private set => SetProperty(ref _EmbedContents, value);
        }

        #endregion Content

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