using System.Collections.Generic;
using System.Xml.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ListMessageBlock : MessageBlockBase
    {
        internal ListMessageBlock(MessageInfo message, XElement element)
            : base(message)
        {
            _Items = new List<MessageListItem>();

            foreach (var li in element.Elements("li"))
            {
                var ivm = new MessageListItem();

                ivm.Spans.AddRange(MessageSpan.EnumerateSpans(li));

                _Items.Add(ivm);
            }
        }

        private readonly List<MessageListItem> _Items;

        public List<MessageListItem> Items => _Items;
    }
}