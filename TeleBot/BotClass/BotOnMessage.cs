﻿using System;
using TeleBot.Classes;
using TeleBot.Plugins;
using TeleBot.SQLite;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClass
{
    public static class BotOnMessage
    {
        private static Log _log = new Log("OnMessage");
        private static Database _db = new Database();
        public static int Count;

        public static async void Handle(object sender, MessageEventArgs e)
        {
            var message = e.Message;

            // pesan lebih dari 1 menit tdk akan direspon
            if (message.Date.AddMinutes(1) <= DateTime.Now.ToUniversalTime())
            {
                if (message.IsTextMessage())
                    _log.Ignore("Id: {0} | {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan lama!",
                        message.MessageId, message.Date.ToLocalTime(), message.FromName(), message.Text);

                return;
            }

            // member baru di grup: bot maupun user lain
            if (message.Type == MessageType.ChatMembersAdded)
            {
                _log.Info("Id: {0} | {1} | Dari: {2} | Pesan: Member baru!",
                    message.MessageId, message.Date.ToLocalTime(), message.ChatName());

                new Welcome(message).SendGreeting();
                return;
            }

            // abaikan pesan selain teks
            if (!message.IsTextMessage())
            {
                _log.Ignore("Id: {0} | {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan bukan text!",
                    message.MessageId, message.Date.ToLocalTime(), message.FromName(), message.Type);
                return;
            }

            // cari pesan yg sama
            var result = await _db.FindMessageIncoming(message.MessageId, message.Chat.Id);
            if (result != null)
            {
                _log.Ignore("Id: {0} | {1} | Dari: {2} | Pesan: {3} | Alasan: Pesan sudah dibaca!",
                    message.MessageId, message.Date.ToLocalTime(), message.FromName(), message.Text);
                return;
            }

            // tambah pesan masuk
            await _db.InsertMessageIncoming(message);
            Count++;

            _log.Info("Id: {0} | {1} | Dari: {2} | Pesan: {3}",
                message.MessageId, message.Date.ToLocalTime(), message.FromName(), message.Text);

            // pesan perintah
            if (message.Text.StartsWith("/"))
                Command.Execute(message);
            // pesan berisi hashtag
            else if (message.Text.Contains("#"))
                new Bookmark(message).FindHashtags();
            /* pesan teks lain
            else
                Talk.Response(message);*/
        }
    }
}