using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageSpan
    {
        public string Text { get; set; }

        public static IEnumerable<MessageSpan> EnumerateSpans(XElement root)
        {
            foreach (var d in root.DescendantNodes())
            {
                if (d.NodeType == XmlNodeType.Text)
                {
                    var t = ((XText)d).Value;

                    yield return new MessageSpan()
                    {
                        Text = t,
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