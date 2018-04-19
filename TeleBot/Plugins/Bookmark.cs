using System;
using System.Linq;
using System.Text.RegularExpressions;
using TeleBot.BotClient;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public class Bookmark
    {
        private static Log _log = new Log("Bookmark");
        private static Database _db = new Database();

        public static async void Save(Message message, string hashtag)
        {
            try
            {
                hashtag = hashtag.TrimStart('#');
                if (string.IsNullOrWhiteSpace(hashtag)) return;
                if (!Regex.IsMatch(hashtag, @"^#?([\w]+)$"))
                {
                    _log.Warning("Format hashtag \"{0}\" salah!", hashtag);
                    await Bot.SendTextAsync(message, $"Hashtag \"{hashtag}\" pakai format ilegal!");
                    return;
                }
                
                var list = await _db.GetBookmarks(message.Chat.Id, hashtag);
                if (list.Count == 0)
                {
                    var hash = new Hashtag()
                    {
                        ChatId = message.Chat.Id,
                        MessageId = message.ReplyToMessage.MessageId,
                        KeyName = hashtag
                    };
                    
                    _log.Debug("Simpan #{0} ({1}) ke {2}", hash.MessageId, hashtag, message.ChatName());
                
                    await _db.InsertBookmark(hash);
                    await Bot.SendTextAsync(message,
                        $"*Bookmark* : {hash.MessageId}\n*Hashtag* : #{hashtag}\n—— —— —— ——\n*Hasil* : Telah tersimpan!",
                        parse: ParseMode.Markdown);
                }
                else
                {
                    var hash = list.FirstOrDefault();
                    hash.MessageId = message.ReplyToMessage.MessageId;
                    hash.KeyName = hashtag;
                
                    _log.Debug("Ganti #{0} ({1}) dari {2}", hash.MessageId, hashtag, message.ChatName());
                    
                    await _db.InsertBookmark(hash, true);
                    await Bot.SendTextAsync(message,
                        $"*Bookmark* : {hash.MessageId}\n*Hashtag* : #{hashtag}\n—— —— —— ——\n*Hasil* : Telah diganti!",
                        parse: ParseMode.Markdown);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(message, $"Gagal menyimpan #{hashtag}.\nError : {e.Message}");
            }
        }

        public static async void Delete(Message message, string hashtag)
        {
            hashtag = hashtag.TrimStart('#');
            if (string.IsNullOrWhiteSpace(hashtag)) return;
            
            var list = await _db.GetBookmarks(message.Chat.Id, hashtag);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada #{0} di {1}", hashtag, message.ChatName());
                
                await Bot.SendTextAsync(message, $"Tidak ada #{hashtag} di grup ini.");
                return;
            }

            try
            {
                _log.Debug("Hapus #{0} dari {1}", hashtag, message.ChatName());
                
                var hash = list.FirstOrDefault();
                await _db.DeleteBookmark(hash);
                
                await Bot.SendTextAsync(message, $"Berhasil menghapus #{hashtag}");
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await Bot.SendTextAsync(message, $"Gagal hapus #{hashtag}.\nError : {e.Message}");
            }
        }

        public static async void List(Message message)
        {
            var list = await _db.GetBookmarks(message.Chat.Id);
            if (list.Count == 0)
            {
                _log.Ignore("Tidak ada list di {0}", message.ChatName());
                
                await Bot.SendTextAsync(message,
                    "Bookmark *kosong*.\nTapi kamu bisa panggil semua admin di grup ini dengan #admin atau #mimin.",
                    parse: ParseMode.Markdown);
                return;
            }

            var count = 1;
            var mod = list.Count % 10;
            var respon = "Bookmark grup ini :\n—— —— —— ——";
            foreach (var hashtag in list.OrderBy(h => h.KeyName))
            {
                var num = count.ToString().PadLeft(mod, '0');
                respon += $"\n{num}. #{hashtag.KeyName}";
                count++;
            }
            
            await Bot.SendTextAsync(message, respon);
        }
    }
}