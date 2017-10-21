using System.Collections;
using System.Collections.Generic;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class ChannelsViewKind : IChannelsViewNodeParent
    {
        private readonly List<ChannelsViewNode> _Children = new List<ChannelsViewNode>();

        public string Title { get; set; }
        public ChannelKind Kind { get; set; }

        internal List<ChannelsViewNode> Descendants { get; } = new List<ChannelsViewNode>();

        public void Add(ChannelsViewNode item)
        {
            _Children.Add(item);
        }

        public void Remove(ChannelsViewNode item)
        {
            _Children.Remove(item);
        }

        public IEnumerator<ChannelsViewNode> GetEnumerator()
            => _Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _Children.GetEnumerator();
    }
}