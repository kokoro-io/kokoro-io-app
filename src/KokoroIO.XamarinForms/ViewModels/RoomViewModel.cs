using System;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class RoomViewModel : ObservableObject
    {
        internal RoomViewModel(ApplicationViewModel application, Room model)
        {
            Application = application;
            Id = model.Id;
            ChannelName = model.ChannelName;
            Kind = model.Kind;
            IsArchived = model.IsArchived;
        }

        internal ApplicationViewModel Application { get; }

        public string Id { get; }
        public string ChannelName { get; }

        public string DisplayName
            => (Kind == RoomKind.DirectMessage ? '@' : '#') + ChannelName;

        public string Placeholder
            => "Let's talk" + (Kind == RoomKind.DirectMessage ? " to " : " at ") + ChannelName;

        public RoomKind Kind { get; }
        public bool IsArchived { get; }

        public string KindName
        {
            get
            {
                switch (Kind)
                {
                    case RoomKind.PublicChannel:
                        return "Public Channels";

                    case RoomKind.PrivateChannel:
                        return "Private Channels";

                    case RoomKind.DirectMessage:
                        return "Direct Messages";
                }

                return Kind.ToString();
            }
        }

        #region MessagesPage

        private WeakReference<MessagesViewModel> _MessagesPage;

        public MessagesViewModel MessagesPage
            => _MessagesPage != null && _MessagesPage.TryGetTarget(out var mp) ? mp : null;

        public MessagesViewModel GetOrCreateMessagesPage()
        {
            var r = MessagesPage;
            if (r == null)
            {
                r = new MessagesViewModel(this);
                _MessagesPage = new WeakReference<MessagesViewModel>(r);
            }
            return r;
        }

        #endregion MessagesPage

        #region UnreadCount

        private int _UnreadCount;

        public int UnreadCount
        {
            get => _UnreadCount;
            set => SetProperty(ref _UnreadCount, value);
        }

        #endregion UnreadCount
    }
}