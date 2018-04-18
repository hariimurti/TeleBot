using System;
using TeleBot.SQLite;

namespace TeleBot.Classes
{
    public class Log
    {
        public static bool ShowDebug = false;
        private readonly DatabaseLog _db = new DatabaseLog();
        private readonly string _header;
        
        private enum Level
        { Debug, Info, Ignore, Warning, Error }

        public Log(string header)
        {
            _header = header;
        }

        public void Info(string message, params Object[] args)
        {
            Print(string.Format(message, args), Level.Info);
        }

        public void Debug(string message, params Object[] args)
        {
            Print(string.Format(message, args), Level.Debug);
        }

        public void Ignore(string message, params Object[] args)
        {
            Print(string.Format(message, args), Level.Ignore);
        }

        public void Warning(string message, params Object[] args)
        {
            Print(string.Format(message, args), Level.Warning);
        }

        public void Error(string message, params Object[] args)
        {
            Print(string.Format(message, args), Level.Error);
        }

        private void Print(string message, Level level)
        {
            // formating teks
            var dtnow = DateTime.Now;
            var text = $"{dtnow.ToLongTimeString()} » {level} » {_header} » {message}";

            // ganti warna teks
            switch (level)
            {
                case Level.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case Level.Ignore:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case Level.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case Level.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                default:
                    Console.ResetColor();
                    if (!ShowDebug) return;
                    break;
            }
            
            // tampilkan log ke console
            Console.WriteLine(text.SingleLine());
            Console.ResetColor();

            // masukkan ke database
            var data = new LogData()
            {
                DateTime = $"{dtnow.ToShortDateString()} {dtnow.ToLongTimeString()}",
                Level = level.ToString(),
                Header = _header,
                Message = message
            };

            _db.Insert(data);
        }
    }
}