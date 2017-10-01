using System;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessagesProfileCandicates : EntityCandicates<ProfileViewModel>
    {
        internal MessagesProfileCandicates(MessagesViewModel page)
            : base(page)
        {
        }

        internal override ObservableRangeCollection<ProfileViewModel> Source
            => Page.Members;

        internal override char PrefixChar => '@';

        internal override bool IsValidChar(char c)
            => ('A' <= c && c <= 'Z')
            || ('a' <= c && c <= 'z')
            || ('0' <= c && c <= '9');

        internal override IEnumerable<ProfileViewModel> FilterByPrefix(string prefix)
            => Source.Where(p => p.ScreenName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        internal override string GetValue(ProfileViewModel item)
            => item.ScreenName;
    }
}