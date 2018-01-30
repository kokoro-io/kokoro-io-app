using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json;

namespace KokoroIO.XamarinForms.Views
{
    internal static class MessagesViewHelper
    {
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
    }
}