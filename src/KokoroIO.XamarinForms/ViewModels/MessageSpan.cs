using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{

    public sealed class MessageSpan
    {
        public MessageSpanType Type { get; set; }

        public string Text { get; set; }

        public static IEnumerable<MessageSpan> EnumerateSpans(XElement root)
        {
            foreach (var d in root.DescendantNodes())
            {
                if (d.NodeType == XmlNodeType.Text)
                {
                    var t = ((XText)d).Value;

                    var xe = d.Parent as XElement;

                    yield return new MessageSpan()
                    {
                        Text = t,
                        Type = "a".Equals(xe?.Name?.LocalName) ? MessageSpanType.Hyperlink : MessageSpanType.Default
                    };
                }
                else if (d.NodeType == XmlNodeType.Element)
                {
                    if ("br".Equals((d as XElement)?.Name.LocalName))
                    {
                        yield return new MessageSpan()
                        {
                            Text = Environment.NewLine,
                        };
                    }
                }
            }
        }
    }
}