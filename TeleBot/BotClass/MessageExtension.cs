using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.BotClass
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
            if (!message.IsPrivateChat())
                name = chat.Title;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = chat.Id.ToString();

            return name;
        }

        public static string FromName(this Message message, bool fullname = false)
        {
            var from = message.From;
            var name = from.FirstName;
            if (string.IsNullOrWhiteSpace(name))
                name = from.LastName;
            if (fullname)
                name = (from.FirstName + " " + from.LastName).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = from.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = from.Id.ToString();

            return name;
        }

        public static string FromName(this User from, bool fullname = false)
        {
            var name = from.FirstName;
            if (string.IsNullOrWhiteSpace(name))
                name = from.LastName;
            if (fullname)
                name = (from.FirstName + " " + from.LastName).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = from.Username;
            if (string.IsNullOrWhiteSpace(name))
                name = from.Id.ToString();

            return name;
        }

        public static string FromNameWithMention(this Message message, ParseMode parse, bool fullname = false)
        {
            var id = message.From.Id;
            var name = message.FromName(fullname);
            switch (parse)
            {
                case ParseMode.Html:
                    return $"<a href=\"tg://user?id={id}\">{name}</a>";

                case ParseMode.Markdown:
                    return $"[{name}](tg://user?id={id})";

                case ParseMode.Default:
                    return name;

                default:
                    return name;
            }
        }

        public static async Task<bool> IsAdminThisGroup(this Message message)
        {
            if (!message.IsGroupChat()) return false;

            var admins = await BotClient.GetChatAdministratorsAsync(message.Chat.Id);
            foreach (var admin in admins)
                if (admin.User.Id == message.From.Id)
                    return true;

            return false;
        }

        public static string GetReplyResponse(this Message message)
        {
            if (message.IsFromOwner()) return BotResponse.ReplyToOwner();

            if (message.IsFromAdmins()) return BotResponse.ReplyToAdmins();

            return BotResponse.ReplyToOthers();
        }

        public static bool IsCallMe(this Message message)
        {
            // cari nama alias + username
            var alias = Bot.Keys.Alias + "|" + Bot.Username;
            return Regex.IsMatch(message.Text, $"\\b({alias})\\b", RegexOptions.IgnoreCase);
        }

        public static bool IsCallMeNotProper(this Message message)
        {
            // cari nama yg tdk seharusnya
            var aliasExcept = Bot.Keys.AliasExcept;
            return Regex.IsMatch(message.Text, $"\\b({aliasExcept})\\b", RegexOptions.IgnoreCase);
        }

        public static bool IsForwardMessage(this Message message)
        {
            return message.ForwardFrom != null;
        }

        public static bool IsFromOwner(this Message message)
        {
            return message.From.Id == Bot.Keys.OwnerId;
        }

        public static bool IsFromAdmins(this Message message)
        {
            return Bot.Keys.AdminIds.Contains(message.From.Id);
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
            return message.Type == MessageType.Text;
        }

        public static bool IsTextTooShort(this Message message)
        {
            // hapus karakter aneh
            var text = Regex.Replace(message.Text, @"\p{C}+", string.Empty);
            // hapus spasi
            text = Regex.Replace(text, @"\s{2,}", string.Empty);
            return string.IsNullOrWhiteSpace(text) || text.Length < 2;
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

        public static bool IsReplyToMessage(this Message message)
        {
            return message.ReplyToMessage != null;
        }
    }
}