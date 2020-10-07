using System.IO;
using System.Reflection;

namespace KokoroIO.XamarinForms
{
    internal static class RH
    {
        public static Stream GetManifestResourceStream(string fileName)
        {
            var t = typeof(RH);
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.Resources." + fileName);
        }
    }
}