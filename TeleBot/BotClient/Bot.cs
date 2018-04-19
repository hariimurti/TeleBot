﻿using System;
using System.Threading.Tasks;
using TeleBot.Classes;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.BotClient
{
    public static class Bot
    {
        private static Log _log = new Log("Bot");
        public static BotKeys Keys = Configs.LoadKeys();
        public static string Name = string.Empty;
        public static string Username = string.Empty;
        private static bool _isReceiving;
        
        private static TelegramBotClient _bot = Client();
        private static TelegramBotClient _botFile = Client(30);
        
        private static TelegramBotClient Client(int timeout = 0)
        {
            var client = new TelegramBotClient(Keys.Token);
            if (timeout > 0)
            {
                client.Timeout = TimeSpan.FromMinutes(timeout);
            }
            return client;
        }

        public static string ReplaceWithBotValue(this string text)
        {
            var alias = Keys.Alias.Replace("|", ", ");
            return text.Replace("{name}", Name)
                .Replace("{username}", Username)
                .Replace("{alias}", alias);
        }

        public static async void StartReceivingMessage()
        {
            if (_isReceiving) return;
            
            var me = await _bot.GetMeAsync();
            Name = me.FirstName;
            Username = me.Username;
            
            Console.Title = string.Format("{0} » {1} — @{2}", Program.AppName, Name, Username);
            _log.Info("Akun bot: {0} — @{1}", Name, Username);

            _bot.OnMessage += Messages.OnMessage;
            _bot.OnCallbackQuery += Messages.OnCallbackQuery;
            _bot.OnReceiveError += OnReceiveError;
            _bot.OnReceiveGeneralError += OnReceiveGeneralError;

            _log.Debug("Mulai menerima pesan...");
            _bot.StartReceiving();
            _isReceiving = true;
        }

        private static void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            _log.Error("OnReceiveGeneralError: " + e.Exception.Message);
        }

        private static void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            _log.Error("OnReceiveError: " + e.ApiRequestException.Message);
        }
        
        public static async Task<Message> SendTextAsync(Message msg, string text, bool reply = false, ParseMode parse = ParseMode.Default, IReplyMarkup button = null, bool preview = true)
        {
            var replyId = reply ? msg.MessageId : 0;
            return await SendTextAsync(msg.Chat.Id, text, replyId, parse, button, preview);
        }

        public static async Task<Message> SendTextAsync(ChatId chatId, string text, int replyId = 0, ParseMode parse = ParseMode.Default, IReplyMarkup button = null, bool preview = true)
        {
            try
            {
                await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
                await Task.Delay(500);
                
                _log.Debug("Kirim pesan ke {0}: {1}", chatId, text.SingleLine());
                return await _bot.SendTextMessageAsync(chatId, text, parse, !preview, replyToMessageId: replyId, replyMarkup: button);
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengirim pesan: {0}", ex.Message);
                return null;
            }
        }

        public static async Task<Message> EditOrSendTextAsync(Message msg, int messageId, string text, ParseMode parse = ParseMode.Default, InlineKeyboardMarkup keyboard = null, bool preview = true)
        {
            return await EditOrSendTextAsync(msg.Chat.Id, messageId, text, parse, keyboard, preview);
        }

        public static async Task<Message> EditOrSendTextAsync(ChatId chatId, int messageId, string text, ParseMode parse = ParseMode.Default, InlineKeyboardMarkup keyboard = null, bool preview = true)
        {
            try
            {
                _log.Debug("Edit pesan {0}: {1}", messageId, text.SingleLine());
                return await _bot.EditMessageTextAsync(chatId, messageId, text, parse, !preview, keyboard);
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengedit pesan: {0}", ex.Message);
                return await SendTextAsync(chatId, text, 0, parse, keyboard, preview);
            }
        }
    }
}