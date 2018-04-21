using System;
using System.Text.RegularExpressions;
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
            // pesan berisi hashtag
            else if (message.Text.Contains("#"))
            {
                new Bookmark(message).FindHashtags();
            }
            // pesan teks lain
            else
            {
                Talk.Response(message);
            }
        }

        public static async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var callback = e.CallbackQuery;
            var message = e.CallbackQuery.Message;
            
            var query = Regex.Match(callback.Data, @"cmd=(\S+)&data=(\S+)");
            if (!query.Success)
            {
                _log.Error("Unknown: {0}", callback.Data);
                await Bot.AnswerCallbackQueryAsync(callback.Id, "Perintah tidak diketahui", true);
                return;
            }
            
            var cmd = query.Groups[1].Value;
            var data = query.Groups[2].Value;

            switch (cmd)
            {
                case "call":
                    new Bookmark(message, callback).FindHashtag(data);
                    break;
                
                case "remove":
                    new Bookmark(message, callback).Delete(data, true);
                    break;
                
                case "mobilism":
                    new Mobilism(message, callback).OpenThread(data);
                    break;
                
                default:
                    _log.Ignore("Unknown: {0}", callback.Data);
                    break;
            }
        }
    }
}