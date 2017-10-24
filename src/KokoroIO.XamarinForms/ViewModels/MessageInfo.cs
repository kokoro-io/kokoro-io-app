using KokoroIO.XamarinForms.Models;
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

            Content = message.Content;

            _EmbedContents = message.EmbedContents?.Count > 1
                            ? message.EmbedContents.OrderBy(c => c.Position).ToArray()
                            : message.EmbedContents;
            _Status = message.Status;
        }

        internal MessageInfo(MessagesViewModel page, string content)
        {
            Page = page;
            IdempotentKey = Guid.NewGuid();
            Profile = page.Application.LoginUser.ToProfile();

            Content = content.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        internal void Update(Message message)
        {
            Id = message.Id;
            Profile = Page.Application.GetProfile(message);
            PublishedAt = message.PublishedAt;
            IsNsfw = message.IsNsfw;
            Content = message.Content;
            EmbedContents = message.EmbedContents;
            _Status = message.Status;
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
            private set => SetProperty(
                ref _Content,
                MessageHelper.InsertLinks(
                    value,
                    c => Page.Application.Channels.FirstOrDefault(v => v.ChannelName.Equals(c, StringComparison.OrdinalIgnoreCase))?.Id,
                    s => Page.Members.FirstOrDefault(v => v.ScreenName.Equals(s, StringComparison.OrdinalIgnoreCase))?.Id));
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
            IsMerged = (prev?.Profile == Profile
                            || (prev?.Profile.Id == Profile.Id
                                && prev?.Profile.ScreenName == Profile.ScreenName
                                && prev?.Profile.DisplayName == Profile.DisplayName
                                && prev?.Profile.Avatar == Profile.Avatar))
                        && (PublishedAt ?? DateTime.UtcNow) < prev.PublishedAt?.AddMinutes(3);
        }

        #endregion IsMerged

        #region IsShown

        private bool _IsShown;

        public bool IsShown
        {
            get => _IsShown;
            set => SetProperty(ref _IsShown, value, onChanged: () =>
            {
                if (_IsShown && !(Page.Channel.LastReadId >= Id))
                {
                    Page.Channel.LastReadId = Id;
                }
            });
        }

        #endregion IsShown

        private MessageStatus _Status;

        public bool CanDelete => !IsDeleted && Profile.Id == Page.Application.LoginUser.Id;

        public bool IsDeleted => _Status != MessageStatus.Active;

        public async void BeginDelete()
        {
            if (Id == null)
            {
                return;
            }
            try
            {
                await Page.Application.DeleteMessageAsync(Id.Value);
            }
            catch (Exception ex)
            {
                ex.Trace("DeleteMessageFailed");
            }
        }
    }
}