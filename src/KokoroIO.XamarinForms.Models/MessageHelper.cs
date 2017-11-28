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
    }
}