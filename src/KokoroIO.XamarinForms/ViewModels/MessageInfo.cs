using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using KokoroIO.XamarinForms.Helpers;
using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class MessageInfo : ObservableObject
    {
        internal MessageInfo(MessagesViewModel page, Message message)
        {
            Page = page;
            Id = message.Id;
            Profile = page.Application.GetProfile(message.Profile);
            PublishedAt = message.PublishedAt;

            Blocks = new ObservableCollection<MessageBlockBase>();
            LoadBlocks(message);
        }

        private MessagesViewModel Page { get; }

        public int Id { get; }

        public ProfileViewModel Profile { get; }

        public DateTime PublishedAt { get; }

        #region IsMerged

        private bool _IsMerged;

        public bool IsMerged
        {
            get => _IsMerged;
            private set => SetProperty(ref _IsMerged, value);
        }

        internal void SetIsMerged(MessageInfo prev)
        {
            IsMerged = prev?.Profile == Profile
                        && PublishedAt < prev.PublishedAt.AddMinutes(3);
        }

        #endregion IsMerged

        public ObservableCollection<MessageBlockBase> Blocks { get; }

        private void LoadBlocks(Message model)
        {
            try
            {
                var html = model.Content.Replace("<br>", "<br />").Replace("\u0008", "");

                using (var sr = new StringReader(html))
                using (var xr = XmlReader.Create(sr, new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
                {
                    while (xr.Read())
                    {
                        if (xr.NodeType == XmlNodeType.Element && xr.Depth == 0)
                        {
                            using (var sxr = xr.ReadSubtree())
                            {
                                var e = XElement.Load(sxr);

                                if (Regex.IsMatch(e.Name.LocalName, "^[uo]l$", RegexOptions.IgnoreCase))
                                {
                                    Blocks.Add(new ListMessageBlock(this, e));
                                }
                                else
                                {
                                    Blocks.Add(new MessageBlock(this, e));
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Blocks.Clear();

                var ps = Regex.Split(model.RawContent, "(\\r|\\r?\\n){2,}");
                foreach (var p in ps)
                {
                    if (!string.IsNullOrEmpty(p))
                    {
                        var pvm = new MessageBlock(this);
                        using (var sr = new StringReader(p))
                        {
                            for (var l = sr.ReadLine(); l != null; l = sr.ReadLine())
                            {
                                if (pvm.Spans.Count > 0)
                                {
                                    pvm.Spans.Add(new MessageSpan() { Text = Environment.NewLine });
                                }
                                pvm.Spans.Add(new MessageSpan()
                                {
                                    Text = l
                                });
                            }
                        }
                        Blocks.Add(pvm);
                    }
                }
            }
        }
    }
}