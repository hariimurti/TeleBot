using System;
using System.Net;
using System.Text.RegularExpressions;
using TeleBot.BotClass;

namespace TeleBot.Classes
{
    public static class Extension
    {
        public static string ToSingleLine(this string text)
        {
            return text
                .Replace("\r\n", "\\r\\n")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        public static string ReplaceWithBotValue(this string text)
        {
            var alias = Bot.Keys.Alias.Replace("|", ", ").Trim();
            return text.Replace("{name}", Bot.Name)
                .Replace("{username}", Bot.Username)
                .Replace("{alias}", alias);
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
            return Regex.Replace(text, "<.*?>", string.Empty).Trim();
        }

        public static string EncodeUrl(this string text)
        {
            return WebUtility.UrlEncode(text);
        }

        public static string DecodeUrl(this string text)
        {
            return WebUtility.UrlDecode(text);
        }

        public static string ToHumanSizeFormat(this double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
            }
            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " + suffixes[suffixes.Length - 1];
        }
        
        private static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
            {
                // No digits after the decimal.
                return value.ToString("0,0");
            }
            else if (value >= 10)
            {
                // One digit after the decimal.
                return value.ToString("0.0");
            }
            else
            {
                // Two digits after the decimal.
                return value.ToString("0.00");
            }
        }
    }
}