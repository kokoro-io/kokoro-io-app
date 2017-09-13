using System.Collections.Generic;
using KokoroIO.XamarinForms.Helpers;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageListItem : ObservableObject
    {
        private readonly List<MessageSpan> _Spans = new List<MessageSpan>();

        public List<MessageSpan> Spans => _Spans;
    }
}