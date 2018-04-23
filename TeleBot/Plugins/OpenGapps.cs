using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TeleBot.BotClient;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.Plugins
{
    public class OpenGapps
    {
        private static Log _log = new Log("OpenGapps");
        private const string apiUrl = "https://api.github.com/repos/opengapps/arch/releases/latest?per_page=1";
        private Message _message;
        private CallbackQuery _callback;

        #region Gapps Class

        private class Gapps
        {
            public List<Assets> Assets { get; set; }
        }
        
        private class Assets
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Size { get; set; }
            
            [JsonProperty("download_count")]
            public int DownloadCount { get; set; }
            
            [JsonProperty("created_at")]
            public DateTime CreateAt { get; set; }
            
            [JsonProperty("updated_at")]
            public DateTime UpdateAt { get; set; }
            
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; set; }
        }

        #endregion
        
        public OpenGapps(Message message, CallbackQuery callback = null)
        {
            _message = message;
            _callback = callback;
        }

        public async void SelectArch(bool edit = false)
        {
            _log.Debug("Pilih arsitektur...");
            
            const string cmd = "cmd=android";
            
            var buttons = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("arm", $"{cmd}&data=arm"),
                    InlineKeyboardButton.WithCallbackData("arm64", $"{cmd}&data=arm64"),
                    InlineKeyboardButton.WithCallbackData("x86", $"{cmd}&data=x86"),
                    InlineKeyboardButton.WithCallbackData("x86_x64", $"{cmd}&data=x86_x64")
                }
            });
            
            if (!edit)
                await Bot.SendTextAsync(_message,
                    "<b>OpenGapps</b>\n—— —— ——\nArsitektur :", true, ParseMode.Html, buttons);
            else
                await Bot.EditOrSendTextAsync(_message, _message.MessageId,
                    "<b>OpenGapps</b>\n—— —— ——\nArsitektur :", ParseMode.Html, buttons);
        }

        public async void SelectAndroid(string arch)
        {
            if (_message.ReplyToMessage.From.Id != _callback.From.Id)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Apaan sih pencet-pencet... Geli tauu!!", true);
                return;
            }
            
            _log.Debug("Arch: {0} | Pilih versi android...", arch);
            await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Silahkan pilih versi android!");
            
            const string cmd = "cmd=variant";
            
            var android = new List<List<InlineKeyboardButton>>();
            if (!arch.Contains("64"))
            {
                var kitkat = new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("4.4", $"{cmd}&data={arch}-4.4")
                };
                android.Add(kitkat);
            }
            
            var lolipop = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("5.0", $"{cmd}&data={arch}-5.0"),
                InlineKeyboardButton.WithCallbackData("5.1", $"{cmd}&data={arch}-5.1")
            };
            android.Add(lolipop);
                
            var marshmallow = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("6.0", $"{cmd}&data={arch}-6.0")
            };
            android.Add(marshmallow);
                
            var naugat = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("7.0", $"{cmd}&data={arch}-7.0"),
                InlineKeyboardButton.WithCallbackData("7.1", $"{cmd}&data={arch}-7.1")
            };
            android.Add(naugat);
                
            var oreo = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("8.0", $"{cmd}&data={arch}-8.0"),
                InlineKeyboardButton.WithCallbackData("8.1", $"{cmd}&data={arch}-8.1")
            };
            android.Add(oreo);
                
            var back = new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("Kembali", $"cmd=arch&data=null")
            };
            android.Add(back);
            
            var buttons = new InlineKeyboardMarkup(android.ToArray());
            await Bot.EditOrSendTextAsync(_message, _message.MessageId,
                $"<b>OpenGapps</b>\n" +
                $"—— —— ——\n" +
                $"Arsitektur : <code>{arch}</code>\n" +
                $"—— —— ——\n" +
                $"Android :",
                ParseMode.Html, buttons);
        }

        public async void SelectVariant(string data)
        {
            if (_message.ReplyToMessage.From.Id != _callback.From.Id)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Apaan sih pencet-pencet... Geli tauu!!", true);
                return;
            }

            var regex = Regex.Match(data, @"(\w+)-([\w\.]+)", RegexOptions.IgnoreCase);
            if (!regex.Success)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Tidak bisa pilih paket!", true);
                return;
            }

            var arch = regex.Groups[1].Value;
            var android = regex.Groups[2].Value;
            
            _log.Debug("Arch: {0} | Android: {1} | Pilih variasi gapps...", arch, android);
            await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Silahkan pilih variasi!");

            const string cmd = "cmd=gapps";
            
            var package = new List<List<InlineKeyboardButton>>();
            var pico = InlineKeyboardButton.WithCallbackData("Pico", $"{cmd}&data={arch}-{android}-pico");
            var nano = InlineKeyboardButton.WithCallbackData("Nano", $"{cmd}&data={arch}-{android}-nano");
            var micro = InlineKeyboardButton.WithCallbackData("Micro", $"{cmd}&data={arch}-{android}-micro");
            var mini = InlineKeyboardButton.WithCallbackData("Mini", $"{cmd}&data={arch}-{android}-mini");
            var full = InlineKeyboardButton.WithCallbackData("Full", $"{cmd}&data={arch}-{android}-full");
            var stock = InlineKeyboardButton.WithCallbackData("Stock", $"{cmd}&data={arch}-{android}-stock");
            var super = InlineKeyboardButton.WithCallbackData("Super", $"{cmd}&data={arch}-{android}-super");
            var aroma = InlineKeyboardButton.WithCallbackData("Aroma", $"{cmd}&data={arch}-{android}-aroma");
            var back = InlineKeyboardButton.WithCallbackData("Kembali", $"cmd=android&data={arch}");

            if (arch.Equals("arm"))
            {
                if (android.Equals("4.4") || android.Equals("5.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                }
                
                if (android.Equals("5.1") || android.Equals("6.0") || android.Equals("7.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                    package.Add(new List<InlineKeyboardButton>() { stock, aroma });
                }

                if (android.Equals("7.1") || android.Equals("8.0") || android.Equals("8.1"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                    package.Add(new List<InlineKeyboardButton>() { micro, mini });
                    package.Add(new List<InlineKeyboardButton>() { full, stock });
                    package.Add(new List<InlineKeyboardButton>() { super, aroma });
                }
            }

            if (arch.Equals("arm64"))
            {
                if (android.Equals("5.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                }

                if (android.Equals("5.1") || android.Equals("6.0") || android.Equals("7.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                    package.Add(new List<InlineKeyboardButton>() { stock, aroma });
                }

                if (android.Equals("7.1") || android.Equals("8.0") || android.Equals("8.1"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                    package.Add(new List<InlineKeyboardButton>() { micro, mini });
                    package.Add(new List<InlineKeyboardButton>() { full, stock });
                    package.Add(new List<InlineKeyboardButton>() { super, aroma });
                }
            }

            if (arch.Equals("x86"))
            {
                if (android.Equals("4.4") || android.Equals("5.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                }

                if (android.Equals("5.1") || android.Equals("6.0") || android.Equals("7.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano, stock });
                }

                if (android.Equals("7.1") || android.Equals("8.0") || android.Equals("8.1"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico });
                    package.Add(new List<InlineKeyboardButton>() { nano, micro });
                    package.Add(new List<InlineKeyboardButton>() { mini, full });
                    package.Add(new List<InlineKeyboardButton>() { stock, super });
                }
            }

            if (arch.Equals("x86_x64"))
            {
                if (android.Equals("5.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano });
                }
                
                if (android.Equals("5.1") || android.Equals("6.0") || android.Equals("7.0"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico, nano, stock });
                }

                if (android.Equals("7.1") || android.Equals("8.0") || android.Equals("8.1"))
                {
                    package.Add(new List<InlineKeyboardButton>() { pico });
                    package.Add(new List<InlineKeyboardButton>() { nano, micro });
                    package.Add(new List<InlineKeyboardButton>() { mini, full });
                    package.Add(new List<InlineKeyboardButton>() { stock, super });
                }
            }
            
            package.Add(new List<InlineKeyboardButton>() { back });
            var buttons = new InlineKeyboardMarkup(package.ToArray());
            await Bot.EditOrSendTextAsync(_message, _message.MessageId,
                $"<b>OpenGapps</b>\n" +
                $"—— —— ——\n" +
                $"Arsitektur : <code>{arch}</code>\n" +
                $"Android : <code>{android}</code>\n" +
                $"—— —— ——\n" +
                $"Variasi :",
                ParseMode.Html, buttons);
        }

        public async void GetLatestRelease(string data)
        {
            if (_message.ReplyToMessage.From.Id != _callback.From.Id)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Apaan sih pencet-pencet... Geli tauu!!", true);
                return;
            }

            var regex = Regex.Match(data, @"(\w+)-([\w\.]+)-(\w+)", RegexOptions.IgnoreCase);
            if (!regex.Success)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Ada sesuatu yang aneh! Coba lain waktu.", true);
                return;
            }

            var arch = regex.Groups[1].Value;
            var android = regex.Groups[2].Value;
            var variant = regex.Groups[3].Value;
            
            _log.Debug("Arch: {0} | Android: {1} | Package {2} | Cari gapps...", arch, android, variant);
            
            Gapps gapps;
            
            try
            {
                var request = new WebRequest()
                {
                    Url = apiUrl.Replace("arch", arch),
                    Method = WebMethod.Get
                };
                var response = await WebClient.GetOrPostStringAsync(request);

                Dump.ToFile("opengapps.json", response);
                
                gapps = JsonConvert.DeserializeObject<Gapps>(response);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                
                await Bot.AnswerCallbackQueryAsync(_callback.Id,
                    "Mohon maaf...\nPlugin opengapps saat ini sedang mengalami gangguan!", true);
                
                return;
            }

            if (gapps.Assets.Count == 0)
            {
                _log.Warning("Gapps asset tidak ditemukan");
                
                await Bot.AnswerCallbackQueryAsync(_callback.Id,
                    "Mohon maaf...\nPlugin opengapps tidak menemukan asset yg dicari 🙁", true);
                
                return;
            }

            var releases = gapps.Assets.ToList()
                .Where(g => g.Name.Contains(android) && g.Name.Contains(variant) && g.Name.EndsWith(".zip"));

            var release = releases.FirstOrDefault();
            if (release == null)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id,
                    "Mohon maaf...\nKriteria yang dicari tidak ada 🙁", true);
                return;
            }
            
            await Bot.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");

            await Bot.EditOrSendTextAsync(_message, _message.MessageId,
                $"<b>OpenGapps</b>\n" +
                $"—— —— ——\n" +
                $"Arsitektur : <code>{arch}</code>\n" +
                $"Android : <code>{android}</code>\n" +
                $"Variasi : <code>{variant}</code>\n" +
                $"—— —— ——\n" +
                $"File Size : <code>{release.Size.ToHumanSizeFormat()}</code>\n" +
                $"File Link :\n" +
                $"» <a href=\"{release.DownloadUrl}\">{release.Name}</a>",
                ParseMode.Html, preview: false);
        }
    }
}