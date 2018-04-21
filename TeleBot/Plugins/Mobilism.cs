using System;
using System.Collections.Generic;
using System.Linq;
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
using Telegram.Bot.Types.ReplyMarkups;
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
        
        private class Thread
        {
            public string LinkPath { get; set; }
            public string Title { get; set; }
            public string Username { get; set; }
            public string PostDate { get; set; }
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
            var maximum = 10;
            var threads = new List<Thread>();
            foreach (Match row in rows)
            {
                var f = string.Empty;
                var t = string.Empty;
                
                // regex f=number & t=number
                var matches = Regex.Matches(row.Groups[1].Value, @"([f|t]=[\d]+)");
                foreach (Match match in matches)
                {
                    var text = match.Groups[1].Value;
                    if (text.StartsWith("f")) f = text;
                    if (text.StartsWith("t")) t = text;
                }

                var thread = new Thread
                {
                    LinkPath = $"/viewtopic.php?{f}&{t}",
                    Title = row.Groups[2].Value,
                    Username = row.Groups[3].Value,
                    PostDate = row.Groups[4].Value
                };
                threads.Add(thread);
                
                if (count < maximum) count++;
                else break;
            }
            
            var total = threads.Count;
            var padding = total.ToString().Length;
            var respon = "<b>Mobilism</b> : " + (isApp ? "Aplikasi\n" : "Permainan\n") +
                         (threadMode ? "" : $"<b>Pencarian</b> : {keywords}\n") +
                         "<b>Hasil</b> : " + (total < 10 ? total.ToString() : "10") + "/" + total.ToString() + " threads\n" +
                         "—— —— —— —— —— ——\n";

            count = 1;
            foreach (var thread in threads)
            {
                // tambahkan ke teks respon
                var num = count.ToString().PadLeft(padding, '0');
                respon += $"<b>{num}</b>. <a href=\"{BaseAddress}{thread.LinkPath}\">{thread.Title}</a> " +
                          $"by <b>{thread.Username}</b> — {thread.PostDate}.\n";
                count++;
            }
            
            var buttonRows = new List<List<InlineKeyboardButton>>();
            if (threads.Count > (maximum / 2))
            {
                // reset counter
                count = 1;
                
                // button baris 1
                var partOne = threads.Take(total / 2).ToList();
                var buttonRowOne = new List<InlineKeyboardButton>();
                foreach (var thread in partOne)
                {
                    var num = count.ToString().PadLeft(padding, '0');
                    var button = InlineKeyboardButton.WithCallbackData(num, "cmd=mobilism&data=" + thread.LinkPath);
                    buttonRowOne.Add(button);
                    count++;
                }
                buttonRows.Add(buttonRowOne);
                
                // button baris 2
                var partTwo = threads.Skip(total / 2).ToList();
                var buttonRowTwo = new List<InlineKeyboardButton>();
                foreach (var thread in partTwo)
                {
                    var num = count.ToString().PadLeft(padding, '0');
                    var button = InlineKeyboardButton.WithCallbackData(num, "cmd=mobilism&data=" + thread.LinkPath);
                    buttonRowTwo.Add(button);
                    count++;
                }
                buttonRows.Add(buttonRowTwo);
            }
            else
            {
                // reset counter
                count = 1;
                
                // button jadi 1 baris
                var buttonRow = new List<InlineKeyboardButton>();
                foreach (var thread in threads)
                {
                    var num = count.ToString().PadLeft(padding, '0');
                    var buttonColumn = InlineKeyboardButton.WithCallbackData(num, "cmd=mobilism&data=" + thread.LinkPath);
                    buttonRow.Add(buttonColumn);
                    count++;
                }
                buttonRows.Add(buttonRow);
            }
            
            var responWithButtons = respon + "—— —— —— —— —— ——\nLink download, pilih nomor dibawah :";
            var buttons = new InlineKeyboardMarkup(buttonRows.ToArray());

            var sentMessage = await Bot.SendTextAsync(_message, responWithButtons, parse: ParseMode.Html, button: buttons);
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