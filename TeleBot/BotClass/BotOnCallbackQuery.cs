using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using TeleBot.Classes;
using TeleBot.Plugins;
using TeleBot.SQLite;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace TeleBot.BotClass
{
    public class BotOnCallbackQuery
    {
        private static Log _log = new Log("OnCallbackQuery");
        private static List<CallbackData> _callbackBlocked = new List<CallbackData>();
        private static readonly int _callbackBlockedTimeout = 60;

        private class CallbackData
        {
            public int MessageId { get; set; }
            public int FromId { get; set; }
            public string Info { get; set; }
            public DateTime LastTime { get; set; }
        }

        public static async void Handle(object sender, CallbackQueryEventArgs e)
        {
            var callback = e.CallbackQuery;
            var message = e.CallbackQuery.Message;

            var query = Regex.Match(callback.Data, @"^cmd=([\w\-]+)&data=(\S+)$");
            if (!query.Success)
            {
                _log.Error("Unknown: {0}", callback.Data);
                await BotClient.AnswerCallbackQueryAsync(callback.Id, "Perintah tidak diketahui", true);
                return;
            }

            var cmd = query.Groups[1].Value;
            var data = query.Groups[2].Value;

            switch (cmd)
            {
                case "greeting":
                    new Welcome(message, callback).Manage(data);
                    break;

                case "call":
                    if (await IsCallbackBlocked(callback, "Hashtag")) return;
                    await new Bookmark(message, callback).ForwardHashtag(data);
                    break;

                case "generate":
                    new Bookmark(message, callback).GenerateList(true, true);
                    break;

                case "manage":
                    new Bookmark(message, callback).ManageList();
                    break;

                case "remove":
                    new Bookmark(message, callback).DeleteWithButton(data);
                    break;

                case "remove-final":
                    new Bookmark(message, callback).DeleteWithButton(data, true);
                    break;

                case "info":
                    new Bookmark(message, callback).ShowInfo(data);
                    break;

                case "mobilism":
                    if (await IsCallbackBlocked(callback, "Mobilism")) return;
                    new Mobilism(message, callback).OpenThread(data);
                    break;

                default:
                    _log.Ignore("Unknown: {0}", callback.Data);
                    await BotClient.AnswerCallbackQueryAsync(callback.Id, "Perintah tidak diketahui", true);
                    break;
            }
        }

        private static async Task<bool> IsCallbackBlocked(CallbackQuery callback, string info)
        {
            var message = callback.Message;
            var found = _callbackBlocked.Where(c => c.MessageId == message.MessageId && c.FromId == callback.From.Id)
                .ToList().FirstOrDefault();

            if (found != null)
            {
                var countdown = (DateTime.Now - found.LastTime).Seconds;
                if (countdown < _callbackBlockedTimeout)
                {
                    await BotClient.AnswerCallbackQueryAsync(callback.Id,
                        $"Sabar ya kaka...\nTunggu {_callbackBlockedTimeout - countdown} dtk baru bisa tekan tombol ini lagi.",
                        true);

                    return true;
                }

                return false;
            }

            _log.Debug("Block Callback: {0}: {1} -- {2}", info, message.MessageId, callback.From.Id);

            var callbackData = new CallbackData
            {
                MessageId = message.MessageId,
                FromId = callback.From.Id,
                Info = info,
                LastTime = DateTime.Now
            };

            _callbackBlocked.Add(callbackData);

            var timer = new Timer(_callbackBlockedTimeout * 1000);
            timer.Elapsed += (ts, te) => TimerElapsed(timer, callbackData);
            timer.Start();
            return false;
        }

        private static void TimerElapsed(Timer timer, CallbackData data)
        {
            timer?.Stop();
            timer?.Dispose();

            _log.Debug("Unblock Callback: {0}: {1} -- {2}", data.Info, data.MessageId, data.FromId);
            _callbackBlocked.Remove(data);
        }
    }
}