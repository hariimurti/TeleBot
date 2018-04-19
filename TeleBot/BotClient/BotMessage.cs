using System;
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
            
            // pesan lebih dari 1 menit tdk akan direspon
            if (message.Date.AddMinutes(1) <= DateTime.Now.ToUniversalTime())
            {
                if (message.IsTextMessage())
                {
                    _log.Ignore("{0} | Id: {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan lama!",
                        message.Date.ToLocalTime(), message.MessageId, message.FromName(), message.Text);
                }
                
                return;
            }

            // member baru di grup: bot maupun user lain
            if (message.Type == MessageType.ChatMembersAdded)
            {
                _log.Info("{0} | Id: {1} | Dari: {2} | Pesan: Member baru!",
                    message.Date.ToLocalTime(), message.MessageId, message.ChatName());
                
                Welcome.SendGreeting(message);
                return;
            }
            
            // abaikan pesan selain teks
            if (!message.IsTextMessage())
            {
                _log.Ignore("{0} | Id: {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan bukan text!",
                    message.Date.ToLocalTime(), message.MessageId, message.FromName(), message.Type);
                return;
            }
            
            // cari pesan yg sama
            var result = await _db.FindMessageIncoming(message.MessageId, message.Chat.Id);
            if (result != null)
            {
                _log.Ignore("{0} | Id: {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan sudah dibaca!",
                    message.Date.ToLocalTime(), message.MessageId, message.FromName(), message.Text);
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
                Talk.Response(message);
            }
        }

        public static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //handle
        }
    }
}