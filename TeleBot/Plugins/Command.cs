using System;
using System.Linq;
using System.Text.RegularExpressions;
using TeleBot.BotClass;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public static class Command
    {
        private static Log _log = new Log("Command");
        private static Database _db = new Database();

        public static void Execute(Message message)
        {
            // pemisahan command, mention & data
            var regexCmd = Regex.Match(message.Text, @"^\/(\w+)(?:@(\w+))?\b ?(.+)?", RegexOptions.IgnoreCase);
            if (!regexCmd.Success)
            {
                _log.Warning("Regex tdk dapat memisahkan command!");
                return;
            }

            var cmd = regexCmd.Groups[1].Value;
            var mention = regexCmd.Groups[2].Value;
            var data = regexCmd.Groups[3].Value;

            if (!string.IsNullOrWhiteSpace(mention) &&
                !string.Equals(mention, Bot.Username, StringComparison.OrdinalIgnoreCase))
            {
                _log.Ignore("Username \"{0}\" (mention) tdk sama dgn \"{1}\" (bot)", mention, Bot.Username);
                return;
            }

            switch (cmd.ToLower())
            {
                case "bantuan":
                case "help":
                    Help(message);
                    break;

                case "id":
                    ShowId(message);
                    break;

                case "start":
                    Start(message);
                    break;

                case "status":
                    Status(message);
                    break;

                case "token":
                    Token(message, data);
                    break;

                case "selamatdatang":
                case "welcome":
                    new Welcome(message).Manage();
                    break;

                case "save":
                case "simpan":
                    new Bookmark(message).Save(data);
                    break;

                case "delete":
                case "hapus":
                    new Bookmark(message).Delete(data).GetAwaiter();
                    break;

                case "hashtag":
                    new Bookmark(message).GenerateList(false);
                    break;

                case "bookmark":
                    new Bookmark(message).GenerateList(true);
                    break;

                case "manage":
                case "kelola":
                    new Bookmark(message).ManageList();
                    break;

                case "app":
                case "aplikasi":
                    new Mobilism(message).ThreadList(data);
                    break;

                case "game":
                case "permainan":
                    new Mobilism(message).ThreadList(data, false);
                    break;

                case "cari":
                case "g":
                case "google":
                case "search":
                    new Qwant(message).SearchWeb(data);
                    break;

                case "img":
                case "image":
                case "photo":
                    new Qwant(message).SearchImage(data);
                    break;

                case "gapps":
                case "opengapps":
                    new OpenGapps(message).SelectArch();
                    break;

                default:
                    _log.Ignore("Perintah: {0} -- tdk ada", cmd);
                    break;
            }
        }

        private static async void Help(Message message)
        {
            var help = "*Panduan penggunaan* :\n" +
                       "—— —— —— —— ——\n" +
                       "Info Grup/User\n" +
                       "• `/id` — menampilkan info.\n" +
                       "\n" +
                       "Mobilism (Android)\n" +
                       "• `/app` — 10 app terbaru.\n" +
                       "• `/app query` — cari app.\n" +
                       "• `/game` — 10 game terbaru.\n" +
                       "• `/game query` — cari game.\n" +
                       "• _alias: aplikasi, permainan._\n" +
                       "\n" +
                       "Bookmark/Hashtag (Grup)\n" +
                       "• Rules: reply pesan & hanya admin.\n" +
                       "• `/simpan nama` — simpan nama.\n" +
                       "• `/hapus nama` — hapus nama.\n" +
                       "• `/kelola` — info & hapus.\n" +
                       "• _alias: save, delete, manage._\n" +
                       "\n" +
                       "Qwant (Pencarian)\n" +
                       "• `/cari query` — cari website.\n" +
                       "• `/image query` — cari image.\n" +
                       "• _alias: g, search, img, photo._\n" +
                       "\n" +
                       "OpenGapps\n" +
                       "• `/gapps` — link dl gapps.\n" +
                       "• _alias: opengapps._\n" +
                       "\n" +
                       "Welcome/Selamat Datang (Grup)\n" +
                       "• `/welcome` — pengaturan.\n" +
                       "• _alias: selamatdatang._\n" +
                       "\n" +
                       "Teman obrolan\n" +
                       "• PM: chat teks spt biasa.\n" +
                       "• Grup: reply pesan atau mention {alias}.\n";

            await BotClient.SendTextAsync(message, help.ReplaceWithBotValue(), parse: ParseMode.Markdown);
        }

        private static async void ShowId(Message message)
        {
            var respon = "";
            if (message.IsGroupChat())
            {
                var chat = message.Chat;
                respon += $"👥 <b>Group Info</b> 👥\n" +
                          $"ID : <code>{chat.Id}</code>\n" +
                          $"Name : {message.ChatName()}\n";
                if (!string.IsNullOrWhiteSpace(chat.Username))
                    respon += $"Username : {chat.Username}\n";
                if (!string.IsNullOrWhiteSpace(chat.InviteLink))
                    respon += $"Invite Link : {chat.InviteLink}\n";
                if (!string.IsNullOrWhiteSpace(chat.Description))
                    respon += $"Description : {chat.Description}\n";

                respon += "\n";
            }

            if (message.IsReplyToMessage())
            {
                var from = message.ReplyToMessage.From;
                respon += $"👤 <b>User Info</b> 👤\n" +
                          $"ID : <code>{from.Id}</code>\n" +
                          $"Name : {from.FromName(true)}";
                if (!string.IsNullOrWhiteSpace(from.Username))
                    respon += $"\nUsername : @{from.Username}";
            }
            else
            {
                var from = message.From;
                respon += $"👤 <b>User Info</b> 👤\n" +
                          $"ID : <code>{from.Id}</code>\n" +
                          $"Name : {from.FromName(true)}";
                if (!string.IsNullOrWhiteSpace(from.Username))
                    respon += $"\nUsername : @{from.Username}";
            }

            await BotClient.SendTextAsync(message, respon, parse: ParseMode.Html);
        }

        public static async void Start(Message message)
        {
            var respon = BotResponse.SayHello();
            await BotClient.SendTextAsync(message, respon, parse: ParseMode.Markdown);
        }

        private static async void Status(Message message)
        {
            var runtime = DateTime.Now - Program.StartTime;
            var tfs = TimeSpan.FromSeconds(runtime.TotalSeconds);
            var timespan = new TimeSpan(tfs.Days, tfs.Hours, tfs.Minutes, tfs.Seconds);

            var respon =
                $"*{Program.AppName} v2.0*\n" +
                $"—— —— —— ——\n" +
                $"SubVersion : {Program.AppVersion}\n" +
                $"UpTime : {timespan}\n" +
                $"Messages : {BotMessage.Count}\n";

            // cek token
            try
            {
                var tokens = await _db.GetTokens();
                var tokenActive = tokens.Where(t => t.LimitExceed <= DateTime.Now).ToList();
                respon += $"Token : {tokenActive.Count}/{tokens.Count}";
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            // kirim status
            await BotClient.SendTextAsync(message, respon, parse: ParseMode.Markdown);
        }

        private static async void Token(Message message, string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
                new Simsimi(message).SaveToken(data);
            else
                await BotClient.SendTextAsync(message,
                    BotResponse.SimsimiHowToGetToken(),
                    parse: ParseMode.Markdown);
        }
    }
}