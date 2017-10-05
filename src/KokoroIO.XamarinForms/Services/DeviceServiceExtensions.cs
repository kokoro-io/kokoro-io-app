using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KokoroIO.XamarinForms.Services
{
    internal static class DeviceServiceExtensions
    {
        private const string TokenName = nameof(KokoroIO) + "." + nameof(XamarinForms);

        public static string GetDeviceIdentifierString(this IDeviceService ds)
        {
            var hash = TokenName.Select(ch => (byte)ch).Concat(ds.GetIdentifier()).ToArray();

            hash = SHA256.Create().ComputeHash(hash);

            var sb = new StringBuilder(Convert.ToBase64String(hash));

            for (int i = 0; i < sb.Length; i++)
            {
                var c = sb[i];
                if (c == '/')
                {
                    sb[i] = '-';
                }
                else if (c == '+')
                {
                    sb[i] = '_';
                }
                else if (c == '=')
                {
                    sb[i] = '@';
                }
            }

            return sb.ToString();
        }
    }
}