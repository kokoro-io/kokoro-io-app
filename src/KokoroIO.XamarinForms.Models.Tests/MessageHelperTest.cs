using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KokoroIO.XamarinForms.Models.Tests
{
    [TestClass]
    public class MessageHelperTest
    {
        [TestMethod]
        public void AsPlainTextTest()
        {
            var md = "- `a`\r\n- b\r\n\r\n[`hoge`](#)";
            var pt = MessageHelper.MarkdownToText(md);
            Assert.IsNotNull(pt);
        }

        [TestMethod]
        public void InsertLinksTest()
        {
            var xml = @"<blockquote><p>#before<br/>#after</p></blockquote>";

            var r = MessageHelper.InsertLinks(xml, null, null);
            Assert.AreEqual(XElement.Parse(xml).Value, XElement.Parse(r).Value);
        }
    }
}