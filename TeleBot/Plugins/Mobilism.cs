using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeleBot.BotClient;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using WebClient = TeleBot.Classes.WebClient;
using WebRequest = TeleBot.Classes.WebRequest;

namespace TeleBot.Plugins
{
    public class Mobilism
    {
        private const string BaseAddress = "https://forum.mobilism.org";
        private const string LoginPath = "/ucp.php?mode=login";
        private const string LoginAddress = BaseAddress + LoginPath;
        
        private static Log _log = new Log("Mobilism");
        private static string _sid;
        private static bool _isLoggedIn;
        private Message _message;
        
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

        private static async Task Login()
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

                    // verifikasi login
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
                
                // cek html atau kode alien?
                if (!loginpage.Contains("html"))
                    throw new Exception("Result bukan html!");
                
                // verifikasi login
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

        public Mobilism(Message message)
        {
            _message = message;
        }

        public void Process(string data, bool isApp = true)
        {
            // default search mode
            var threadMode = false;
            
            // antisipasi error jika data null
            // data diganti dgn string empty & pindah ke thread mode
            if (string.IsNullOrWhiteSpace(data))
            {
                threadMode = true;
                data = string.Empty;
            }
            
            var keywords = WebUtility.UrlEncode(data);
            var forumId = isApp ? "399" : "408";
            
            // search mode : app - game
            var threadUrl = isApp ?
                $"{BaseAddress}/search.php?keywords={keywords}&fid%5B%5D=399&sr=topics&sf=titleonly" :
                $"{BaseAddress}/search.php?keywords={keywords}&sr=topics&sf=titleonly";
            
            // thread mode : app - game
            if (threadMode)
                threadUrl = $"{BaseAddress}/viewforum.php?f={forumId}";
            
            var headers = new List<WebHeader>
            {
                new WebHeader() {Key = "Referer", Value = $"{BaseAddress}/viewforum.php?f={forumId}"}
            };
            
            var threadRequest = new WebRequest()
            {
                Url = threadUrl,
                Headers = headers,
                Method = WebMethod.Get
            };

            _log.Debug("Thread {0}{1}", isApp ? "aplikasi" : "permainan", threadMode ? "" : $" -- Cari: {data}");
            GetThreads(threadRequest, threadMode, isApp, data);
        }

        private async void GetThreads(WebRequest threadRequest, bool threadMode, bool isApp, string keywords)
        {
            // search mode harus login
            if (!threadMode)
            {
                await Login();
                if (!_isLoggedIn)
                {
                    // coba login sekali lagi
                    await Login();
                    if (!_isLoggedIn)
                    {
                        _log.Warning("Gangguan login...");
                        await Bot.SendTextAsync(_message,
                            "Mohon maaf...\nPlugin mobilism saat ini sedang mengalami gangguan tidak bisa login!");
                        return;
                    }
                }
            }

            string content;
            try
            {
                // akses thread/search
                content = await WebClient.GetOrPostStringAsync(threadRequest);
                
                // cek html atau kode alien?
                if (!content.Contains("html"))
                    throw new Exception("Result bukan html!");
                
                // verifikasi login
                if (content.Contains(LoginPath))
                {
                    _log.Warning("Status: logout...");
                    _isLoggedIn = false;
                    
                    if (!threadMode)
                        throw new Exception("Search mode harus login!");
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(_message, "Mohon maaf...\nPlugin mobilism sedang mengalami gangguan.\nCobalah beberapa saat lagi.");
                return;
            }
            
            // regex tabel thread
            var findTable = Regex.Match(content, @"<table.+>([\s\S]+)<\/table>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!findTable.Success)
            {
                _log.Error("Tabel tidak ditemukan!");
                await Bot.SendTextAsync(_message, "Mohon maaf...\nPlugin mobilism tidak bisa membaca threads.");
                return;
            }
            
            const string patternSearch = "<td>\n.+\n.+\n" +
                                         "<a.+href=\"(.+)\".+topictitle.+>(.+)<\\/a>.+\n.+" +
                                         "<a.+username.+>(.+)<\\/a>.+" +
                                         "<small>(.+)<\\/small>\n.+Android.+\n" +
                                         "<\\/td>";
            const string patternThread = "<tr.+class(?:=\"\"|)>\n.+\n.+\n" +
                                         "<a.+href=\"(.+)\".+topictitle.+>(.+)<\\/a>\n.+\n.+" +
                                         "<a.+username.+>(.+)<\\/a>.+" +
                                         "<small>(.+)<\\/small>\n.+\n.+\n.+\n.+\n.+\n.+\n.+\n" +
                                         "<\\/tr>";
            
            // regex cari per-baris dalam tabel
            var table = findTable.Groups[1].Value;
            var rows = Regex.Matches(table, threadMode ? patternThread : patternSearch, RegexOptions.Multiline);
            if (rows.Count == 0)
            {
                _log.Warning("Rows tidak ditemukan!");
                await Bot.SendTextAsync(_message, $"Maaf kaka...\n{Bot.Name} gak nemu yg dicari." +
                                                  (threadMode ? "" : "\nCoba pakai keyword yg lain."));
                return;
            }

            var count = 1;
            var total = rows.Count;
            var padding = total.ToString().Length;
            var respon = "Mobilism : " + (isApp ? "Aplikasi\n" : "Permainan\n") +
                         (threadMode ? "" : $"Pencarian : {keywords}\n") +
                         "Hasil : " + (total < 10 ? total.ToString() : "10") + "/" + total.ToString() + " threads\n" +
                         "—— —— —— —— —— ——\n";
            
            foreach (Match row in rows)
            {
                // regex f= & t=
                string f = null, t = null;
                var mc = Regex.Matches(row.Groups[1].Value, @"([f|t]=[\d]+)");
                foreach (Match m in mc)
                {
                    var text = m.Groups[1].Value;
                    if (text.StartsWith("f")) f = text;
                    if (text.StartsWith("t")) t = text;
                }

                var link = $"http://forum.mobilism.org/viewtopic.php?{f}&{t}";
                var title = row.Groups[2].Value;
                var user = row.Groups[3].Value;
                var date = row.Groups[4].Value;

                // append result
                var num = count.ToString().PadLeft(padding, '0');
                respon += $"{num}. <a href=\"{link}\">{title}</a> by <b>{user}</b> — {date}.\n";

                if (count < 10) count++;
                else break;
            }
            
            var responFinal = respon + "—— —— —— —— —— ——\nUntuk link download, klik nomor dibawah :";

            var sentMessage = await Bot.SendTextAsync(_message, responFinal, parse: ParseMode.Html);
            if (sentMessage == null) return;
            
            var schedule = new ScheduleData()
            {
                ChatId = sentMessage.Chat.Id,
                MessageId = sentMessage.MessageId,
                DateTime = DateTime.Now.AddMinutes(10),
                Operation = ScheduleData.Type.Edit,
                Text = respon,
                ParseMode = ParseMode.Html
            };
            Schedule.RegisterNew(schedule);
        }
    }
}