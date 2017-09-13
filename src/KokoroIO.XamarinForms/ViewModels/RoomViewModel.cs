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
        }

        internal ApplicationViewModel Application { get; }

        public string Id { get; }
        public string ChannelName { get; }

        public string DisplayName
            => (Kind == RoomKind.DirectMessage ? '@' : '#') + ChannelName;

        public RoomKind Kind { get; }

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
    }
}