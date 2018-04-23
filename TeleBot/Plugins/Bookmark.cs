using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleBot.BotClient;
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

        private async Task<bool> CheckingHashtag(string hashtag)
        {
            if (Regex.IsMatch(hashtag, @"^#?([\w]+)$")) return true;
            
            _log.Warning("Format hashtag \"{0}\" salah!", hashtag);
            await Bot.SendTextAsync(_message, $"Hashtag \"{hashtag}\" pakai format ilegal!");
            return false;
        }
        
        private async Task<bool> CheckingGroup()
        {
            if (_message.IsGroupChat()) return true;
            
            _log.Warning("Khusus digunakan didalam grup!");
            await Bot.SendTextAsync(_message, $"Perintah bookmark hanya bisa dipakai didalam grup!");
            return false;
        }

        public async void Save(string hashtag)
        {
            try
            {
                hashtag = hashtag.TrimStart('#');
                if (string.IsNullOrWhiteSpace(hashtag)) return;
                
                // harus grup chat
                if (!await CheckingGroup()) return;

                // harus mereply pesan
                if (!_message.IsReplyToMessage())
                {
                    _log.Warning("Tidak ada pesan yang akan disimpan!");
                    await Bot.SendTextAsync(_message, $"Tidak ada pesan yg akan disimpan!");
                    return;
                }
                
                // cek format hashtag
                if (!await CheckingHashtag(hashtag)) return;
                
                // cek admin atau bukan
                if (!await _message.IsAdminThisGroup())
                {
                    _log.Warning("User {0} bukan admin grup!", _message.FromName());
                    await Bot.SendTextAsync(_message, $"Maaf kaka... Kamu bukan admin grup ini!");
                    return;
                }
                
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
                    
                    _log.Debug("Simpan #{0} ({1}) ke {2}", hash.MessageId, hashtag, _message.ChatName());
                
                    await _db.InsertBookmark(hash);
                    await Bot.SendTextAsync(_message.Chat.Id,
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
                
                    _log.Debug("Ganti #{0} ({1}) dari {2}", query.MessageId, hashtag, _message.ChatName());
                    
                    await _db.InsertBookmark(query, true);
                    await Bot.SendTextAsync(_message.Chat.Id,
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
                await Bot.SendTextAsync(_message, $"Gagal menyimpan #{hashtag}.\n—— —— —— ——\nError : {e.Message}");
            }
        }

        public async Task Delete(string hashtag)
        {
            if (!_callbackMode)
            {
                hashtag = hashtag.TrimStart('#');
                if (string.IsNullOrWhiteSpace(hashtag)) return;

                // harus grup chat
                if (!await CheckingGroup()) return;

                // cek format hashtag
                if (!await CheckingHashtag(hashtag)) return;
            }
            
            // cek admin atau bukan
            if (!await _message.IsAdminThisGroup())
            {
                _log.Warning("User {0} bukan admin grup!", _message.FromName());
                await Bot.SendTextAsync(_message,
                    $"Maaf kakak,\n" +
                    $"Kamu bukan admin grup ini, jadi tidak bisa menghapus #{hashtag}.",
                    parse: ParseMode.Html);
                
                return;
            }

            var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
            if (query == null)
            {
                _log.Ignore("Tidak ada #{0} di {1}", hashtag, _message.ChatName());
                
                await Bot.SendTextAsync(_message, $"Tidak ada #{hashtag} di grup ini.");
                return;
            }

            try
            {
                _log.Debug("Hapus #{0} dari {1}", hashtag, _message.ChatName());
                
                await _db.DeleteBookmark(query);
                
                await Bot.SendTextAsync(_message,
                    $"{_message.FromNameWithMention(ParseMode.Html)} telah menghapus #{hashtag}!",
                    parse: ParseMode.Html);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(_message, $"Gagal hapus #{hashtag}!\nError : {e.Message}");
            }
        }

        public async void DeleteWithButton(string hashtag, bool final = false)
        {
            if (!_callbackMode) return;
            if (_message.ReplyToMessage.From.Id == _callback.From.Id)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
            }
            else
            {
                //Kamu tidak mempunyai hak untuk memencet tombol ini!
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Apaan sih pencet-pencet... Geli tauu!!", true);
                return;
            }

            if (!final)
            {
                var buttons = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("Yakin", $"cmd=remove-final&data={hashtag}"),
                    InlineKeyboardButton.WithCallbackData("Tidak", $"cmd=manage&data=null")
                });
                await Bot.EditOrSendTextAsync(_message, _message.MessageId, $"Apakah kamu yakin mau menghapus #{hashtag}?", button: buttons);
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
                if (!await CheckingGroup()) return;
            }
            else if (_callbackMode)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
            }

            var list = await _db.GetBookmarks(_message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", _message.ChatName());

                var respon = "Bookmark/Hashtag *kosong*.\nTapi kamu bisa panggil semua admin di grup ini dengan #admin atau #mimin.";
                await Bot.SendTextAsync(_message, respon, parse: ParseMode.Markdown);
                
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
            
                var sentMessage = await Bot.SendTextAsync(_message, respon, parse: ParseMode.Html);
                if (sentMessage == null) return;
                var schedule = new ScheduleData()
                {
                    ChatId = _message.Chat.Id,
                    MessageId = sentMessage.MessageId,
                    DateTime = DateTime.Now.AddMinutes(10),
                    Operation = ScheduleData.Type.Edit,
                    Text = "Bookmark kadaluarsa!\nGunakan /list untuk melihat daftar lagi."
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
                    await Bot.SendTextAsync(_message, respon, parse: ParseMode.Html, button: buttons);
                else
                    await Bot.EditOrSendTextAsync(_message, _message.MessageId, respon, ParseMode.Html, buttons);
            }
        }

        public async void ManageList()
        {
            if (!_callbackMode)
            {
                // harus grup chat
                if (!await CheckingGroup()) return;

                // cek admin atau bukan
                if (!await _message.IsAdminThisGroup())
                {
                    _log.Warning("User {0} bukan admin grup!", _message.FromName());
                    await Bot.SendTextAsync(_message,
                        $"Maaf kakak,\n" +
                        $"Kamu bukan admin grup ini, jadi tidak bisa menggunakan fitur ini.",
                        parse: ParseMode.Html);

                    return;
                }
            }
            else
            {
                if (_message.ReplyToMessage.From.Id == _callback.From.Id)
                {
                    await Bot.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
                }
                else
                {
                    //Kamu tidak mempunyai hak untuk memencet tombol ini!
                    await Bot.AnswerCallbackQueryAsync(_callback.Id, "Apaan sih pencet-pencet... Geli tauu!!", true);
                    return;
                }
            }

            var list = await _db.GetBookmarks(_message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", _message.ChatName());

                var norespon = "Bookmark/Hashtag *kosong*.";
                if (_callbackMode)
                    await Bot.EditOrSendTextAsync(_message, _message.MessageId, norespon, ParseMode.Markdown);
                else
                    await Bot.SendTextAsync(_message, norespon, parse: ParseMode.Markdown);
                
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
                sentMessage = await Bot.EditOrSendTextAsync(_message, _message.MessageId, respon, ParseMode.Html, buttons);
            else
                sentMessage = await Bot.SendTextAsync(_message, respon, true, ParseMode.Html, buttons);
            
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
                    
                    var admins = await Bot.GetChatAdministratorsAsync(_message);
                    admins = admins.OrderBy(x => x.User.FirstName).ToArray();
                    var respon = "Panggilan kepada :";
                    foreach (var x in admins)
                    {
                        var user = (x.User.FirstName + " " + x.User.LastName).Trim();
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Username;
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Id.ToString();
                        respon += $"\n• <a href=\"tg://user?id={x.User.Id}\">{user.Trim()}</a>";
                    }
                    
                    await Bot.SendTextAsync(_message, respon, parse: ParseMode.Html);
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
                    await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Tidak ada #{hashtag} di grup ini!", true);
                
                return;
            }
            
            _log.Debug("Panggil hashtag #{0}", hashtag);
            var sentMessage = await Bot.ForwardMessageAsync(_message.Chat.Id, query.ChatId, query.MessageId);

            if (!_callbackMode) return;

            if (sentMessage != null)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Hashtag #{hashtag} pesenan kaka udah tak siapin..", true);
                await Bot.SendTextAsync(_message,
                    $"Buat kaka {_message.FromNameWithMention(ParseMode.Html)},\n" +
                    $"itu {hashtag} udah tak siapin...", parse: ParseMode.Html);
            }
            else
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Hashtag #{hashtag} pesenan kaka gak bisa diforward 😔", true);
            }
        }

        public async void ShowInfo(string hashtag)
        {
            if (!_callbackMode) return;
            
            var query = await _db.GetBookmarkByHashtag(_message.Chat.Id, hashtag);
            if (query == null)
            {
                await Bot.AnswerCallbackQueryAsync(_callback.Id, $"Tidak ada #{hashtag} di grup ini!", true);
                
                return;
            }
            
            await Bot.AnswerCallbackQueryAsync(_callback.Id,
                $"Hashtag : {query.KeyName}\n" +
                $"MessageId : {query.MessageId}\n" +
                $"Set By : {query.ByName}\n" +
                $"DateTime : {query.DateTime}", true);
        }
    }
}