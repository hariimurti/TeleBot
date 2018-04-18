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
            
            var client = Client();
            var me = await client.GetMeAsync();
            Console.Title = string.Format("{0} » {1} — @{2}", Program.AppName, me.FirstName, me.Username);
            _log.Info("Akun bot: {0} — @{1}", me.FirstName, me.Username);

            client.OnMessage += OnMessage;
            client.OnCallbackQuery += OnCallbackQuery;
            client.OnReceiveError += OnReceiveError;
            client.OnReceiveGeneralError += OnReceiveGeneralError;

            _log.Debug("Mulai menerima pesan...");
            client.StartReceiving();
            _isReceiving = true;
        }
        
        private static void OnMessage(object sender, MessageEventArgs e)
        {
            //handle
        }

        private static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //handle
        }

        private static void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            _log.Error("OnReceiveGeneralError: " + e.Exception.Message);
        }

        private static void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            _log.Error("OnReceiveError: " + e.ApiRequestException.Message);
        }
        
        public static async Task<Message> SendTextAsync(Message msg, string text, bool reply = false, bool parse = false, IReplyMarkup button = null, bool preview = true)
        {
            int replyId = reply ? msg.MessageId : 0;
            return await SendTextAsync(msg.Chat.Id, text, replyId, parse, button, preview);
        }

        public static async Task<Message> SendTextAsync(ChatId chatId, string text, int replyId = 0, bool parse = false, IReplyMarkup button = null, bool preview = true)
        {
            try
            {
                _log.Debug("Kirim pesan ke {0}: {1}", chatId, text.SingleLine());
                
                var parseMode = parse ? ParseMode.Html : ParseMode.Default;
                var client = Client();
                await client.SendChatActionAsync(chatId, ChatAction.Typing);
                await Task.Delay(500);
                return await client.SendTextMessageAsync(chatId, text, parseMode, !preview, replyToMessageId: replyId, replyMarkup: button);
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
                _log.Debug("Edit pesan {0}: {1}", messageId, text.SingleLine());
                
                var parseMode = parse ? ParseMode.Html : ParseMode.Default;
                return await Client().EditMessageTextAsync(chatId, messageId, text, parseMode, !preview, keyboard);
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengedit pesan: {0}", ex.Message);
                
                return await SendTextAsync(chatId, text, 0, parse, keyboard, preview);
            }
        }
    }
}