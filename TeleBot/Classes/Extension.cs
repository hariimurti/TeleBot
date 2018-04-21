using System.Text.RegularExpressions;
using TeleBot.BotClient;

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
        
        public static string ReplaceBotNameWithSimsimi(this string text)
        {
            text = text.Replace("@", "");
            var pattern = Bot.Keys.Alias + "|" + Bot.Username;
            var result = Regex.Replace(text, $"\\b({pattern})\\b", "simi", RegexOptions.IgnoreCase);
            return Regex.Replace(result, "\\b(simi)\\b(\\s\\1)+", "simi", RegexOptions.IgnoreCase);
        }
        
        public static string ReplaceSimsimiWithBotName(this string text)
        {
            var pattern = @"\b((?:[s]+[i]+[m]+ ?)?[s]+[i]+[m]+[i]+(?:[w]+[a]+[t]+[i]+|[n]+[y]+[a]+|[k]+[u]+|[m]+[u]+)?)\b";
            var result = Regex.Replace(text, pattern, Bot.Name, RegexOptions.IgnoreCase);
            
            // ganti "sim" jika tdk ada kata berikut
            pattern = @"\b(gak|gk|tidak|tdk|punya|pnya)\b";
            if (!Regex.IsMatch(result, pattern, RegexOptions.IgnoreCase))
            {
                result = Regex.Replace(result, @"\b(sim ?)\b", string.Empty, RegexOptions.IgnoreCase);
            }

            return result;
        }
        
        public static string RemoveHtmlTag(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }
    }
}