using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeleBot.Classes;
using WebClient = TeleBot.Classes.WebClient;
using WebRequest = TeleBot.Classes.WebRequest;

namespace TeleBot.Plugins
{
    public static class Mobilism
    {
        private const string BaseAddress = "https://forum.mobilism.org";
        private const string LoginPath = "/ucp.php?mode=login";
        private const string LoginAddress = BaseAddress + LoginPath;
        
        private static Log _log = new Log("Mobilism");
        private static string _sid;
        private static bool _isLoggedIn;
        
        private static readonly string JsonPath = Program.FilePathInData("Mobilism.json");
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
        };
        
        private class Account
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private static Account ReadAccount()
        {
            if (!File.Exists(JsonPath))
            {
                var auth = new Account() {Username = "", Password = ""};
                var json = JsonConvert.SerializeObject(auth, JsonSettings);

                File.WriteAllText(JsonPath, json);
                throw new Exception("Akun tidak boleh kosong!");
            }
            else
            {
                var json = File.ReadAllText(JsonPath);
                var auth = JsonConvert.DeserializeObject<Account>(json);
                
                if (string.IsNullOrWhiteSpace(auth.Username) || string.IsNullOrWhiteSpace(auth.Password))
                    throw new Exception("Akun tidak boleh kosong!");
                
                return auth;
            }
        }

        private static void GetSidFromCookies()
        {
            var newSid = WebClient.ReadCookieValue(BaseAddress, "sid");
            if (string.Equals(_sid, newSid)) return;
            
            _sid = newSid;
            _log.Info("New SID: {0}", newSid);
        }

        public static async Task Login()
        {
            try
            {
                if (_isLoggedIn) return;
                if (string.IsNullOrWhiteSpace(_sid))
                {
                    // buka homepage
                    var homepage = await WebClient.GetOrPostStringAsync(new WebRequest() {Url = BaseAddress});
                    Dump.ToFile("Mobilism.html", homepage);
                    
                    GetSidFromCookies();

                    if (!homepage.Contains(LoginPath))
                    {
                        _log.Ignore("Status: sudah login...");
                        _isLoggedIn = true;
                        return;
                    }
                }

                var auth = ReadAccount();
                var redirect = WebUtility.UrlEncode(LoginPath);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", auth.Username),
                    new KeyValuePair<string, string>("password", auth.Password),
                    new KeyValuePair<string, string>("login", "Login"),
                    new KeyValuePair<string, string>("autologin", "on"),
                    new KeyValuePair<string, string>("viewonline", "on"),
                    new KeyValuePair<string, string>("redirect", redirect),
                    new KeyValuePair<string, string>("sid", _sid),
                    new KeyValuePair<string, string>("redirect", "index.php")
                });
                var headers = new List<WebHeader>
                {
                    new WebHeader() {Key = "Referer", Value = $"{LoginAddress}&sid={_sid}"}
                };

                var loginRequest = new WebRequest()
                {
                    Url = LoginAddress,
                    Headers = headers,
                    Method = WebMethod.Post,
                    Content = content
                };
                
                var loginpage = await WebClient.GetOrPostStringAsync(loginRequest);
                Dump.ToFile("Mobilism-Login.html", loginpage);
                
                GetSidFromCookies();
                
                if (!loginpage.Contains(LoginPath))
                {
                    _log.Info("Status: berhasil login...");
                    _isLoggedIn = true;
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }
    }
}