using System;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesRoomCandicates : EntityCandicates<RoomViewModel>
    {
        internal MessagesRoomCandicates(MessagesViewModel page)
            : base(page)
        {
        }

        internal override ObservableRangeCollection<RoomViewModel> Source
            => Page.Application.Rooms;

        internal override char PrefixChar => '#';

        internal override bool IsValidChar(char c)
            => !char.IsWhiteSpace(c);

        internal override IEnumerable<RoomViewModel> FilterByPrefix(string prefix)
            => Source.Where(r => r.Kind != RoomKind.DirectMessage && r.ChannelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        internal override string GetValue(RoomViewModel item)
            => item.ChannelName;
    }
}