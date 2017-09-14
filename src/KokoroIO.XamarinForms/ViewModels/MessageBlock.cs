using System.Collections.Generic;
using System.Xml.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageBlock : MessageBlockBase
    {
        internal MessageBlock(MessageInfo message)
            : base(message)
        {
        }

        internal MessageBlock(MessageInfo message, XElement element)
            : base(message)
        {
            _Spans.AddRange(MessageSpan.EnumerateSpans(Message, element));
        }

        private readonly List<MessageSpan> _Spans = new List<MessageSpan>();

        public List<MessageSpan> Spans => _Spans;
    }
}