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
    }
}