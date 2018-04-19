using System;
using System.Threading.Tasks;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.BotClient
{
    public static class Bot
    {
        private static Log _log = new Log("Bot");
        private static Database _db = new Database();
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

        public static async Task StartReceivingMessage()
        {
            if (_isReceiving) return;
            
            var me = await _bot.GetMeAsync();
            Name = me.FirstName;
            Username = me.Username;
            
            Console.Title = string.Format("{0} » {1} — @{2}", Program.AppName, Name, Username);
            _log.Info("Akun bot: {0} — @{1}", Name, Username);

            _bot.OnMessage += BotMessage.OnMessage;
            _bot.OnCallbackQuery += BotMessage.OnCallbackQuery;
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

        public static async Task<bool> DeleteMessageAsync(Message msg)
        {
            return await DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
        }

        public static async Task<bool> DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try
            {
                _log.Debug("Hapus pesan {0} dari {1}", messageId, chatId);
                await _bot.DeleteMessageAsync(chatId, messageId);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa menghapus pesan: {0}", ex.Message);
                return false;
            }
        }

        public static async Task<ChatMember[]> GetChatAdministratorsAsync(Message message)
        {
            return await GetChatAdministratorsAsync(message.Chat.Id);
        }

        public static async Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId)
        {
            try
            {
                _log.Debug("Chat admins dari {0}", chatId);
                return await _bot.GetChatAdministratorsAsync(chatId);
            }
            catch (Exception ex)
            {
                _log.Error("Gagal mendapatkan chat admins: {0}", ex.Message);
                return null;
            }
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
                var message = await _bot.SendTextMessageAsync(chatId, text, parse, !preview, replyToMessageId: replyId, replyMarkup: button);
                
                await _db.InsertMessageOutgoing(message);
                return message;
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
                var message = await _bot.EditMessageTextAsync(chatId, messageId, text, parse, !preview, keyboard);
                
                await _db.InsertMessageOutgoing(message);
                return message;
            }
            catch (Exception ex)
            {
                _log.Error("Tidak bisa mengedit pesan: {0}", ex.Message);
                return await SendTextAsync(chatId, text, 0, parse, keyboard, preview);
            }
        }

        public static async Task<Message> SendPhotoAsync(Message msg, InputOnlineFile file, string caption = null, ParseMode parse = ParseMode.Default, bool reply = false)
        {
            var replyId = reply ? msg.MessageId : 0;
            return await SendPhotoAsync(msg.Chat.Id, file, caption, parse, replyId);
        }

        public static async Task<Message> SendPhotoAsync(ChatId chatId, InputOnlineFile file, string caption = null, ParseMode parse = ParseMode.Default, int replyId = 0)
        {
            try
            {
                Message message;
                await _bot.SendChatActionAsync(chatId, ChatAction.UploadPhoto);

                _log.Debug("Kirim photo ke {0}", chatId);
                
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
                _log.Error("Tidak bisa kirim photo: {0}", ex.Message);
                return null;
            }
        }
    }
}