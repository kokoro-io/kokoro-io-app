using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommonMark;
using CommonMark.Formatters;
using CommonMark.Syntax;

namespace KokoroIO.XamarinForms.Models
{
    internal static class MessageHelper
    {
        public static string MarkdownToHtml(string markdown)
        {
            try
            {
                var b = CommonMarkConverter.Parse(markdown);
                foreach (var c in b.AsEnumerable())
                {
                    if (c.Inline != null)
                    {
                        switch (c.Inline.Tag)
                        {
                            case InlineTag.Link:
                            case InlineTag.Image:
                            case InlineTag.Strikethrough:
                            case InlineTag.Emphasis:
                            case InlineTag.Strong:
                                c.Inline.Tag = InlineTag.Strong;
                                break;
                        }
                    }
                }
                using (var sw = new StringWriter())
                {
                    new HtmlFormatter(sw, CommonMarkSettings.Default).WriteDocument(b);

                    return sw.ToString();
                }
            }
            catch { }

            return markdown?.Replace("<", "&lt;").Replace(">", "&gt;").Replace(Environment.NewLine, "<br />");
        }

        private static readonly Regex _LinkStart = new Regex("(@[-_a-z0-9]+|#[-_a-z0-9.]+)", RegexOptions.IgnoreCase);

        public static string InsertLinks(string content, Func<string, string> channelNameResolver, Func<string, string> screenNameResolver)
        {
            if (content == null || !_LinkStart.IsMatch(content))
            {
                return content;
            }

            try
            {
                using (var sr = new StringReader(content.Replace("<br>", "<br />")))
                using (var xr = XmlReader.Create(sr, new XmlReaderSettings()
                {
                    ConformanceLevel = ConformanceLevel.Fragment,
                    CheckCharacters = false,
#if !WINDOWS_UWP
                    ValidationType = ValidationType.None,
#endif
                }))
                using (var sw = new StringWriter(new StringBuilder(content.Length + 16)))
                using (var xw = XmlWriter.Create(sw, new XmlWriterSettings()
                {
                    CheckCharacters = false,
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment
                }))
                {
                    var inAnchor = false;
                    while (xr.Read())
                    {
                        switch (xr.NodeType)
                        {
                            case XmlNodeType.Element:
                                var empty = xr.IsEmptyElement;

                                if (!empty)
                                {
                                    inAnchor |= "a".Equals(xr.LocalName, StringComparison.OrdinalIgnoreCase);
                                }
                                xw.WriteStartElement(xr.Prefix, xr.LocalName, xr.NamespaceURI);

                                while (xr.MoveToNextAttribute())
                                {
                                    xw.WriteAttributeString(xr.LocalName, xr.NamespaceURI, xr.Value);
                                }

                                if (empty)
                                {
                                    xw.WriteEndElement();
                                }

                                continue;

                            case XmlNodeType.Attribute:
                                xw.WriteAttributeString(xr.LocalName, xr.NamespaceURI, xr.Value);
                                continue;

                            case XmlNodeType.EndElement:
                                inAnchor = inAnchor && !"a".Equals(xr.LocalName, StringComparison.OrdinalIgnoreCase);
                                xw.WriteEndElement();
                                continue;

                            case XmlNodeType.Text:
                                WriteTextNode(xr, xw, inAnchor, channelNameResolver, screenNameResolver);
                                continue;

                            case XmlNodeType.Whitespace:
                                xw.WriteWhitespace(xr.Value);
                                continue;
                        }
                        xw.WriteNode(xr, true);
                    }
                    xw.Flush();
                    return sw.ToString();
                }
            }
            catch
            {
                return content;
            }
        }

        private static void WriteTextNode(XmlReader xr, XmlWriter xw, bool inAnchor, Func<string, string> channelNameResolver, Func<string, string> screenNameResolver)
        {
            if (!inAnchor)
            {
                var i = 0;

                void WriteStringBefore(int m)
                {
                    if (i < m)
                    {
                        var s = xr.Value.Substring(i, m - i);
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            xw.WriteWhitespace(" ");
                        }
                        else
                        {
                            if (char.IsWhiteSpace(s[0]))
                            {
                                xw.WriteWhitespace(" ");
                            }
                            xw.WriteString(s);

                            if (char.IsWhiteSpace(s.Last()))
                            {
                                xw.WriteWhitespace(" ");
                            }
                        }
                    }
                }

                foreach (Match m in _LinkStart.Matches(xr.Value))
                {
                    var v = m.Value;
                    var n = v.Substring(1);
                    if (v[0] == '#')
                    {
                        var matched = channelNameResolver?.Invoke(n);

                        if (matched != null)
                        {
                            WriteStringBefore(m.Index);

                            xw.WriteStartElement("a");
                            xw.WriteAttributeString("href", "https://kokoro.io/channels/" + matched);
                            xw.WriteString(v);
                            xw.WriteEndElement();
                            i = m.Index + m.Length;
                        }
                    }
                    else
                    {
                        var matched = screenNameResolver?.Invoke(n);

                        if (matched != null)
                        {
                            WriteStringBefore(m.Index);

                            xw.WriteStartElement("a");
                            xw.WriteAttributeString("href", "https://kokoro.io/profiles/" + matched);
                            xw.WriteString(v);
                            xw.WriteEndElement();
                            i = m.Index + m.Length;
                        }
                    }
                }

                if (i > 0)
                {
                    WriteStringBefore(xr.Value.Length);
                    return;
                }
            }

            xw.WriteString(xr.Value);
        }
    }
}