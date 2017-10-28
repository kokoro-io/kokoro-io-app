using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace KokoroIO.XamarinForms.Models
{
    public sealed class ImgurImageUploader : IImageUploader
    {
        private const string ACCESS_TOKEN_KEY = nameof(ImgurImageUploader) + "." + nameof(_AccessToken);

        private const string REFRESH_TOKEN_KEY = nameof(ImgurImageUploader) + "." + nameof(_RefreshToken);

        // TODO: TEMPORAL KEY. MUST BE REMOVED AND REVOKED.
        private readonly string _ClientId = "21b4c2e0b6db3c3";

        private readonly string _ClientSecret = "62d75cf90ab7db546928f593ea4b643a85611363";

        private string _AccessToken;
        private string _RefreshToken;

        public ImgurImageUploader()
        {
            if (App.Current.Properties.TryGetValue(ACCESS_TOKEN_KEY, out var obj))
            {
                _AccessToken = obj as string;
            }
            if (App.Current.Properties.TryGetValue(REFRESH_TOKEN_KEY, out obj))
            {
                _RefreshToken = obj as string;
            }
        }

        public string DisplayName => "Imgur";

        public string LogoImage => null;

        public bool IsAuthorized => _AccessToken != null;

        public string AuthorizationUrl => $"https://api.imgur.com/oauth2/authorize?client_id={_ClientId}&response_type=token";

        public string CallbackUrl => "http://kokoro.io/client/oauthcallback/imgur";

        public async Task ReceiveCallbackAsync(string url)
        {
            string at = null, rt = null;
            try
            {
                var m = Regex.Match(url, "[?&#]access_token=([^&]*)($|&)");
                if (m.Success)
                {
                    at = Uri.UnescapeDataString(m.Groups[1].Value);
                }
                var m2 = Regex.Match(url, "[?&#]refresh_token=([^&]*)($|&)");
                if (m2.Success)
                {
                    rt = Uri.UnescapeDataString(m2.Groups[1].Value);
                }
            }
            finally
            {
                try
                {
                    _AccessToken = at;
                    App.Current.Properties[ACCESS_TOKEN_KEY] = at;
                    App.Current.Properties[REFRESH_TOKEN_KEY] = rt;
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
                    for (var i = 0; i < 2; i++)
                    {
                        if (i == 1)
                        {
                            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3oauth2/token");
                            req.Content = new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("refresh_token", _RefreshToken),
                                new KeyValuePair<string, string>("client_id", _ClientId),
                                new KeyValuePair<string, string>("client_secret", _ClientSecret),
                                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            });

                            var res = await hc.SendAsync(req).ConfigureAwait(false);

                            res.EnsureSuccessStatusCode();

                            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var jo = JObject.Parse(json);
                            _RefreshToken = jo.Property("refresh_token")?.Value?.Value<string>();
                            try
                            {
                                App.Current.Properties[ACCESS_TOKEN_KEY] = _AccessToken;
                                await App.Current.SavePropertiesAsync().ConfigureAwait(false);
                            }
                            catch { }
                        }
                        try
                        {
                            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
                            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _AccessToken);

                            var mp = new MultipartFormDataContent();

                            var sc = new StreamContent(data);

                            sc.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                            {
                                Name = "image",
                                FileName = fileName
                            };
                            mp.Add(sc, "image");

                            req.Content = mp;

                            var res = await hc.SendAsync(req).ConfigureAwait(false);

                            res.EnsureSuccessStatusCode();

                            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                            var jo = JObject.Parse(json);

                            return jo.Property("data")?.Value?.Value<JObject>()?.Property("link")?.Value?.Value<string>();
                        }
                        catch
                        {
                            if (i == 0 && _RefreshToken != null)
                            {
                                continue;
                            }
                            throw;
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    _AccessToken = null;
                    App.Current.Properties[ACCESS_TOKEN_KEY] = null;
                    App.Current.Properties[REFRESH_TOKEN_KEY] = null;
                    await App.Current.SavePropertiesAsync().ConfigureAwait(false);
                }
                catch { }

                throw;
            }
            throw new Exception();
        }
    }
}