using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.BotClass
{
    public static class BotClient
    {
        private static Log _log = new Log("Bot");
        private static Database _db = new Database();

        private static TelegramBotClient _bot = Client();
        private static TelegramBotClient _botFile = Client(30);
        private static bool _isReceiving;

        private static TelegramBotClient Client(int timeout = 0)
        {
            try
            {
                var client = new TelegramBotClient(Bot.Keys.Token);
                if (timeout > 0) client.Timeout = TimeSpan.FromMinutes(timeout);
                return client;
            }
            catch (Exception e)
            {
                if (e.Message.ToLower().Contains("token"))
                {
                    _log.Error("Silahkan isi Token dgn benar! Konfigurasi ada di file Bot.json");
                    Program.Terminate(1);
                }
                else
                {
                    _log.Error(e.Message);
                }

                return null;
            }
        }

        #region StartReceivingMessage

        public static async Task<bool> StartReceivingMessage()
        {
            if (_isReceiving) return true;

            // mendapatkan info bot
            var me = await _bot.GetMeAsync();

            // baca nama depan
            var readFirstName = Regex.Match(me.FirstName, @"^(\w+)\b");
            Bot.Name = readFirstName.Success ? readFirstName.Groups[1].Value : me.FirstName;
            Bot.Username = me.Username;

            Console.Title = string.Format("{0} » {1} — @{2}", Program.AppName, Bot.Name, Bot.Username);
            _log.Info("Akun bot: {0} — @{1}", Bot.Name, Bot.Username);

            _bot.OnMessage += BotOnMessage.Handle;
            _bot.OnCallbackQuery += BotOnCallbackQuery.Handle;
            _bot.OnReceiveError += OnReceiveError;
            _bot.OnReceiveGeneralError += OnReceiveGeneralError;

            _log.Debug("Mulai menerima pesan...");
            _bot.StartReceiving();
            _isReceiving = true;
            return true;
        }

        private static void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            _log.Error("OnReceiveGeneralError: " + e.Exception.Message);
        }

        private static void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            _log.Error("OnReceiveError: " + e.ApiRequestException.Message);
        }

        #endregion

        #region AnswerCallbackQueryAsync

        public static async Task AnswerCallbackQueryAsync(string queryId, string text, bool showAlert = false)
        {
            try
            {
                _log.Debug("AnswerCallbackQueryAsync: {0} » {1}", queryId, text);
                await _bot.AnswerCallbackQueryAsync(queryId, text, showAlert);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        #endregion

        #region DeleteMessageAsync

        public static async Task<bool> DeleteMessageAsync(Message msg)
        {
            return await DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
        }

        public static async Task<bool> DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try
            {
                _log.Debug("DeleteMessageAsync: {0} » Dari: {1}", messageId, chatId);
                await _bot.DeleteMessageAsync(chatId, messageId);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
        }

        #endregion

        #region GetChatAdministratorsAsync

        public static async Task<ChatMember[]> GetChatAdministratorsAsync(Message message)
        {
            return await GetChatAdministratorsAsync(message.Chat.Id);
        }

        public static async Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId)
        {
            try
            {
                _log.Debug("GetChatAdministratorsAsync: {0}", chatId);
                return await _bot.GetChatAdministratorsAsync(chatId);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return null;
            }
        }

        #endregion

        #region ForwardMessageAsync

        public static async Task<Message> ForwardMessageAsync(Message message)
        {
            return await ForwardMessageAsync(message.Chat.Id, message.Chat.Id, message.MessageId);
        }

        public static async Task<Message> ForwardMessageAsync(ChatId toChatId, ChatId fromChatId, int messageId)
        {
            try
            {
                _log.Debug("ForwardMessageAsync: {0} » {1}", messageId, toChatId);
                return await _bot.ForwardMessageAsync(toChatId, fromChatId, messageId);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return null;
            }
        }

        #endregion

        #region SendTextAsync

        public static async Task<Message> SendTextAsync(Message msg, string text, bool reply = false,
            ParseMode parse = ParseMode.Default, IReplyMarkup button = null, bool preview = true)
        {
            var replyId = reply ? msg.MessageId : 0;
            return await SendTextAsync(msg.Chat.Id, text, replyId, parse, button, preview);
        }

        public static async Task<Message> SendTextAsync(ChatId chatId, string text, int replyId = 0,
            ParseMode parse = ParseMode.Default, IReplyMarkup button = null, bool preview = true)
        {
            try
            {
                await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
                await Task.Delay(500);

                _log.Debug("SendTextAsync: {0} » {1}", chatId, text.ToSingleLine());
                var message = await _bot.SendTextMessageAsync(chatId, text, parse, !preview, replyToMessageId: replyId,
                    replyMarkup: button);

                await _db.InsertMessageOutgoing(message);
                return message;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return null;
            }
        }

        #endregion

        #region EditOrSendTextAsync

        public static async Task<Message> EditOrSendTextAsync(Message msg, int messageId, string text,
            ParseMode parse = ParseMode.Default, InlineKeyboardMarkup button = null, bool preview = true,
            bool sendOnError = true)
        {
            return await EditOrSendTextAsync(msg.Chat.Id, messageId, text, parse, button, preview, sendOnError);
        }

        public static async Task<Message> EditOrSendTextAsync(ChatId chatId, int messageId, string text,
            ParseMode parse = ParseMode.Default, InlineKeyboardMarkup button = null, bool preview = true,
            bool sendOnError = true)
        {
            try
            {
                _log.Debug("EditOrSendTextAsync: {0} » {1}", messageId, text.ToSingleLine());
                var message = await _bot.EditMessageTextAsync(chatId, messageId, text, parse, !preview, button);

                await _db.InsertMessageOutgoing(message);
                return message;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);

                if (sendOnError)
                    return await SendTextAsync(chatId, text, 0, parse, button, preview);
                return null;
            }
        }

        #endregion

        #region SendPhotoAsync

        public static async Task<Message> SendPhotoAsync(Message msg, InputOnlineFile file, string caption = null,
            ParseMode parse = ParseMode.Default, bool reply = false)
        {
            var replyId = reply ? msg.MessageId : 0;
            return await SendPhotoAsync(msg.Chat.Id, file, caption, parse, replyId);
        }

        public static async Task<Message> SendPhotoAsync(ChatId chatId, InputOnlineFile file, string caption = null,
            ParseMode parse = ParseMode.Default, int replyId = 0)
        {
            try
            {
                Message message;
                await _bot.SendChatActionAsync(chatId, ChatAction.UploadPhoto);

                _log.Debug("SendPhotoAsync: {0}", chatId);

                if (caption?.Length > 200)
                {
                    message = await _botFile.SendPhotoAsync(chatId, file, replyToMessageId: replyId);
                    await _bot.SendTextMessageAsync(chatId, caption, parse, true, replyToMessageId: message.MessageId);
                }
                else
                {
                    message = await _botFile.SendPhotoAsync(chatId, file, caption, parse, replyToMessageId: replyId);
                }

                await _db.InsertMessageOutgoing(message);
                return message;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return null;
            }
        }

        #endregion
    }
}