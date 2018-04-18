using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClient
{
    public static class MessageExtension
    {
        public static string ChatName(this Message message)
        {
            var chat = message.Chat;
            var name = (chat.FirstName + " " + chat.LastName).Trim();
            if (message.IsPrivateChat())
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

        public static bool IsCallMe(this Message message)
        {
            var pattern = Bot.Keys.Alias;
            return Regex.IsMatch(message.Text, $"\\b({pattern})\\b", RegexOptions.IgnoreCase);
        }

        public static bool IsCallMeUgly(this Message message)
        {
            var pattern = Bot.Keys.AliasExcept;
            return Regex.IsMatch(message.Text, $"\\b({pattern})\\b", RegexOptions.IgnoreCase);
        }

        public static bool IsForwardMessage(this Message message)
        {
            return (message.ForwardFrom != null);
        }

        public static bool isFromOwner(this Message message)
        {
            return message.From.Id == Bot.Keys.OwnerId;
        }

        public static bool isFromAdmins(this Message message)
        {
            foreach (var admin in Bot.Keys.AdminIds)
            {
                if (message.From.Id == admin) return true;
            }
            return false;
        }

        public static bool IsGroupChat(this Message message)
        {
            return message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup;
        }

        public static bool isGroupTester(this Message message)
        {
            return message.Chat.Id == Bot.Keys.GroupId;
        }
        
        public static bool IsPrivateChat(this Message message)
        {
            return message.Chat.Type == ChatType.Private;
        }

        public static bool IsTextMessage(this Message message)
        {
            return (message.Type == MessageType.Text);
        }

        public static bool IsReplyMessage(this Message message)
        {
            return (message.ReplyToMessage != null);
        }
    }
}