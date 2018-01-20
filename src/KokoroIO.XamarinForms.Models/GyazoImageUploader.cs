using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#if !MODEL_TESTS

namespace KokoroIO.XamarinForms.Models
{
    using static ModelSecrets;

    public sealed class GyazoImageUploader : IImageUploader
    {
        private const string ACCESS_TOKEN_KEY = nameof(GyazoImageUploader) + "." + nameof(_AccessToken);

        // TODO: TEMPORAL KEY. MUST BE REMOVED AND REVOKED.

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

        public string AuthorizationUrl => $"https://api.gyazo.com/oauth/authorize?client_id={GYAZO_CLIENT_ID}&redirect_uri={Uri.EscapeDataString(CallbackUrl)}&response_type=code";

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
                        new KeyValuePair<string, string>("client_id", GYAZO_CLIENT_ID),
                        new KeyValuePair<string, string>("client_secret", GYAZO_CLIENT_SECRET),
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

        public async Task<UploadedImageInfo> UploadAsync(Stream data, string fileName)
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

                    return new UploadedImageInfo
                    {
                        RawUrl = jo.Property("url")?.Value?.Value<string>(),
                        ThumbnailUrl = jo.Property("thumb_url")?.Value?.Value<string>()
                    };
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

#endif