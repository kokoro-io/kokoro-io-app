using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json;
using Plugin.Clipboard;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public static class MessagesViewHelper
    {
        public static string GetHtml()
        {
            using (var rs = RH.GetManifestResourceStream("Messages.html"))
            using (var sr = new StreamReader(rs))
            {
                var html = sr.ReadToEnd();

                if (XDevice.Idiom == TargetIdiom.Desktop)
                {
                    return html.Replace("<html>", "<html class=\"html-desktop\">");
                }
                else if (XDevice.Idiom == TargetIdiom.Tablet)
                {
                    return html.Replace("<html>", "<html class=\"html-tablet\">");
                }
                return html;
            }
        }

        public static string CreateScriptForRequest(IEnumerable<MessageInfo> messages, bool reset, MessagesViewUpdateRequest[] requests, bool? hasUnread)
        {
            var sm = requests.LastOrDefault(r => r.Type == MessagesViewUpdateType.Show)?.Message;
            using (var sw = new StringWriter())
            {
                if (reset)
                {
                    sw.Write("window.setMessages(");
                    if (messages == null)
                    {
                        sw.Write("null");
                        TH.Info("Clearing messages of MessagesView");
                    }
                    else
                    {
                        new JsonSerializer().Serialize(
                            sw, messages.Select(m => m.ToJson())
                                        .OrderBy(m => m.PublishedAt ?? DateTime.MaxValue));
                        TH.Info("Setting {0} messages to MessagesView", messages.Count());
                    }

                    sw.WriteLine(");");
                }
                else
                {
                    List<MessageInfo> added = null, removed = null, merged = null;
                    foreach (var g in requests.GroupBy(r => r.Message))
                    {
                        if (g.Any(r => r.Type == MessagesViewUpdateType.Added))
                        {
                            (added ?? (added = new List<MessageInfo>())).Add(g.Key);
                        }
                        else if (g.Any(r => r.Type == MessagesViewUpdateType.Removed))
                        {
                            (removed ?? (removed = new List<MessageInfo>())).Add(g.Key);
                        }
                        else if (g.Any(r => r.Type == MessagesViewUpdateType.IsMergedChanged))
                        {
                            (merged ?? (merged = new List<MessageInfo>())).Add(g.Key);
                        }
                    }

                    var js = new JsonSerializer();

                    if (removed != null)
                    {
                        sw.Write("window.removeMessages(");
                        js.Serialize(sw, removed.Select(m => m.Id).OfType<int>());
                        sw.Write(",");
                        js.Serialize(sw, removed.Select(m => m.IdempotentKey).OfType<Guid>());
                        sw.Write(",");
                        if (merged != null && added == null)
                        {
                            js.Serialize(sw, merged.Select(m => m.ToMergeInfo()));
                        }
                        else
                        {
                            sw.Write("null");
                        }
                        sw.WriteLine(");");
                        TH.Info("Removing {0} messages of MessagesView", removed.Count);
                    }
                    if (added != null || (merged != null && removed == null))
                    {
                        sw.Write("window.addMessages(");
                        if (added != null)
                        {
                            js.Serialize(
                                sw,
                                added.Select(m => m.ToJson())
                                        .OrderBy(m => m.PublishedAt ?? DateTime.MaxValue));
                            TH.Info("Adding {0} messages to MessagesView", added.Count);
                        }
                        else
                        {
                            sw.Write("[]");
                        }
                        sw.Write(",");
                        if (merged != null)
                        {
                            js.Serialize(sw, merged.Select(m => m.ToMergeInfo()));
                        }
                        else
                        {
                            sw.Write("[]");
                        }
                        sw.WriteLine(");");
                    }
                }
                if (sm?.Id > 0)
                {
                    sw.WriteLine("window.showMessage({0}, true);", sm.Id);
                    TH.Info("Showing message#{0} in MessagesView", sm.Id);
                }
                if (hasUnread != null)
                {
                    sw.WriteLine("window.setHasUnread({0});", hasUnread == true ? "true" : "false");
                    TH.Info("Setting HasUnread = {0}", hasUnread);
                }
                return sw.ToString();
            }
        }

        #region Navigating

        public static bool ShouldOverrideRequest(MessagesView mv, string url)
        {
            if (HandleLoaded(mv, url)
                || HandlePrepend(mv, url)
                || HandleAppend(mv, url)
                || HandleVisibility(mv, url)
                || HandleReply(mv, url)
                || HandleCopy(mv, url)
                || HandleDeletion(mv, url)
                || HandleMenu(mv, url))
            {
                return true;
            }

            var c = mv.NavigatingCommand;

            if (c?.CanExecute(url) == true)
            {
                c.Execute(url);
            }

            return true;
        }

        private static bool HandleLoaded(MessagesView mv, string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=loaded";
            if (url == PREFIX)
            {

                // TODO: mv._HtmlLoaded = true;
                return true;
            }
            return false;
        }

        private static bool HandlePrepend(MessagesView mv, string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=prepend&count=";
            if (url.StartsWith(PREFIX))
            {
                if (int.TryParse(url.Substring(PREFIX.Length), out var c)
                    && c == 0)
                {
                    return true;
                }
                mv.LoadOlderCommand?.Execute(null);
                return true;
            }
            return false;
        }

        private static bool HandleAppend(MessagesView mv, string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=append&count=";
            if (url.StartsWith(PREFIX))
            {
                if (int.TryParse(url.Substring(PREFIX.Length), out var c)
                    && c == 0)
                {
                    return true;
                }
                mv.RefreshCommand?.Execute(null);
                return true;
            }
            return false;
        }

        private static bool HandleVisibility(MessagesView mv, string url)
        {
            const string IDURL = "http://kokoro.io/client/control?event=visibility&ids=";
            if (url.StartsWith(IDURL))
            {
                var messages = mv.Messages;

                if (messages != null && messages.Any())
                {
                    int? max = null;
                    int? min = null;
                    List<Guid> keys = null;

                    foreach (var id in url.Substring(IDURL.Length).Split(','))
                    {
                        if (int.TryParse(id, out var i))
                        {
                            min = Math.Min(min ?? int.MaxValue, i);
                            max = Math.Max(max ?? int.MinValue, i);
                        }
                        else if (Guid.TryParse(id, out var g))
                        {
                            (keys ?? (keys = new List<Guid>())).Add(g);
                        }
                    }

                    TH.Info("Shown message: {0}-{1} of ({2}-{3})", min, max, messages.FirstOrDefault()?.Id, messages.LastOrDefault()?.Id);

                    foreach (var m in messages)
                    {
                        m.IsShown = (min <= m.Id && m.Id <= max)
                                    || (m.Id == null
                                        && m.IdempotentKey != null
                                        && keys?.Contains(m.IdempotentKey.Value) == true);
                    }
                }

                return true;
            }
            return false;
        }

        private static bool HandleReply(MessagesView mv, string url)
        {
            const string REPLY_URL = "http://kokoro.io/client/control?event=replyToMessage&id=";
            if (url.StartsWith(REPLY_URL))
            {
                if (int.TryParse(url.Substring(REPLY_URL.Length), out var id))
                {
                    var msg = mv.Messages.FirstOrDefault(m => m.Id == id);
                    msg?.Reply();
                }
                return true;
            }
            return false;
        }

        private static bool HandleCopy(MessagesView mv, string url)
        {
            const string COPY_URL = "http://kokoro.io/client/control?event=copyMessage&id=";
            if (url.StartsWith(COPY_URL))
            {
                if (int.TryParse(url.Substring(COPY_URL.Length), out var id))
                {
                    var msg = mv.Messages.FirstOrDefault(m => m.Id == id);

                    if (msg != null)
                    {
                        CrossClipboard.Current.SetText(msg.PlainTextContent);
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HandleDeletion(MessagesView mv, string url)
        {
            const string DELETEURL = "http://kokoro.io/client/control?event=deleteMessage&id=";
            if (url.StartsWith(DELETEURL))
            {
                if (int.TryParse(url.Substring(DELETEURL.Length), out var id))
                {
                    var msg = mv.Messages.FirstOrDefault(m => m.Id == id);
                    msg?.BeginConfirmDeletion();
                }
                return true;
            }
            return false;
        }

        private static bool HandleMenu(MessagesView mv, string url)
        {
            const string DELETEURL = "http://kokoro.io/client/control?event=messageMenu&id=";
            if (url.StartsWith(DELETEURL))
            {
                if (int.TryParse(url.Substring(DELETEURL.Length), out var id))
                {
                    var msg = mv.Messages.FirstOrDefault(m => m.Id == id);
                    msg?.ShowMenu();
                }
                return true;
            }
            return false;
        }

        #endregion Navigating
    }
}