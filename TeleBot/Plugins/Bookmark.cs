using System;
using System.Collections.Generic;
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
    public static class Bookmark
    {
        private static Log _log = new Log("Bookmark");
        private static Database _db = new Database();

        private static async Task<bool> CheckingHashtag(Message message, string hashtag)
        {
            if (Regex.IsMatch(hashtag, @"^#?([\w]+)$")) return true;
            
            _log.Warning("Format hashtag \"{0}\" salah!", hashtag);
            await Bot.SendTextAsync(message, $"Hashtag \"{hashtag}\" pakai format ilegal!");
            return false;
        }
        
        private static async Task<bool> CheckingGroup(Message message)
        {
            if (message.IsGroupChat()) return true;
            
            _log.Warning("Khusus digunakan didalam grup!");
            await Bot.SendTextAsync(message, $"Perintah bookmark hanya bisa dipakai didalam grup!");
            return false;
        }

        public static async void Save(Message message, string hashtag)
        {
            try
            {
                hashtag = hashtag.TrimStart('#');
                if (string.IsNullOrWhiteSpace(hashtag)) return;
                
                // harus grup chat
                if (!await CheckingGroup(message)) return;

                // harus mereply pesan
                if (!message.IsReplyToMessage())
                {
                    _log.Warning("Tidak ada pesan yang akan disimpan!");
                    await Bot.SendTextAsync(message, $"Tidak ada pesan yg akan disimpan!");
                    return;
                }
                
                // cek format hashtag
                if (!await CheckingHashtag(message, hashtag)) return;
                
                // cek admin atau bukan
                if (!await message.IsAdminThisGroup())
                {
                    _log.Warning("User {0} bukan admin grup!", message.FromName());
                    await Bot.SendTextAsync(message, $"Maaf kaka... Kamu bukan admin grup ini!");
                    return;
                }
                
                var query = await _db.GetBookmarkByHashtag(message.Chat.Id, hashtag);
                if (query == null)
                {
                    var hash = new Hashtag()
                    {
                        ChatId = message.Chat.Id,
                        MessageId = message.ReplyToMessage.MessageId,
                        KeyName = hashtag
                    };
                    
                    _log.Debug("Simpan #{0} ({1}) ke {2}", hash.MessageId, hashtag, message.ChatName());
                
                    await _db.InsertBookmark(hash);
                    await Bot.SendTextAsync(message.Chat.Id,
                        $"<b>Simpan Bookmark!</b>\nMessageId : {hash.MessageId}\nHashtag : #{hashtag}\n—— —— —— ——\nHasil : Telah tersimpan.",
                        message.ReplyToMessage.MessageId,
                        ParseMode.Html);
                }
                else
                {
                    query.MessageId = message.ReplyToMessage.MessageId;
                    query.KeyName = hashtag;
                
                    _log.Debug("Ganti #{0} ({1}) dari {2}", query.MessageId, hashtag, message.ChatName());
                    
                    await _db.InsertBookmark(query, true);
                    await Bot.SendTextAsync(message.Chat.Id,
                        $"<b>Simpan Bookmark!</b>\nMessageId : {query.MessageId}\nHashtag : #{hashtag}\n—— —— —— ——\nHasil : Telah diganti.",
                        message.ReplyToMessage.MessageId,
                        ParseMode.Html);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(message, $"Gagal menyimpan #{hashtag}.\nError : {e.Message}");
            }
        }

        public static async void Delete(Message message, string hashtag, bool editList = false)
        {
            hashtag = hashtag.TrimStart('#');
            if (string.IsNullOrWhiteSpace(hashtag)) return;
            
            // harus grup chat
            if (!await CheckingGroup(message)) return;
                
            // cek format hashtag
            if (!await CheckingHashtag(message, hashtag)) return;
            
            // cek admin atau bukan
            if (!editList)
            {
                if (!await message.IsAdminThisGroup())
                {
                    _log.Warning("User {0} bukan admin grup!", message.FromName());
                    await Bot.SendTextAsync(message, $"Maaf kaka... Kamu bukan admin grup ini!");
                    return;
                }
            }
            
            var query = await _db.GetBookmarkByHashtag(message.Chat.Id, hashtag);
            if (query == null)
            {
                _log.Ignore("Tidak ada #{0} di {1}", hashtag, message.ChatName());
                
                await Bot.SendTextAsync(message, $"Tidak ada #{hashtag} di grup ini.");
                return;
            }

            try
            {
                _log.Debug("Hapus #{0} dari {1}", hashtag, message.ChatName());
                
                await _db.DeleteBookmark(query);
                
                await Bot.SendTextAsync(message,
                    $"Berhasil menghapus #{hashtag}!" +
                    (editList ? $"\nDihapus oleh : {message.FromName(true)}" : ""));
                
                if (editList) List(message, true, true);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(message,
                    $"Gagal hapus #{hashtag}!" +
                    (editList ? $"\nDihapus oleh : {message.FromName(true)}.\n" : "\n") +
                    $"Error : {e.Message}");
            }
        }

        public static async void List(Message message, bool useButton = false, bool editMessage = false)
        {
            // harus grup chat
            if (!await CheckingGroup(message)) return;
            
            var list = await _db.GetBookmarks(message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", message.ChatName());
                
                await Bot.SendTextAsync(message,
                    "Bookmark *kosong*.\nTapi kamu bisa panggil semua admin di grup ini dengan #admin atau #mimin.",
                    parse: ParseMode.Markdown);
                return;
            }

            if (!useButton)
            {
                var count = 1;
                var padding = list.Count.ToString().Length;
                var respon = $"Bookmark : <b>{message.ChatName()}</b>\nGenerated : {DateTime.Now}\nTotal : {list.Count}\n—— —— —— ——";
                foreach (var hashtag in list.OrderBy(h => h.KeyName))
                {
                    var num = count.ToString().PadLeft(padding, '0');
                    respon += $"\n{num}. #{hashtag.KeyName}";
                    count++;
                }
            
                var sentMessage = await Bot.SendTextAsync(message, respon, parse: ParseMode.Html);
                var schedule = new ScheduleData()
                {
                    ChatId = message.Chat.Id,
                    MessageId = sentMessage.MessageId,
                    DateTime = DateTime.Now.AddMinutes(10),
                    Operation = ScheduleData.Type.Edit,
                    Text = "Bookmark kadaluarsa!\nGunakan /list untuk melihat daftar lagi."
                };
                Schedule.RegisterNew(schedule);
            }
            else
            {
                var respon = $"Bookmark : <b>{message.ChatName()}</b>\nGenerated : {DateTime.Now}\nTotal : {list.Count}";
                var buttonRows = new List<List<InlineKeyboardButton>>();
                foreach (var hashtag in list.OrderBy(h => h.KeyName))
                {
                    var buttonColumns = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData(hashtag.KeyName, $"cmd=call&data=" + hashtag.KeyName),
                        InlineKeyboardButton.WithCallbackData("Hapus", $"cmd=remove&data=" + hashtag.KeyName)
                    };
                    buttonRows.Add(buttonColumns);
                }
                
                var buttons = new InlineKeyboardMarkup(buttonRows.ToArray());
                if (!editMessage)
                    await Bot.SendTextAsync(message, respon, parse: ParseMode.Html, button: buttons);
                else
                    await Bot.EditOrSendTextAsync(message, message.MessageId, respon, ParseMode.Html, buttons);
            }
        }

        public static async void GetAllFromText(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Text)) return;
            
            // harus grup chat
            if (!message.IsGroupChat()) return;
            
            _log.Debug("Cari hashtag dalam teks : {0}", message.Text);
            var matches = Regex.Matches(message.Text, @"#([\w\d]+)");
            foreach (Match match in matches)
            {
                var hashtag = match.Groups[1].Value;
                
                // panggil admin/mimin
                if (hashtag == "admin" || hashtag == "mimin")
                {
                    _log.Debug("Panggil admins grup!", hashtag);
                    
                    var admins = await Bot.GetChatAdministratorsAsync(message);
                    admins = admins.OrderBy(x => x.User.FirstName).ToArray();
                    var respon = "Panggilan kepada :";
                    foreach (var x in admins)
                    {
                        var user = (x.User.FirstName + " " + x.User.LastName).Trim();
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Username;
                        if (string.IsNullOrWhiteSpace(user)) user = x.User.Id.ToString();
                        respon += $"\n• <a href=\"tg://user?id={x.User.Id}\">{user.Trim()}</a>";
                    }
                    
                    await Bot.SendTextAsync(message, respon, parse: ParseMode.Html);
                    continue;
                }

                // cari hashtag
                GetHashtag(message, hashtag);
            }
        }

        public static async void GetHashtag(Message message, string hashtag)
        {
            var query = await _db.GetBookmarkByHashtag(message.Chat.Id, hashtag);
            if (query == null) return;
                
            _log.Debug("Panggil hashtag #{0}", hashtag);

            await Bot.ForwardMessageAsync(message.Chat.Id, query.ChatId, query.MessageId);
        }
    }
}