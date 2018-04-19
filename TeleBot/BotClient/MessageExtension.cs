using System;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClient
{
    public static class MessageExtension
    {
        public static string ChatName(this Message message, bool full = false)
        {
            var chat = message.Chat;
            var name = chat.FirstName;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.LastName;
            if (full)
                name = (chat.FirstName + " " + chat.LastName).Trim();
            if (message.IsPrivateChat())
                name = chat.Title;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Id.ToString();

            return name;
        }

        public static string FromName(this Message message, bool full = false)
        {
            var from = message.From;
            var name = from.FirstName;
            if (string.IsNullOrWhiteSpace(name))
                name = from.LastName;
            if (full)
                name = (from.FirstName + " " + from.LastName).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = from.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = from.Id.ToString();

            return name;
        }

        public static string GetBadWordResponse(this Message message)
        {
            var respon = Bot.Keys.BadWordResponse;
            var rand = new Random().Next(0, respon.Count - 1);
            return respon[rand];
        }

        public static string GetReplyResponse(this Message message)
        {
            var respon = Bot.Keys.ReplyOthers;
            
            if (message.IsFromOwner())
            {
                respon = Bot.Keys.ReplyOwner;
            }
            else if (message.IsFromAdmins())
            {
                respon = Bot.Keys.ReplyAdmins;
            }

            var rand = new Random().Next(0, respon.Count - 1);
            return respon[rand];
        }

        public static string MentionFromName(this Message message)
        {
            var name = string.Format("{0} {1}", message.From.FirstName, message.From.LastName).Trim();
            var username = message.From.Username;

            if (!string.IsNullOrWhiteSpace(name))
                return string.Format("[{0}](tg://user?id={1})", name, message.From.Id);
            else if (!string.IsNullOrWhiteSpace(username))
                return "@" + username;
            else
                return string.Format("[Mr/Ms. {0}](tg://user?id={0})", message.From.Id);
        }

        public static string MentionFromUsername(this Message message)
        {
            var username = message.From.Username;

            if (!string.IsNullOrWhiteSpace(username))
                return "@" + username;
            else
                return message.MentionFromName();
        }

        public static bool IsCallMe(this Message message)
        {
            // cari nama yg tdk seharusnya
            var aliasExcept = Bot.Keys.AliasExcept;
            var isMatch = Regex.IsMatch(message.Text, $"\\b({aliasExcept})\\b", RegexOptions.IgnoreCase);
            if (isMatch) return false;
            
            // cari nama alias
            var alias = Bot.Keys.Alias;
            return Regex.IsMatch(message.Text, $"\\b({alias})\\b", RegexOptions.IgnoreCase);
        }

        public static bool IsForwardMessage(this Message message)
        {
            return (message.ForwardFrom != null);
        }

        public static bool IsFromOwner(this Message message)
        {
            return message.From.Id == Bot.Keys.OwnerId;
        }

        public static bool IsFromAdmins(this Message message)
        {
            foreach (var admin in Bot.Keys.AdminIds)
            {
                if (message.From.Id == admin) return true;
            }
            return false;
        }

        public static bool IsGodMode(this Message message)
        {
            // cek apakah dari owner, admin atau grup tester
            return message.IsFromOwner() || message.IsFromAdmins() || message.IsGroupTester();
        }

        public static bool IsGroupChat(this Message message)
        {
            return message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup;
        }

        public static bool IsGroupTester(this Message message)
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

        public static bool IsTextTooShort(this Message message)
        {
            // hapus karakter aneh
            var text = Regex.Replace(message.Text, @"\p{C}+", string.Empty);
            // hapus spasi
            text = Regex.Replace(text, @"\s{2,}", string.Empty);
            return (string.IsNullOrWhiteSpace(text) || text.Length < 2);
        }

        public static bool IsTextMesum(this Message message)
        {
            // owner, admin dan grup tester boleh mesum
            if (message.IsGodMode()) return false;

            // menggunakan pattern badwords
            var pattern = Bot.Keys.BadWords;
            return Regex.IsMatch(message.Text,
                $"\\b(?:ber|di|ke?|nge?|ter)?({pattern})(?:i|k?an|ku|lah|mu|nya|pun|\\W)?\\b",
                RegexOptions.IgnoreCase);
        }

        public static bool IsReplyMessage(this Message message)
        {
            return (message.ReplyToMessage != null);
        }
    }
}