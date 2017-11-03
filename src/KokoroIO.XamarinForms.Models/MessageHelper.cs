﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommonMark;
using CommonMark.Syntax;

namespace KokoroIO.XamarinForms.Models
{
    internal static class MessageHelper
    {
        public static string AsPlainText(this Message message)
            => MarkdownToText(message.RawContent);

        public static string MarkdownToText(string markdown)
        {
            try
            {
                var b = CommonMarkConverter.Parse(markdown);
                var sb = new StringBuilder();

                WriteSiblings(sb, b);

                return sb.ToString();
            }
            catch { }
            return markdown;
        }

        private static void WriteSiblings(StringBuilder sb, Block b)
        {
            while (b != null)
            {
                WriteSingle(sb, b);

                b = b.NextSibling;
            }
        }

        private static void WriteSiblings(StringBuilder sb, Inline b)
        {
            while (b != null)
            {
                WriteSingle(sb, b);

                b = b.NextSibling;
            }
        }

        private static void WriteSingle(StringBuilder sb, Block b)
        {
            WriteSiblings(sb, b.InlineContent);

            switch (b.Tag)
            {
                case BlockTag.ThematicBreak:
                    sb.AppendLine("----");
                    break;

                case BlockTag.List:
                    var li = b.FirstChild;
                    while (li != null)
                    {
                        sb.Append("- ");
                        WriteSingle(sb, li);
                        var lc = sb[sb.Length - 1];

                        if (lc != '\r' && lc != '\n')
                        {
                            sb.AppendLine();
                        }

                        li = li.NextSibling;
                    }
                    break;

                case BlockTag.AtxHeading:
                case BlockTag.BlockQuote:
                case BlockTag.Document:
                case BlockTag.FencedCode:
                case BlockTag.HtmlBlock:
                case BlockTag.IndentedCode:
                case BlockTag.ListItem:
                case BlockTag.Paragraph:
                case BlockTag.ReferenceDefinition:
                case BlockTag.SetextHeading:
                default:
                    WriteSiblings(sb, b.FirstChild);
                    break;
            }
        }

        private static void WriteSingle(StringBuilder sb, Inline b)
        {
            if (b.LiteralContent != null)
            {
                if (sb.Length == 0 || !char.IsWhiteSpace(sb[sb.Length - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(b.LiteralContent);
            }
            WriteSiblings(sb, b.FirstChild);
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