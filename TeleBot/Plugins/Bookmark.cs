using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleBot.BotClass;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.Plugins
{
    public class Bookmark
    {
        private static Log _log = new Log("Bookmark");
        private static Database _db = new Database();
        private Message _message;
        private CallbackQuery _callback;
        private bool _callbackMode;

        public Bookmark(Message message, CallbackQuery callback = null)
        {
            _message = message;
            _callback = callback;
            if (callback != null)
            {
                _message.From = callback.From;
                _callbackMode = true;
            }
        }

        private static string NormalizeHashtag(string hashtag)
        {
            var result = hashtag
                .Replace("#", "")
                .Replace("-", "_").Trim()
                .Replace(" ", "_").TrimEnd('_');
            
            if (!hashtag.Equals(result))
                _log.Debug("Normalize: \"{0}\" » \"{1}\"", hashtag, result);

            return result;
        }

        private async Task<bool> IsNotHashtag(string hashtag)
        {
            if (string.IsNullOrWhiteSpace(hashtag))
            {
                _log.Warning("Hashtag kosong!", hashtag);
                await BotClient.SendTextAsync(_message, $"Cara penggunaan :\n1. Reply pesan,\n2. `/simpan nama_hashtag`!");
                return true;
            }
            
            if (Regex.IsMatch(hashtag, @"^#?([\w]+)$")) return false;
            
            _log.Warning("Format \"{0}\" salah!", hashtag);
            await BotClient.SendTextAsync(_message, $"Format ilegal! Hashtag \"{hashtag}\" tidak bisa dipakai.");
            return true;
        }
        
        private async Task<bool> IsNotInGroup()
        {
            if (_message.IsGroupChat()) return false;
            
            _log.Warning("Bukan grup!");
            await BotClient.SendTextAsync(_message, $"Perintah bookmark hanya bisa dipakai didalam grup!");
            return true;
        }

        private async Task<bool> IsNotAdmin()
        {
            if (_message.IsGodMode()) return false;

            if (await _message.IsAdminThisGroup()) return false;
            
            _log.Warning("User {0} bukan admin grup!", _message.FromName());
            await BotClient.SendTextAsync(_message, $"Maaf kaka... Kamu bukan admin grup ini!");
            return true;
        }

        public async void Save(string hashtag)
        {
            try
            {
                // normalize dan cek hashtag
                hashtag = NormalizeHashtag(hashtag);
                if (await IsNotHashtag(hashtag)) return;
                
                // harus grup chat
                if (await IsNotInGroup()) return;

                // harus mereply pesan
                if (!_message.IsReplyToMessage())
                {
                    _log.Warning("Tidak ada pesan yang akan disimpan!");
                    await BotClient.SendTextAsync(_message, $"Tidak ada pesan yg akan disimpan!");
                    return;
                }
                
                // cek admin atau bukan
                if (await IsNotAdmin()) return;
                
                var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
                if (query == null)
                {
                    var hash = new Hashtag()
                    {
                        ChatId = _message.Chat.Id,
                        MessageId = _message.ReplyToMessage.MessageId,
                        KeyName = hashtag,
                        ByName = _message.FromName(true),
                        DateTime = DateTime.Now.ToString(CultureInfo.CurrentCulture)
                    };
                    
                    _log.Debug("Simpan #{0} ({1}) ke {2}", hashtag, hash.MessageId, _message.ChatName());
                
                    await _db.InsertBookmark(hash);
                    await BotClient.SendTextAsync(_message.Chat.Id,
                        $"<b>Simpan Bookmark!</b>\n—— —— —— ——\n" +
                        $"MessageId : {hash.MessageId}\n" +
                        $"Hashtag : #{hashtag}\n—— —— —— ——\n" +
                        $"Hasil : Tersimpan.",
                        _message.ReplyToMessage.MessageId,
                        ParseMode.Html);
                }
                else
                {
                    query.MessageId = _message.ReplyToMessage.MessageId;
                    query.KeyName = hashtag;
                
                    _log.Debug("Ganti #{0} ({1}) dari {2}", hashtag, query.MessageId, _message.ChatName());
                    
                    await _db.InsertBookmark(query, true);
                    await BotClient.SendTextAsync(_message.Chat.Id,
                        $"<b>Simpan Bookmark!</b>\n—— —— —— ——\n" +
                        $"MessageId : {query.MessageId}\n" +
                        $"Hashtag : #{hashtag}\n—— —— —— ——\n" +
                        $"Hasil : Telah diganti.",
                        _message.ReplyToMessage.MessageId,
                        ParseMode.Html);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await BotClient.SendTextAsync(_message, $"Gagal menyimpan #{hashtag}!\n—— —— —— ——\nError : {e.Message}");
            }
        }

        public async Task Delete(string hashtag)
        {
            if (!_callbackMode)
            {
                // normalize dan cek hashtag
                hashtag = NormalizeHashtag(hashtag);
                if (await IsNotHashtag(hashtag)) return;

                // harus grup chat
                if (await IsNotInGroup()) return;
            }
            
            // cek admin atau bukan
            if (await IsNotAdmin()) return;

            var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
            if (query == null)
            {
                _log.Ignore("Tidak ada #{0} di {1}", hashtag, _message.ChatName());
                
                await BotClient.SendTextAsync(_message, $"Tidak ada #{hashtag} di grup ini.");
                return;
            }

            try
            {
                _log.Debug("Hapus #{0} dari {1}", hashtag, _message.ChatName());
                
                await _db.DeleteBookmark(query);
                
                await BotClient.SendTextAsync(_message,
                    $"{_message.FromNameWithMention(ParseMode.Html)} telah menghapus #{hashtag}!",
                    parse: ParseMode.Html);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await BotClient.SendTextAsync(_message, $"Gagal hapus #{hashtag}!\nError : {e.Message}");
            }
        }

        public async void DeleteWithButton(string hashtag, bool final = false)
        {
            if (!_callbackMode) return;
            if (_message.ReplyToMessage.From.Id == _callback.From.Id)
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
            }
            else
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, BotResponse.NoAccessToButton(), true);
                return;
            }

            if (!final)
            {
                var buttons = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("Yakin", $"cmd=remove-final&data={hashtag}"),
                    InlineKeyboardButton.WithCallbackData("Tidak", $"cmd=manage&data=null")
                });
                
                await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                    $"Apakah kamu yakin mau menghapus #{hashtag}?",
                    button: buttons);
            }
            else
            {
                await Delete(hashtag);
                ManageList();
            }
        }

        public async void GenerateList(bool useButton, bool update = false)
        {
            if (!update)
            {
                // harus grup chat
                if (await IsNotInGroup()) return;
            }
            else if (_callbackMode)
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
            }

            var list = await _db.GetBookmarks(_message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", _message.ChatName());

                var respon = "Bookmark/Hashtag *kosong*.\nTapi kamu bisa panggil semua admin di grup ini dengan #admin atau #mimin.";
                await BotClient.SendTextAsync(_message, respon, parse: ParseMode.Markdown);
                
                return;
            }

            if (!useButton)
            {
                var count = 1;
                var padding = list.Count.ToString().Length;
                var respon = $"<b>Daftar Hashtag</b>\nGenerated : {DateTime.Now}\nTotal : {list.Count}\n—— —— —— ——";
                foreach (var hashtag in list.OrderBy(h => h.KeyName))
                {
                    var num = count.ToString().PadLeft(padding, '0');
                    respon += $"\n{num}. #{hashtag.KeyName}";
                    count++;
                }
            
                var sentMessage = await BotClient.SendTextAsync(_message, respon, parse: ParseMode.Html);
                if (sentMessage == null) return;
                var schedule = new ScheduleData()
                {
                    ChatId = _message.Chat.Id,
                    MessageId = sentMessage.MessageId,
                    DateTime = DateTime.Now.AddMinutes(10),
                    Operation = ScheduleData.Type.Edit,
                    Text = "Daftar hashtag kadaluarsa!\nGunakan /hashtag untuk melihat daftar lagi."
                };
                Schedule.RegisterNew(schedule);
            }
            else
            {
                var respon = $"<b>Daftar Bookmark</b>\nGenerated : {DateTime.Now}\nTotal : {list.Count}";
                var buttonRows = new List<List<InlineKeyboardButton>>();
                
                // tombol update -- atas
                var updateButton = new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Perbarui Bookmark 🔄", $"cmd=generate&data=null")
                };
                buttonRows.Add(updateButton);
                
                foreach (var hashtag in list.OrderBy(h => h.KeyName))
                {
                    var buttonColumns = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData(hashtag.KeyName, $"cmd=call&data=" + hashtag.KeyName)
                    };
                    buttonRows.Add(buttonColumns);
                }
                
                // tombol update -- bawah
                buttonRows.Add(updateButton);
                
                var buttons = new InlineKeyboardMarkup(buttonRows.ToArray());
                
                if (!update)
                    await BotClient.SendTextAsync(_message, respon, parse: ParseMode.Html, button: buttons);
                else
                    await BotClient.EditOrSendTextAsync(_message, _message.MessageId, respon, ParseMode.Html, buttons);
            }
        }

        public async void ManageList()
        {
            if (!_callbackMode)
            {
                // harus grup chat
                if (await IsNotInGroup()) return;

                // cek admin atau bukan
                if (await IsNotAdmin()) return;
            }
            else
            {
                if (_message.ReplyToMessage.From.Id == _callback.From.Id)
                {
                    await BotClient.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
                }
                else
                {
                    await BotClient.AnswerCallbackQueryAsync(_callback.Id, BotResponse.NoAccessToButton(), true);
                    return;
                }
            }

            var list = await _db.GetBookmarks(_message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", _message.ChatName());

                var norespon = "Bookmark/Hashtag *kosong*.";
                if (_callbackMode)
                    await BotClient.EditOrSendTextAsync(_message, _message.MessageId, norespon, ParseMode.Markdown);
                else
                    await BotClient.SendTextAsync(_message, norespon, parse: ParseMode.Markdown);
                
                return;
            }
            
            var respon = $"<b>Kelola Bookmark</b>\n" +
                         $"Total : {list.Count} hashtag";
            var buttonRows = new List<List<InlineKeyboardButton>>();
            foreach (var hashtag in list.OrderBy(h => h.KeyName))
            {
                var buttonColumns = new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData(hashtag.KeyName, $"cmd=info&data={hashtag.KeyName}"),
                    InlineKeyboardButton.WithCallbackData("Hapus", $"cmd=remove&data={hashtag.KeyName}")
                };
                buttonRows.Add(buttonColumns);
            }
            
            var buttons = new InlineKeyboardMarkup(buttonRows.ToArray());
            Message sentMessage;
            if (_callbackMode)
                sentMessage = await BotClient.EditOrSendTextAsync(_message, _message.MessageId, respon, ParseMode.Html, buttons);
            else
                sentMessage = await BotClient.SendTextAsync(_message, respon, true, ParseMode.Html, buttons);
            
            if (sentMessage == null) return;
            
            // schedule edit pesan keluar
            var schedule = new ScheduleData()
            {
                ChatId = sentMessage.Chat.Id,
                MessageId = sentMessage.MessageId,
                DateTime = DateTime.Now.AddMinutes(30),
                Operation = ScheduleData.Type.Edit,
                Text = "Perintah telah kadaluarsa.",
                ParseMode = ParseMode.Html
            };
            Schedule.RegisterNew(schedule);
        }

        public async void FindHashtags()
        {
            if (string.IsNullOrWhiteSpace(_message.Text)) return;
            
            // harus grup chat
            if (!_message.IsGroupChat()) return;
            
            _log.Debug("Cari hashtag dalam teks : {0}", _message.Text);
            var matches = Regex.Matches(_message.Text, @"#([\w\d]+)");
            foreach (Match match in matches)
            {
                var hashtag = match.Groups[1].Value;
                
                // panggil admin/mimin
                if (hashtag == "admin" || hashtag == "mimin")
                {
                    _log.Debug("Panggil admins grup!", hashtag);
                    
                    var admins = await BotClient.GetChatAdministratorsAsync(_message);
                    admins = admins.OrderBy(x => x.User.FirstName).ToArray();
                    var respon = "Panggilan kepada :";
                    foreach (var x in admins)
                    {
                        var user = (x.User.FirstName + " " + x.User.LastName).Trim();
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Username;
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Id.ToString();
                        respon += $"\n• <a href=\"tg://user?id={x.User.Id}\">{user.Trim()}</a>";
                    }
                    
                    await BotClient.SendTextAsync(_message, respon, parse: ParseMode.Html);
                    continue;
                }

                // cari hashtag
                FindHashtag(hashtag);
            }
        }

        public async void FindHashtag(string hashtag)
        {
            var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
            if (query == null)
            {
                if (_callbackMode)
                    await BotClient.AnswerCallbackQueryAsync(_callback.Id, $"Tidak ada #{hashtag} di grup ini!", true);
                
                return;
            }
            
            _log.Debug("Panggil hashtag #{0}", hashtag);
            var sentMessage = await BotClient.ForwardMessageAsync(_message.Chat.Id, query.ChatId, query.MessageId);

            if (!_callbackMode) return;

            if (sentMessage != null)
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, $"Hashtag #{hashtag} pesenan kaka udah tak siapin..", true);
                await BotClient.SendTextAsync(_message,
                    $"Buat kaka {_message.FromNameWithMention(ParseMode.Html)},\n" +
                    $"itu {hashtag} udah tak siapin...", parse: ParseMode.Html);
            }
            else
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, $"Hashtag #{hashtag} pesenan kaka gak bisa diforward 😔", true);
            }
        }

        public async void ShowInfo(string hashtag)
        {
            if (!_callbackMode) return;
            
            var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
            if (query == null)
            {
                await BotClient.AnswerCallbackQueryAsync(_callback.Id, $"Tidak ada #{hashtag} di grup ini!", true);
                
                return;
            }
            
            await BotClient.AnswerCallbackQueryAsync(_callback.Id,
                $"Hashtag : {query.KeyName}\n" +
                $"MessageId : {query.MessageId}\n" +
                $"Set By : {query.ByName}\n" +
                $"DateTime : {query.DateTime}", true);
        }
    }
}