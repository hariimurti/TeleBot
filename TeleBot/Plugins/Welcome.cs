using System;
using TeleBot.BotClient;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public static class Welcome
    {
        public static async void SendGreeting(Message message)
        {
            var mention = string.Empty;
            foreach (var member in message.NewChatMembers)
            {
                var usernameExist = !string.IsNullOrWhiteSpace(member.Username);
                if (usernameExist && string.Equals(member.Username, Bot.Username, StringComparison.OrdinalIgnoreCase))
                {
                    Command.Start(message);
                    return;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(mention)) mention += " ";

                    var name = (member.FirstName + " " + member.LastName).Trim();
                    if (string.IsNullOrWhiteSpace(name)) name = member.Username;
                    if (string.IsNullOrWhiteSpace(name)) name = member.Id.ToString();

                    mention += $"[{name}](tg://user?id={member.Id}),";
                }
            }
            
            if (string.IsNullOrWhiteSpace(mention)) return;
            
            var greeting = Bot.Keys.SayHelloNewMember
                .ReplaceWithBotValue()
                .Replace("{member}", mention.TrimEnd(','))
                .Replace("{group}", message.ChatName());
            
            await Bot.SendTextAsync(message, greeting, parse: ParseMode.Markdown);
        }
    }
}