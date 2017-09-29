using System;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesChannelCandicates : EntityCandicates<ChannelViewModel>
    {
        internal MessagesChannelCandicates(MessagesViewModel page)
            : base(page)
        {
        }

        internal override ObservableRangeCollection<ChannelViewModel> Source
            => Page.Application.Channels;

        internal override char PrefixChar => '#';

        internal override bool IsValidChar(char c)
            => !char.IsWhiteSpace(c);

        internal override IEnumerable<ChannelViewModel> FilterByPrefix(string prefix)
            => Source.Where(r => r.Kind != ChannelKind.DirectMessage && r.ChannelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        internal override string GetValue(ChannelViewModel item)
            => item.ChannelName;
    }
}