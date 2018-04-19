using TeleBot.Classes;
using TeleBot.Plugins;
using TeleBot.SQLite;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClient
{
    public static class Messages
    {
        private static Log _log = new Log("Messages");
        private static Database _db = new Database();
        
        public static async void OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            // member baru di grup: bot maupun user lain
            if (message.Type == MessageType.ChatMembersAdded)
            {
                Welcome.SendGreeting(message);
                return;
            }
            
            if (!message.IsTextMessage()) return;
            if (!message.IsPrivateChat()) return;
            
            // cek apakah sudah ada dieksekusi?
            var already = await _db.InsertMessageIncoming(message);
            if (already) return;
            
            _log.Info("{0}: Dari {1}: Pesan: {2}", message.Date.ToLocalTime(), message.FromName(), message.Text);

            if (message.Text.StartsWith("/"))
            {
                // pesan perintah
                Command.Execute(message);
            }
            else
            {
                // pesan teks lain
                var kirim = await Bot.SendTextAsync(message, $"{message.Text}, juga.");
                await _db.InsertMessageOutgoing(kirim);
            }
        }

        public static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //handle
        }
    }
}