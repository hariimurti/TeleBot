using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    class Translator
    {
        private static Log _log = new Log("Translator");
        private Message _message;

        public Translator(Message message)
        {
            _message = message;
        }

        public async void SendUsage()
        {
            await BotClient.SendTextAsync(_message,
                    $"*Translator*\n" +
                    $"—— Penggunaan ——\n" +
                    $"1. Balas pesan :\n" +
                    $"`/translate [code]`\n" +
                    $"\n2. Perintah langsung :\n" +
                    $"`/translate [code] teks asli`\n" +
                    $"\nCode :\n" +
                    $"`en` — terjemah ke inggris\n" +
                    $"`en-id` — terjemah dari inggris ke indonesia\n" +
                    $"\n—— Contoh ——\n" +
                    $"`/translate en teks asli`\n" +
                    $"`/translate id-en teks asli`",
                    parse: ParseMode.Markdown, preview: false);
        }

        public async void Translate(string data)
        {
            if (!_message.IsTextMessage() && !_message.IsReplyToMessage() && string.IsNullOrWhiteSpace(data))
            {
                SendUsage();
                return;
            }

            string direction = "id";
            string text;
            if (!_message.IsReplyToMessage())
            {
                var regex = Regex.Match(data, @"([a-z]{2}(?:-[a-z]{2})?) ([\S\s]+)", RegexOptions.IgnoreCase);
                if (!regex.Success)
                {
                    SendUsage();
                    return;
                }

                direction = regex.Groups[1].Value.ToLower();
                text = regex.Groups[2].Value;
            }
            else
            {
                text = _message.ReplyToMessage.Text;
                if (!string.IsNullOrWhiteSpace(data))
                {
                    var regex = Regex.Match(data, @"^([a-z]{2}(?:-[a-z]{2})?)$", RegexOptions.IgnoreCase);
                    if (!regex.Success)
                    {
                        SendUsage();
                        return;
                    }

                    direction = regex.Groups[1].Value.ToLower();
                }
            }

            _log.Debug("Direction: {0} | Text : {1}", direction, text);

            var yandex = new Yandex();
            var result = await yandex.Translate(direction, text);
            if (result.Success)
            {
                if (_message.IsReplyToMessage())
                    await BotClient.SendTextAsync(_message.Chat.Id, result.Text, _message.ReplyToMessage.MessageId);
                else
                    await BotClient.SendTextAsync(_message, result.Text);
            }
            else
            {
                await BotClient.SendTextAsync(_message, "Paijem bingung, gak bisa jawab");
            }
        }
    }
}
