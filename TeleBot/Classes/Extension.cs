using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Classes
{
    public static class Extension
    {
        public static string SingleLine(this string text)
        {
            return text
                .Replace("\r\n", "\\r\\n")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        public static string ChatName(this Message message)
        {
            var chat = message.Chat;
            var name = (chat.FirstName + " " + chat.LastName).Trim();
            if (chat.Type != ChatType.Private)
                name = chat.Title;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Id.ToString();

            return name;
        }

        public static string FromName(this Message message)
        {
            var from = message.From;
            var name = (from.FirstName + " " + from.LastName).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = from.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = from.Id.ToString();

            return name;
        }
    }
}