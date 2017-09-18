using System.IO;
using System.Reflection;

namespace KokoroIO.XamarinForms
{
    internal static class RH
    {
        public static Stream GetManifestResourceStream(string fileName)
        {
            var t = typeof(RH);
#if WINDOWS_UWP
            return t.GetTypeInfo().Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.UWP.Resources." + fileName);
#else
#if __IOS__
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.iOS.Resources." + fileName);
#else
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.Droid.Resources." + fileName);
#endif
#endif
        }
    }
}