using System;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageInfo : ObservableObject
    {
        internal MessageInfo(MessagesViewModel page, Message message)
        {
            Page = page;
            _Id = message.Id;
            IdempotentKey = message.IdempotentKey.HasValue && message.IdempotentKey.Value != default(Guid) ? message.IdempotentKey : null;
            Profile = page.Application.GetProfile(message);
            _PublishedAt = message.PublishedAt;
            _IsNsfw = message.IsNsfw;

            _Content = message.Content;

            _EmbedContents = message.EmbedContents;
        }
        internal MessageInfo(MessagesViewModel page, string content)
        {
            Page = page;
            IdempotentKey = Guid.NewGuid();
            Profile = page.Application.LoginUser.ToProfile();

            _Content = content.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        internal void Update(Message message)
        {
            Id = message.Id;
            Profile = Page.Application.GetProfile(message);
            PublishedAt = message.PublishedAt;
            IsNsfw = message.IsNsfw;
            Content = message.Content;
            EmbedContents = message.EmbedContents;
        }

        private MessagesViewModel Page { get; }

        #region Id

        private int? _Id;

        public int? Id
        {
            get => _Id;
            private set => SetProperty(ref _Id, value);
        }

        #endregion Id

        public Guid? IdempotentKey { get; }

        #region Profile

        private Profile _Profile;

        public Profile Profile
        {
            get => _Profile;
            private set => SetProperty(ref _Profile, value);
        }

        #endregion Profile

        public string DisplayAvatar
        {
            get
            {
                if (Profile.Avatars != null)
                {
                    var s = Page.Application.DisplayScale * 40;
                    return Profile.Avatars.OrderBy(a => a.Size).Where(a => a.Size >= s).FirstOrDefault()?.Url
                            ?? Profile.Avatars.OrderByDescending(a => a.Size).FirstOrDefault()?.Url
                            ?? Profile.Avatar;
                }

                return Profile.Avatar;
            }
        }

        #region PublishedAt

        private DateTime? _PublishedAt;

        public DateTime? PublishedAt
        {
            get => _PublishedAt;
            private set => SetProperty(ref _PublishedAt, value);
        }

        #endregion PublishedAt

        #region IsNsfw

        private bool _IsNsfw;

        public bool IsNsfw
        {
            get => _IsNsfw;
            private set => SetProperty(ref _IsNsfw, value);
        }

        #endregion IsNsfw

        #region Content

        private string _Content;

        public string Content
        {
            get => _Content;
            private set => SetProperty(ref _Content, value);
        }

        #endregion Content

        #region EmbedContents

        private IList<EmbedContent> _EmbedContents;

        public IList<EmbedContent> EmbedContents
        {
            get => _EmbedContents;
            private set => SetProperty(ref _EmbedContents, value);
        }

        #endregion EmbedContents

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
                        && PublishedAt < prev.PublishedAt?.AddMinutes(3);
        }

        #endregion IsMerged
    }
}