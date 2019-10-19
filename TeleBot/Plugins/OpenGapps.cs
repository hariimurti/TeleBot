using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.Plugins
{
    public class OpenGapps
    {
        private static Log _log = new Log("OpenGapps");
        private Message _message;

        #region OpenGapps Json

        public class Variant
        {
            public string name { get; set; }
            public string zip { get; set; }
            public int zip_size { get; set; }
            public string md5 { get; set; }
            public string version_info { get; set; }
            public string source_report { get; set; }
        }

        public class Variants
        {
            public IList<Variant> variants { get; set; }
        }

        public class Apis
        {
            [JsonProperty(PropertyName = "4.4")]
            public Variants android_44 { get; set; }
            [JsonProperty(PropertyName = "5.0")]
            public Variants android_50 { get; set; }
            [JsonProperty(PropertyName = "5.1")]
            public Variants android_51 { get; set; }
            [JsonProperty(PropertyName = "6.0")]
            public Variants android_60 { get; set; }
            [JsonProperty(PropertyName = "7.0")]
            public Variants android_70 { get; set; }
            [JsonProperty(PropertyName = "7.1")]
            public Variants android_71 { get; set; }
            [JsonProperty(PropertyName = "8.0")]
            public Variants android_80 { get; set; }
            [JsonProperty(PropertyName = "8.1")]
            public Variants android_81 { get; set; }
            [JsonProperty(PropertyName = "9.0")]
            public Variants android_90 { get; set; }
            [JsonProperty(PropertyName = "10.0")]
            public Variants android_100 { get; set; }
        }

        public class Arch
        {
            public Apis apis { get; set; }
            public string date { get; set; }
            public string human_date { get; set; }
        }

        public class Archs
        {
            public Arch arm { get; set; }
            public Arch arm64 { get; set; }
            public Arch x86 { get; set; }
            public Arch x86_64 { get; set; }
        }

        public class Gapps
        {
            public Archs archs { get; set; }
        }

        #endregion

        public OpenGapps(Message message)
        {
            _message = message;
        }

        private async void SendUsage()
        {
            var arch = string.Empty;
            foreach (var prop in typeof(Archs).GetProperties())
                arch = arch.JoinWithComma($"`{prop.Name}`");

            var android = string.Empty;
            foreach (var prop in typeof(Apis).GetProperties())
            {
                var name = prop.GetCustomAttribute<JsonPropertyAttribute>().PropertyName;
                android = android.JoinWithComma($"`{name}`");
            }

            var variant = "`pico`, `nano`, `micro`, `mini`, `full`, `stock`, `super`, `aroma`, `tvstock`";

            await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                    $"*OpenGapps*\n" +
                    $"—— Opsi ——\n" +
                    $"Platform : {arch}.\n" +
                    $"Android : {android}.\n" +
                    $"Variant : {variant}.\n" +
                    $"—— Penggunaan ——\n" +
                    $"`/gapps platform android`\n" +
                    $"`/gapps platform android variant`\n" +
                    $"—— Contoh ——\n" +
                    $"`/gapps arm 7.1`\n" +
                    $"`/gapps arm64 9.0 pico`",
                    ParseMode.Markdown, preview: false);
        }

        public async void GetLatestRelease(string data)
        {
            Gapps gapps;
            var regex = Regex.Match(data.ToLower(), @"([armx864_]+) ?([0-9]{1,2}.[0-9])? ?([a-zA-Z]+)?", RegexOptions.IgnoreCase);
            if (!regex.Success)
            {
                SendUsage();
                return;
            }

            var platform = regex.Groups[1].Value;
            var android = regex.Groups[2].Value;
            var variant = regex.Groups[3].Value;

            if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(android))
            {
                await BotClient.SendTextAsync(_message, "Kriteria minimal belum terpenuhi!\nGunakan /gapps untuk lebih jelasnya.");
                return;
            }

            _log.Debug("Platform: {0} | Android: {1} | Variant: {2} | Cari gapps...", platform, android, variant);

            try
            {
                var client = new RestClient("https://api.opengapps.org/list");
                var req = new RestRequest();
                req.AddHeader("Sec-Fetch-Mode", "cors");
                req.AddHeader("Referer", "https://opengapps.org/");
                req.AddHeader("Origin", "https://opengapps.org");
                req.AddHeader("User-Agent", App.UserAgent);
                req.AddHeader("DNT", "1");

                _message = await BotClient.SendTextAsync(_message, "Tunggu sebentar, aku carikan di websitenya dulu...");

                _log.Debug("Getting json from api...");
                var res = await client.ExecuteTaskAsync(req);
                gapps = JsonConvert.DeserializeObject<Gapps>(res.Content);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);

                await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                    "Mohon maaf...\nPlugin opengapps saat ini sedang mengalami gangguan!");

                return;
            }

            var textResult = $"*OpenGapps*\n—— —— ——\n" +
                $"Platform : `{platform}`\n" +
                $"Android : `{android}`\n";

            if (string.IsNullOrWhiteSpace(variant))
                textResult += $"Variant : `all`";
            else
                textResult += $"Variant : `{variant}`";

            var listGapps = new List<Tuple<string, string>>();
            foreach (var propArch in typeof(Archs).GetProperties())
            {
                if (propArch.Name != platform) continue;

                var arch = (Arch)propArch.GetValue(gapps.archs);
                if (arch == null) continue;

                textResult += $"\nRelease : `{arch.date}`";
                foreach (var propAndroid in typeof(Apis).GetProperties())
                {
                    var version = propAndroid.GetCustomAttribute<JsonPropertyAttribute>().PropertyName;
                    if (version != android) continue;

                    var api = (Variants)propAndroid.GetValue(arch.apis);
                    if (api == null) continue;

                    foreach (var package in api.variants)
                    {
                        if (!string.IsNullOrWhiteSpace(variant) && package.name != variant) continue;

                        listGapps.Add(new Tuple<string, string>(package.name, package.zip));
                    }
                }
            }

            var buttonRows = new List<List<InlineKeyboardButton>>();
            if (listGapps.Count == 0)
            {
                textResult += $"\n—— —— ——\nTidak ada atau tidak ketemu!";
                var buttonLink = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithUrl("Visit Website!", "https://opengapps.org")
                        };
                buttonRows.Add(buttonLink);
            }
            else if (listGapps.Count == 1)
            {
                var name = Path.GetFileName(listGapps[0].Item2);
                var buttonLink = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithUrl(name, listGapps[0].Item2)
                        };
                buttonRows.Add(buttonLink);
            }
            else
            {
                var skip = 0;
                while (true)
                {
                    var take = listGapps.Skip(skip).Take(3);
                    if (take.Count() == 0) break;

                    var buttonRow = new List<InlineKeyboardButton>();
                    foreach (var t in take)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithUrl(t.Item1, t.Item2));
                    }

                    buttonRows.Add(buttonRow);
                    skip += 3;
                }
            }

            var buttons = new InlineKeyboardMarkup(buttonRows.ToArray());
            await BotClient.EditOrSendTextAsync(_message, _message.MessageId, textResult, ParseMode.Markdown, buttons, false);
        }
    }
}