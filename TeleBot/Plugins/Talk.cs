using System;
using System.Text.RegularExpressions;
using TeleBot.BotClass;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;

namespace TeleBot.Plugins
{
    public static class Talk
    {
        private static Log _log = new Log("Talk");
        private static Database _db = new Database();
        
        public static async void Response(Message message)
        {
            // pesan merupakan reply
            if (message.IsReplyToMessage())
            {
                var username = message.ReplyToMessage.From.Username;
                
                // username kosong
                if (string.IsNullOrWhiteSpace(username)) return;
                
                // username tdk sama dgn bot
                if (!string.Equals(username, Bot.Username, StringComparison.OrdinalIgnoreCase)) return;
                
                // mention user lain
                var mention = Regex.IsMatch(message.Text, "@(\\w+)\\b");
                if (mention)
                {
                    _log.Ignore("Pesan {0} | Alasan: mention user lain", message.MessageId);
                    return;
                }
            }
            // pesan dari grup
            else if (message.IsGroupChat())
            {
                if (Regex.IsMatch(message.Text, @"^(diam|shut ?up)!?$", RegexOptions.IgnoreCase))
                {
                    _log.Ignore("Pesan {0} | Alasan: disuruh diam...", message.MessageId);
                    return;
                }
                
                // tdk panggil bot
                if (!message.IsCallMe())
                {
                    var lastIn = await _db.FindLastMessageIncoming(message.Chat.Id, true);
                    var lastOut = await _db.FindLastMessageOutgoing(message.Chat.Id);
                    
                    // skip jika null semua
                    if (lastIn == null && lastOut == null) return;
                    
                    // bandingakan fromId dan messageId
                    if ((message.From.Id != lastIn?.FromId) || (message.MessageId != lastOut?.MessageId + 1)) return;
                    
                    // skip pesan lbh dari 20detik
                    if (message.Date > lastOut.DateTime.AddSeconds(20)) return;
                }
                
                // panggilan tdk baik
                if (message.IsCallMeNotProper()) return;
            }

            // rubah nama bot jadi simsimi
            var text = message.Text.ReplaceBotNameWithSimsimi();
            
            // respon panggilan
            if (text == "simi")
            {
                _log.Debug("Pesan {0} | Respon: kalimat template", message.MessageId);
                var respon = message.GetReplyResponse().ReplaceWithBotValue();
                await BotClient.SendTextAsync(message, respon);
                return;
            }
            
            // respon pesan mesum
            if (message.IsTextMesum())
            {
                _log.Debug("Pesan {0} | Alasan: terindikasi mesum!", message.MessageId);
                var respon = BotResponse.BadWord();
                await BotClient.SendTextAsync(message, respon);
                return;
            }
            
            // respon pesan pendek
            if (message.IsTextTooShort())
            {
                _log.Ignore("Pesan: {0} | Alasan: text terlalu pendek!", message.MessageId);
                return;
            }
            
            // respon dgn simsimi
            _log.Debug("Pesan {0} | Respon: tanya simsimi", message.MessageId);
            new Simsimi(message).SendResponse(text);
        }
    }
}