using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace TeleBot.Classes
{
    public enum WebMethod
    {
        Get,
        Post
    }

    public class WebRequest
    {
        public string Url { get; set; }
        public List<WebHeader> Headers { get; set; }
        public WebMethod Method { get; set; }
        public HttpContent Content { get; set; }
    }

    public class WebHeader
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public static class WebClient
    {
        private static Log _log = new Log("WebClient");
        private static readonly string CookiesFile = Program.FilePathInData("WebClient.cookies");

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";

        #region Cookies

        private static void SaveCookies(CookieContainer cookies)
        {
            try
            {
                using (Stream stream = File.Create(CookiesFile))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookies);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Gagal tulis cookies: {0}", ex.Message);
            }
        }

        private static CookieContainer ReadCookies()
        {
            if (!File.Exists(CookiesFile)) return new CookieContainer();

            try
            {
                using (Stream stream = File.Open(CookiesFile, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    return formatter.Deserialize(stream) as CookieContainer;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Gagal baca cookies: {0}", ex.Message);
                return new CookieContainer();
            }
        }

        public static string ReadCookieValue(string link, string key)
        {
            var cookies = ReadCookies();
            foreach (Cookie cookie in cookies.GetCookies(new Uri(link)))
                if (cookie.Name.ToLower().Contains(key.ToLower()))
                    return cookie.Value.Trim();

            return string.Empty;
        }

        #endregion

        #region Client

        public static async Task<string> GetOrPostStringAsync(WebRequest request)
        {
            var linkUri = new Uri(request.Url);
            using (var handler = new HttpClientHandler {CookieContainer = ReadCookies()})
            using (var client = new HttpClient(handler))
            {
                var header = client.DefaultRequestHeaders;
                header.Add("User-Agent", UserAgent);
                header.Add("Host", linkUri.Host);
                if (request.Headers != null)
                    foreach (var h in request.Headers)
                        header.Add(h.Key, h.Value);

                HttpResponseMessage response;
                switch (request.Method)
                {
                    case WebMethod.Get:
                        _log.Debug("GetString: {0}", request.Url);
                        response = await client.GetAsync(linkUri);
                        break;

                    case WebMethod.Post:
                        _log.Debug("PostString: {0}", request.Url);
                        response = await client.PostAsync(linkUri, request.Content);
                        break;

                    default:
                        throw new Exception("WebMethod tidak boleh kosong!");
                }

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                SaveCookies(handler.CookieContainer);
                return result;
            }
        }

        public static async Task<Stream> GetStreamAsync(WebRequest request)
        {
            var linkUri = new Uri(request.Url);
            using (var handler = new HttpClientHandler {CookieContainer = ReadCookies()})
            using (var client = new HttpClient())
            {
                var header = client.DefaultRequestHeaders;
                header.Add("User-Agent", UserAgent);
                header.Add("Host", linkUri.Host);
                if (request.Headers != null)
                    foreach (var h in request.Headers)
                        header.Add(h.Key, h.Value);

                _log.Debug("GetStream: {0}", request.Url);
                var response = await client.GetAsync(linkUri);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStreamAsync();

                SaveCookies(handler.CookieContainer);
                return result;
            }
        }

        #endregion
    }
}