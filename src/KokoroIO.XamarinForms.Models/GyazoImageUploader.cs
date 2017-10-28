using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace KokoroIO.XamarinForms.Models
{
    public sealed class GyazoImageUploader : IImageUploader
    {
        private const string ACCESS_TOKEN_KEY = nameof(GyazoImageUploader) + "." + nameof(_AccessToken);

        // TODO: TEMPORAL KEY. MUST BE REMOVED AND REVOKED.
        private readonly string _ClientId = "1ddda67818dd339ff8fc80a9d03885ac1e3d32a44712d5775b7be2f67b39aa66";

        private readonly string _ClientSecret = "d78f76bd6127e07fae0f46476ec42649d0eaba360e86dba4e4d3f7ee2b02d2e2";

        private string _AccessToken;

        public GyazoImageUploader()
        {
            if (App.Current.Properties.TryGetValue(ACCESS_TOKEN_KEY, out var obj))
            {
                _AccessToken = obj as string;
            }
        }

        public string DisplayName => "Gyazo";

        public string LogoImage => "Gyazo.png";

        public bool IsAuthorized => _AccessToken != null;

        public string AuthorizationUrl => $"https://api.gyazo.com/oauth/authorize?client_id={_ClientId}&redirect_uri={Uri.EscapeDataString(CallbackUrl)}&response_type=code";

        public string CallbackUrl => "http://kokoro.io/client/oauthcallback/gyazo";

        public async Task ReceiveCallbackAsync(string url)
        {
            string at = null;
            try
            {
                var m = Regex.Match(url, "[?&]code=([^&]*)($|&)");
                if (m.Success)
                {
                    var code = m.Groups[1].Value;

                    using (var hc = new HttpClient())
                    {
                        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.gyazo.com/oauth/token");

                        req.Content = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("client_id", _ClientId),
                        new KeyValuePair<string, string>("client_secret", _ClientSecret),
                        new KeyValuePair<string, string>("redirect_uri", CallbackUrl),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("grant_type", "authorization_code")
                    });

                        var res = await hc.SendAsync(req).ConfigureAwait(false);

                        res.EnsureSuccessStatusCode();

                        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var jo = JObject.Parse(json);

                        at = jo.Property("access_token")?.Value?.Value<string>();
                    }
                }
            }
            finally
            {
                try
                {
                    _AccessToken = at;
                    App.Current.Properties[ACCESS_TOKEN_KEY] = at;
                    await App.Current.SavePropertiesAsync().ConfigureAwait(false);
                }
                catch { }
            }
        }

        public async Task<string> UploadAsync(Stream data, string fileName)
        {
            if (_AccessToken == null)
            {
                throw new InvalidOperationException("Not authoized");
            }

            try
            {
                using (var hc = new HttpClient())
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "https://upload.gyazo.com/api/upload");

                    var mp = new MultipartFormDataContent();

                    mp.Add(new StringContent(_AccessToken), "access_token");

                    var sc = new StreamContent(data);

                    sc.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "imagedata",
                        FileName = fileName
                    };
                    mp.Add(sc, "imagedata");

                    req.Content = mp;

                    var res = await hc.SendAsync(req).ConfigureAwait(false);

                    res.EnsureSuccessStatusCode();

                    var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var jo = JObject.Parse(json);

                    return jo.Property("url")?.Value?.Value<string>();
                }
            }
            catch
            {
                try
                {
                    _AccessToken = null;
                    App.Current.Properties[ACCESS_TOKEN_KEY] = null;
                    await App.Current.SavePropertiesAsync().ConfigureAwait(false);
                }
                catch { }

                throw;
            }
        }
    }
}