using System;
using System.Threading.Tasks;
using TeleBot.Classes;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.BotClient
{
    public class Bot
    {
        private static Log _log = new Log("Bot");
        private static TelegramBotClient _bot = Client();
        private static TelegramBotClient _botFile = Client(30);
        private static bool _isReceiving;
        
        private static TelegramBotClient Client(int timeout = 0)
        {
            var client = new TelegramBotClient(Program.BotToken);
            if (timeout > 0)
            {
                client.Timeout = TimeSpan.FromMinutes(timeout);
            }
            return client;
        }

        public static async void StartReceivingMessage()
        {
            if (_isReceiving) return;
            
            var me = await _bot.GetMeAsync();
            Console.Title = string.Format("{0} » {1} — @{2}", Program.AppName, me.FirstName, me.Username);
            _log.Info("Akun bot: {0} — @{1}", me.FirstName, me.Username);

            _bot.OnMessage += Messages.OnMessage;
            _bot.OnCallbackQuery += Messages.OnCallbackQuery;
            _bot.OnReceiveError += OnReceiveError;
            _bot.OnReceiveGeneralError += OnReceiveGeneralError;

            _log.Debug("Mulai menerima pesan...");
            _bot.StartReceiving();
            _isReceiving = true;
        }

        public static void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            _log.Error("OnReceiveGeneralError: " + e.Exception.Message);
        }

        public static void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            _log.Error("OnReceiveError: " + e.ApiRequestException.Message);
        }
        
        public static async Task<Message> SendTextAsync(Message msg, string text, bool reply = false, bool parse = false, IReplyMarkup button = null, bool preview = true)
        {
            var replyId = reply ? msg.MessageId : 0;
            return await SendTextAsync(msg.Chat.Id, text, replyId, parse, button, preview);
        }

        public static async Task<Message> SendTextAsync(ChatId chatId, string text, int replyId = 0, bool parse = false, IReplyMarkup button = null, bool preview = true)
        {
            try
            {
                var parseMode = parse ? ParseMode.Html : ParseMode.Default;
                
                await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
                await Task.Delay(500);
                
                _log.Debug("Kirim pesan ke {0}: {1}", chatId, text.SingleLine());
                return await _bot.SendTextMessageAsync(chatId, text, parseMode, !preview, replyToMessageId: replyId, replyMarkup: button);
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengirim pesan: {0}", ex.Message);
                return null;
            }
        }

        public static async Task<Message> EditOrSendTextAsync(Message msg, int messageId, string text, bool parse = false, InlineKeyboardMarkup keyboard = null, bool preview = true)
        {
            return await EditOrSendTextAsync(msg.Chat.Id, messageId, text, parse, keyboard, preview);
        }

        public static async Task<Message> EditOrSendTextAsync(ChatId chatId, int messageId, string text, bool parse = false, InlineKeyboardMarkup keyboard = null, bool preview = true)
        {
            try
            {
                var parseMode = parse ? ParseMode.Html : ParseMode.Default;
                
                _log.Debug("Edit pesan {0}: {1}", messageId, text.SingleLine());
                return await _bot.EditMessageTextAsync(chatId, messageId, text, parseMode, !preview, keyboard);
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengedit pesan: {0}", ex.Message);
                return await SendTextAsync(chatId, text, 0, parse, keyboard, preview);
            }
        }
    }
}