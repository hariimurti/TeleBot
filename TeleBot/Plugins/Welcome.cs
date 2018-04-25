using System;
using System.Collections.Generic;
using TeleBot.BotClass;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.Plugins
{
    public class Welcome
    {
        private static Log _log = new Log("Welcome");
        private static Database _db = new Database();
        private Message _message;
        private CallbackQuery _callback;
        private bool _callbackMode;

        public Welcome(Message message, CallbackQuery callback = null)
        {
            _message = message;
            _callback = callback;
            if (callback != null)
            {
                _message.From = callback.From;
                _callbackMode = true;
            }
        }

        public async void SendGreeting()
        {
            var mention = string.Empty;
            foreach (var member in _message.NewChatMembers)
            {
                var usernameExist = !string.IsNullOrWhiteSpace(member.Username);
                if (usernameExist && member.Username.Equals(Bot.Username, StringComparison.OrdinalIgnoreCase))
                {
                    Command.Start(_message);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(mention)) mention += " ";

                var name = (member.FirstName + " " + member.LastName).Trim();
                if (string.IsNullOrWhiteSpace(name)) name = member.Username;
                if (string.IsNullOrWhiteSpace(name)) name = member.Id.ToString();

                mention += $"[{name}](tg://user?id={member.Id}),";
            }

            if (string.IsNullOrWhiteSpace(mention)) return;

            // cari kontak yg ada
            var exist = await _db.FindContact(_message.Chat.Id);
            if (!exist.Greeting) return;

            var welcome = BotResponse.WelcomeToGroup()
                .Replace("{member}", mention.TrimEnd(','))
                .Replace("{group}", _message.ChatName());

            await BotClient.SendTextAsync(_message, welcome, parse: ParseMode.Markdown);
        }

        public async void Manage(string data = null)
        {
            if (!_callbackMode)
            {
                if (!_message.IsGroupChat()) return;

                // cek admin atau bukan
                if (!await _message.IsAdminThisGroup())
                {
                    _log.Warning("User {0} bukan admin grup!", _message.FromName());
                    await BotClient.SendTextAsync(_message, $"Maaf kaka... Kamu bukan admin grup ini!");
                    return;
                }
            }
            else
            {
                if (_message.ReplyToMessage.From.Id == _callback.From.Id)
                {
                    await BotClient.AnswerCallbackQueryAsync(_callback.Id, "Tunggu sebentar...");
                }
                else
                {
                    //Kamu tidak mempunyai hak untuk memencet tombol ini!
                    await BotClient.AnswerCallbackQueryAsync(_callback.Id, BotResponse.NoAccessToButton(), true);
                    return;
                }
            }

            // cari kontak yg ada
            var existContact = await _db.FindContact(_message.Chat.Id);

            if (string.IsNullOrWhiteSpace(data))
            {
                var buttons = new InlineKeyboardMarkup(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Aktifkan", $"cmd=greeting&data=enable"),
                    InlineKeyboardButton.WithCallbackData("Nonaktifkan", $"cmd=greeting&data=disable")
                });
                await BotClient.SendTextAsync(_message,
                    $"Pengaturan ucapan selamat datang.\nStatus sekarang : " +
                    (existContact.Greeting ? "aktif." : "nonaktif."),
                    true, button: buttons);

                return;
            }

            var contact = new SQLite.Contact
            {
                Id = _message.Chat.Id,
                Name = _message.ChatName(),
                UserName = _message.Chat.Username,
                Private = _message.Chat.Type == ChatType.Private,
                Blocked = existContact.Blocked,
                Greeting = data.Equals("enable", StringComparison.OrdinalIgnoreCase)
            };

            var result = await _db.InsertOrReplaceContact(contact);
            if (!result) return;

            _log.Debug("Greeting {0} telah diperbaharui ({1})", _message.ChatName(), contact.Greeting);
            await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                $"Ucapan selamat datang telah " + (contact.Greeting ? "diaktifkan." : "dinonaktifkan."));
        }
    }
}