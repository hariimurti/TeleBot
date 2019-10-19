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
                    $"\n—— Opsi ——\n" +
                    $"Platform : {arch}.\n" +
                    $"Android : {android}.\n" +
                    $"Variant : {variant}.\n" +
                    $"\n—— Penggunaan ——\n" +
                    $"`/gapps platform android`\n" +
                    $"`/gapps platform android variant`\n" +
                    $"\n—— Contoh ——\n" +
                    $"`/gapps arm 7.1`\n" +
                    $"`/gapps arm64 9.0 pico`",
                    ParseMode.Markdown, preview: false);
        }

        public async void GetLatestRelease(string data)
        {
            var regex = Regex.Match(data.ToLower(), @"([armx864_]+) ?([0-9.]+)? ?([a-zA-Z]+)?", RegexOptions.IgnoreCase);
            if (!regex.Success)
            {
                SendUsage();
                return;
            }

            var arch = regex.Groups[1].Value;
            var android = regex.Groups[2].Value;
            var variant = regex.Groups[3].Value;

            if (string.IsNullOrWhiteSpace(arch) || string.IsNullOrWhiteSpace(android))
            {
                await BotClient.SendTextAsync(_message, "Kriteria minimal belum terpenuhi!\nGunakan /gapps untuk lebih jelasnya.");
                return;
            }

            _log.Debug("Platform: {0} | Android: {1} | Variant: {2} | Cari gapps...", arch, android, variant);

            Gapps gapps;

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

            var found = false;
            var result = $"<b>OpenGapps</b>\n\n—— —— ——\n";
            foreach (var pArch in typeof(Archs).GetProperties())
            {
                var gArch = (Arch)pArch.GetValue(gapps.archs);
                if (gArch == null) continue;
                if (pArch.Name.ToLower() != arch) continue;

                result += $"Release Date : <code>{gArch.human_date}</code>\n" +
                    $"Platform : <code>{pArch.Name}</code>\n";
                foreach (var pAndroid in typeof(Apis).GetProperties())
                {
                    var androidName = pAndroid.GetCustomAttribute<JsonPropertyAttribute>().PropertyName;
                    if (androidName != android) continue;

                    var gAndroid = (Variants)pAndroid.GetValue(gArch.apis);
                    if (gAndroid == null) continue;

                    result += $"Android : <code>{androidName}</code>\nVariant : ";
                    foreach (var gVariant in gAndroid.variants)
                    {
                        var zip = Path.GetFileName(gVariant.zip);
                        if (string.IsNullOrWhiteSpace(variant))
                        {
                            if (!found)
                                result += "<code>all</code>\n\n—— Downloads ——";

                            found = true;
                            result += $"\n• <a href=\"{gVariant.zip}\">{zip}</a>";
                        }
                        else if (gVariant.name == variant)
                        {
                            found = true;
                            result += $"<code>{gVariant.name}</code>\n\n" +
                                $"—— Downloads ——\n" +
                                $"Link : <a href=\"{gVariant.zip}\">{zip}</a>";
                        }
                    }
                }
            }

            if (!found)
            {
                await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                    "Mohon maaf... Kriteria yang dicari tidak ada atau tidak ketemu 🙁");
                return;
            }

            await BotClient.EditOrSendTextAsync(_message, _message.MessageId, result, ParseMode.Html, preview: false);
        }
    }
}