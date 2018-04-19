using TeleBot.Classes;
using TeleBot.Plugins;
using TeleBot.SQLite;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClient
{
    public static class BotMessage
    {
        private static Log _log = new Log("Message");
        private static Database _db = new Database();
        
        public static async void OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            // member baru di grup: bot maupun user lain
            if (message.Type == MessageType.ChatMembersAdded)
            {
                _log.Info("{0} | Id: {1} | Dari: {2} | Pesan: Member baru!",
                    message.Date.ToLocalTime(), message.MessageId, message.ChatName());
                
                Welcome.SendGreeting(message);
                return;
            }
            
            if (!message.IsTextMessage()) return;
            if (!message.IsPrivateChat()) return;
            
            // cari pesan yg sama
            var result = await _db.FindMessageIncoming(message.MessageId, message.Chat.Id);
            if (result != null)
            {
                _log.Ignore("Pesan {0} dari {1} sudah dibaca!", message.MessageId, message.FromName());
                return;
            }
            
            // tambah pesan masuk
            await _db.InsertMessageIncoming(message);
            
            _log.Info("{0} | Id: {1} | Dari: {2} | Pesan: {3}",
                message.Date.ToLocalTime(), message.MessageId, message.FromName(), message.Text);

            // pesan perintah
            if (message.Text.StartsWith("/"))
            {
                Command.Execute(message);
            }
            // pesan teks lain
            else
            {
                var respon = message.GetReplyResponse().ReplaceWithBotValue();
                await Bot.SendTextAsync(message, respon);
            }
        }

        public static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //handle
        }
    }
}