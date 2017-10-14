using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageInfo : ObservableObject
    {
        internal MessageInfo(MessagesViewModel page, Message message)
        {
            Page = page;
            _Id = message.Id;
            IdempotentKey = message.IdempotentKey.HasValue && message.IdempotentKey.Value != default(Guid) ? message.IdempotentKey : null;
            Profile = page.Application.GetProfile(message);
            _PublishedAt = message.PublishedAt;
            _IsNsfw = message.IsNsfw;

            _OriginalContent = message.Content;

            _EmbedContents = message.EmbedContents;
        }

        internal MessageInfo(MessagesViewModel page, string content)
        {
            Page = page;
            IdempotentKey = Guid.NewGuid();
            Profile = page.Application.LoginUser.ToProfile();

            _OriginalContent = content.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        internal void Update(Message message)
        {
            Id = message.Id;
            Profile = Page.Application.GetProfile(message);
            PublishedAt = message.PublishedAt;
            IsNsfw = message.IsNsfw;
            SetOriginalContent(message.Content);
            EmbedContents = message.EmbedContents;
        }

        private MessagesViewModel Page { get; }

        #region Id

        private int? _Id;

        public int? Id
        {
            get => _Id;
            private set => SetProperty(ref _Id, value);
        }

        #endregion Id

        public Guid? IdempotentKey { get; }

        #region Profile

        private Profile _Profile;

        public Profile Profile
        {
            get => _Profile;
            private set => SetProperty(ref _Profile, value);
        }

        #endregion Profile

        public string DisplayAvatar
        {
            get
            {
                if (Profile.Avatars != null)
                {
                    var s = Page.Application.DisplayScale * 40;
                    return Profile.Avatars.OrderBy(a => a.Size).Where(a => a.Size >= s).FirstOrDefault()?.Url
                            ?? Profile.Avatars.OrderByDescending(a => a.Size).FirstOrDefault()?.Url
                            ?? Profile.Avatar;
                }

                return Profile.Avatar;
            }
        }

        #region PublishedAt

        private DateTime? _PublishedAt;

        public DateTime? PublishedAt
        {
            get => _PublishedAt;
            private set => SetProperty(ref _PublishedAt, value);
        }

        #endregion PublishedAt

        #region IsNsfw

        private bool _IsNsfw;

        public bool IsNsfw
        {
            get => _IsNsfw;
            private set => SetProperty(ref _IsNsfw, value);
        }

        #endregion IsNsfw

        #region Content

        private string _OriginalContent;

        private string _Content;

        private static readonly Regex _LinkStart = new Regex("(@[-_a-z0-9]+|#[-_a-z0-9.]+)", RegexOptions.IgnoreCase);

        public string Content
        {
            get
            {
                if (_Content == null)
                {
                    try
                    {
                        using (var sr = new StringReader(_OriginalContent.Replace("<br>", "<br />")))
                        using (var xr = XmlReader.Create(sr, new XmlReaderSettings()
                        {
                            ConformanceLevel = ConformanceLevel.Fragment,
                            CheckCharacters = false,
#if !WINDOWS_UWP
                            ValidationType = ValidationType.None,
#endif
                        }))
                        using (var sw = new StringWriter(new StringBuilder(_OriginalContent.Length + 16)))
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
                                        if (!xr.IsEmptyElement)
                                        {
                                            inAnchor |= "a".Equals(xr.LocalName, StringComparison.OrdinalIgnoreCase);
                                            xw.WriteStartElement(xr.Prefix, xr.LocalName, xr.NamespaceURI);

                                            while (xr.MoveToNextAttribute())
                                            {
                                                xw.WriteAttributeString(xr.LocalName, xr.NamespaceURI, xr.Value);
                                            }

                                            continue;
                                        }
                                        break;

                                    case XmlNodeType.Attribute:
                                        xw.WriteAttributeString(xr.LocalName, xr.NamespaceURI, xr.Value);
                                        continue;

                                    case XmlNodeType.EndElement:
                                        inAnchor = inAnchor && !"a".Equals(xr.LocalName, StringComparison.OrdinalIgnoreCase);
                                        xw.WriteEndElement();
                                        continue;

                                    case XmlNodeType.Text:
                                        WriteTextNode(xr, xw, inAnchor);
                                        continue;

                                    case XmlNodeType.Whitespace:
                                        xw.WriteWhitespace(xr.Value);
                                        continue;
                                }
                                xw.WriteNode(xr, true);
                            }
                            xw.Flush();
                            _Content = sw.ToString();
                        }
                    }
                    catch { }
                    finally
                    {
                        _Content = _Content ?? _OriginalContent ?? "<p></p>";
                    }
                }
                return _Content;
            }
        }

        private void WriteTextNode(XmlReader xr, XmlWriter xw, bool inAnchor)
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
                        var matched = Page.Application.Channels.FirstOrDefault(c => c.ChannelName.Equals(n, StringComparison.OrdinalIgnoreCase));

                        if (matched != null)
                        {
                            WriteStringBefore(m.Index);

                            xw.WriteStartElement("a");
                            xw.WriteAttributeString("href", "https://kokoro.io/channels/" + matched.Id);
                            xw.WriteString(v);
                            xw.WriteEndElement();
                            i = m.Index + m.Length;
                        }
                    }
                    else
                    {
                        var matched = Page.Members.FirstOrDefault(p => p.ScreenName.Equals(n, StringComparison.OrdinalIgnoreCase));

                        if (matched != null)
                        {
                            WriteStringBefore(m.Index);

                            xw.WriteStartElement("a");
                            xw.WriteAttributeString("href", "https://kokoro.io/@" + matched.ScreenName);
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

        private void SetOriginalContent(string value)
        {
            var notify = _Content != null;
            _Content = null;
            _OriginalContent = value;
            if (notify)
            {
                OnPropertyChanged(nameof(Content));
            }
        }

        #endregion Content

        #region EmbedContents

        private IList<EmbedContent> _EmbedContents;

        public IList<EmbedContent> EmbedContents
        {
            get => _EmbedContents;
            private set => SetProperty(ref _EmbedContents, value);
        }

        #endregion EmbedContents

        #region IsMerged

        private bool _IsMerged;

        public bool IsMerged
        {
            get => _IsMerged;
            private set => SetProperty(ref _IsMerged, value);
        }

        internal void SetIsMerged(MessageInfo prev)
        {
            IsMerged = (prev?.Profile == Profile
                            || (prev?.Profile.Id == Profile.Id
                                && prev?.Profile.ScreenName == Profile.ScreenName
                                && prev?.Profile.DisplayName == Profile.DisplayName
                                && prev?.Profile.Avatar == Profile.Avatar))
                        && (PublishedAt ?? DateTime.UtcNow) < prev.PublishedAt?.AddMinutes(3);
        }

        #endregion IsMerged

        #region IsShown

        private bool _IsShown;

        public bool IsShown
        {
            get => _IsShown;
            set => SetProperty(ref _IsShown, value, onChanged: () =>
            {
                if (_IsShown && !(Page.Channel.LastReadId >= Id))
                {
                    Page.Channel.LastReadId = Id;
                }
            });
        }

        #endregion IsShown
    }
}