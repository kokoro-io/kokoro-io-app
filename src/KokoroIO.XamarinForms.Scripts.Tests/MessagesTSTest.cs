using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using KokoroIO.XamarinForms.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;

namespace KokoroIO.XamarinForms.Scripts
{
    [TestClass]
    public sealed class MessagesTSTest
    {
        private ChromeDriver _Driver;

        public ChromeDriver Driver
        {
            get
            {
                if (_Driver == null)
                {
                    _Driver = new ChromeDriver();

                    var au = new Uri(GetType().Assembly.Location);
                    var tu = new Uri(au, "Messages.template.html");

                    var html = File.ReadAllText(tu.LocalPath);

                    var u = new Uri(au, "Messages.html");
                    File.WriteAllText(u.LocalPath, html.Replace("<base", "<b.a.s.e").Replace("Messages.js", "Messages.debug.js"), Encoding.UTF8);


                    var u2 = new Uri(au, "Messages.js");

                    var js = File.ReadAllText(u2.LocalPath);
                    var u3 = new Uri(au, "Messages.debug.js");
                    File.WriteAllText(u3.LocalPath, js.Replace("  location.href = ", "  // location.href = "), Encoding.UTF8);

                    _Driver.Navigate().GoToUrl(u);
                }
                return _Driver;
            }
        }

        private MessagesViewMessage CreateMessage(int? id = null, Guid? idempotentKey = null)
            => new MessagesViewMessage()
            {
                Id = id,
                IdempotentKey = idempotentKey,
                ProfileId = "XXXXXXXXX",
                Avatar = "#",
                ScreenName = "screenname",
                DisplayName = "display_name",
                HtmlContent = $"Id: {id}, IdempotentKey: {idempotentKey}",
                PublishedAt = DateTime.Now,
                EmbedContents = new List<EmbedContent>(0)
            };

        private void SetMessages(params MessagesViewMessage[] messages)
        {
            using (var sw = new StringWriter())
            {
                sw.Write("window.setMessages(");
                new JsonSerializer().Serialize(sw, messages);
                sw.Write(")");

                ExecuteScript(sw.ToString());
            }
        }

        private void AddMessages(params MessagesViewMessage[] messages)
        {
            using (var sw = new StringWriter())
            {
                sw.Write("window.addMessages(");
                new JsonSerializer().Serialize(sw, messages);
                sw.Write(",[])");

                ExecuteScript(sw.ToString());
            }
        }

        private MessagesViewMessage[] GetResult()
        {
            var script = "var ts = document.querySelectorAll('.talk');"
                        + "var ary = [];"
                        + "for(var i=0;i<ts.length;i++){"
                        + "var t = ts[i];"
                        + "ary.push({Id:t.getAttribute('data-message-id'),IdempotentKey:t.getAttribute('data-idempotent-key')});"
                        + "}"
                        + "return JSON.stringify(ary);";

            var json = ExecuteScript(script) as string;

            return JsonConvert.DeserializeObject<MessagesViewMessage[]>(json);
        }

        private object ExecuteScript(string s)
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    return Driver.ExecuteScript(s);
                }
                catch
                {
                    if (i == 9)
                    {
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void IdempotentTest()
        {
            var m1 = CreateMessage(id: 1);
            var m2 = CreateMessage(id: 2);
            var m3 = CreateMessage(idempotentKey: default(Guid));

            SetMessages(m1, m3);
            m3.Id = 3;
            AddMessages(m2, m3);

            var rs = GetResult();
            Assert.AreEqual(3, rs.Length);
            Assert.IsTrue(rs.Select(r => (int)r.Id).SequenceEqual(new[] { 1, 2, 3 }));
            Assert.IsTrue(rs.All(r => r.IdempotentKey == null));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _Driver?.Close();
        }
    }
}