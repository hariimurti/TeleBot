﻿using TeleBot.Classes;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClient
{
    public class Messages
    {
        private static Log _log = new Log("Messages");
        
        public static async void OnMessage(object sender, MessageEventArgs e)
        {
            var pesan = e.Message;
            if (pesan.Type != MessageType.Text) return;
            if (pesan.Chat.Type != ChatType.Private) return;
            _log.Ignore("{0}: Dari {1}: Pesan: {2}", pesan.Date.ToLocalTime(), pesan.From.FirstName, pesan.Text);
            await Bot.SendTextAsync(pesan, $"{pesan.Text}, juga.");
        }

        public static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //handle
        }
    }
}