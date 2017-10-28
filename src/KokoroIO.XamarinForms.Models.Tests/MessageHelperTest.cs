using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KokoroIO.XamarinForms.Models.Tests
{
    [TestClass]
    public class MessageHelperTest
    {
        [TestMethod]
        public void InsertLinksTest()
        {
//            var xml = @"<blockquote>
//<p>status (string, optional):<br/>
//発言の状態 = [&#39;active&#39;, &#39;deleted_by_publisher&#39;, &#39;deleted_by_another_member&#39;],</p>
//</blockquote>
//";
            var xml = @"<blockquote><p>#before<br/>#after</p></blockquote>";

            var r = MessageHelper.InsertLinks(xml, null, null);
            Assert.AreEqual(XElement.Parse(xml).Value, XElement.Parse(r).Value);
        }
    }
}
