using System;
using TeleBot.BotClient;
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
            }
            // pesan dari grup
            else if (message.IsGroupChat())
            {
                // tdk panggil bot
                if (!message.IsCallMe())
                {
                    var lastIn = await _db.FindLastMessageIncoming(message.Chat.Id, true);
                    var lastOut = await _db.FindLastMessageOutgoing(message.Chat.Id);
                    
                    if (lastIn == null && lastOut == null) return;
                    
                    // bandingakan fromId dan messageId
                    if ((message.From.Id == lastIn.FromId) && (message.MessageId == lastOut.MessageId + 1))
                    {
                        if (message.Date > lastOut.DateTime.AddSeconds(20))
                            return;
                    }
                    return;
                }
                
                // panggilan tdk baik
                if (message.IsCallMeNotProper()) return;
            }

            // rubah nama bot jadi simsimi
            var text = message.Text.ReplaceBotNameWithSimsimi();
            
            // respon panggilan
            if (text == "simi")
            {
                var respon = message.GetReplyResponse().ReplaceWithBotValue();
                await Bot.SendTextAsync(message, respon);
                return;
            }
            
            // respon pesan mesum
            if (message.IsTextMesum())
            {
                var respon = message.GetBadWordResponse().ReplaceWithBotValue();
                await Bot.SendTextAsync(message, respon);
                return;
            }
            
            // respon pesan pendek
            if (message.IsTextTooShort())
            {
                _log.Ignore("{0} | Id: {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan terlalu pendek!",
                    message.Date.ToLocalTime(), message.MessageId, message.FromName(), message.Text);
                return;
            }
            
            // respon dgn simsimi
            new Simsimi(message).SendResponse(text);
        }
    }
}