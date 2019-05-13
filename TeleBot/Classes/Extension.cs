using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using TeleBot.BotClass;

namespace TeleBot.Classes
{
    public static class Extension
    {
        private static Log _log = new Log("StringExtension");
        
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
            var pattern =
                @"\b((?:[s]+[i]+[m]+ ?)?[s]+[i]+[m]+[iy]+(?:[w]+[a]+[t]+[i]+|[n]+[y]+[a]+|[k]+[u]+|[m]+[u]+)?)\b";
            var result = Regex.Replace(text, pattern, Bot.Name, RegexOptions.IgnoreCase);

            // ganti "sim" jika tdk ada kata berikut
            pattern = @"\b(gak|gk|tidak|tdk|ndak|ndk|punya|pnya|pny|gpny)\b";
            if (!Regex.IsMatch(result, pattern, RegexOptions.IgnoreCase))
                result = Regex.Replace(result, @"\b(sim ?)\b", string.Empty, RegexOptions.IgnoreCase);

            return result;
        }

        public static string RemoveHtmlTag(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty).Trim();
        }

        public static string EncodeToUrl(this string text)
        {
            try
            {
                return WebUtility.UrlEncode(text);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return text;
            }
        }

        public static string DecodeFromUrl(this string text)
        {
            try
            {
                return WebUtility.UrlDecode(text);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return text;
            }
        }

        public static string EncodeToHtml(this string text)
        {
            try
            {
                return HttpUtility.HtmlDecode(text);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return text;
            }
        }

        public static string DecodeFromHtml(this string text)
        {
            try
            {
                return HttpUtility.HtmlDecode(text);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return text;
            }
        }

        public static string ToHumanSizeFormat(this double value)
        {
            string[] suffixes = {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            for (var i = 0; i < suffixes.Length; i++)
                if (value <= Math.Pow(1024, i + 1))
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " +
                   suffixes[suffixes.Length - 1];
        }

        private static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
                return value.ToString("0,0");
            if (value >= 10)
                return value.ToString("0.0");
            return value.ToString("0.00");
        }
    }
}