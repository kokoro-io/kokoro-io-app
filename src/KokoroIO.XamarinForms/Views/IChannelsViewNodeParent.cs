using System.Collections.Generic;

namespace KokoroIO.XamarinForms.Views
{
    internal interface IChannelsViewNodeParent : IEnumerable<ChannelsViewNode>
    {
        void Add(ChannelsViewNode item);

        void Remove(ChannelsViewNode item);
    }
}