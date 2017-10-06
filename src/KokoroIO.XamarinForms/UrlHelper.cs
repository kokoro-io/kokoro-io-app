using System;
using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms
{
    internal static class UrlHelper
    {
        public static Dictionary<string, string> ParseQueryString(this string url)
            => url?.Split('?', '&').Select(s => s.Split(new[] { '=' }, 2)).Where(a => a.Length == 2).ToDictionary(a => a[0], a => Uri.UnescapeDataString(a[1]))
                ?? new Dictionary<string, string>();
    }
}