﻿using System;
using System.Text.RegularExpressions;
using TeleBot.BotClient;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public static class Command
    {
        private static Log _log = new Log("Command");
        
        public static void Execute(Message message)
        {
            // pemisahan command, mention & data
            var regexCmd = Regex.Match(message.Text, @"^\/(\w+)(?:@(\w+))?\b ?(.+)?", RegexOptions.IgnoreCase);
            if (!regexCmd.Success)
            {
                _log.Warning("Regex tdk dapat memisahkan command!");
                return;
            }

            var cmd = regexCmd.Groups[1].Value.ToLower();
            var mention = regexCmd.Groups[2].Value;
            var data = regexCmd.Groups[3].Value;

            if (!string.IsNullOrWhiteSpace(mention) && (!string.Equals(mention, Bot.Username, StringComparison.OrdinalIgnoreCase)))
            {
                _log.Ignore("Username \"{0}\" (mention) tdk sama dgn \"{1}\" (bot)", mention, Bot.Username);
                return;
            }

            switch (cmd)
            {
                case "start":
                    Start(message);
                    break;
                
                case "help":
                    Help(message);
                    break;
                
                case "echo":
                    Echo(message, data);
                    break;
                
                default:
                    _log.Ignore("Perintah: {0} -- tdk ada", cmd);
                    break;
            }
        }

        private static async void Start(Message message)
        {
            var respon = Bot.Keys.SayHello.ReplaceWithBotValue();
            await Bot.SendTextAsync(message, respon, parse: ParseMode.Markdown);
        }

        private static async void Help(Message message)
        {
            await Bot.SendTextAsync(message, "Ini adalah pesan help");
        }
        
        private static async void Echo(Message message, string data)
        {
            await Bot.SendTextAsync(message, data);
        }
    }
}