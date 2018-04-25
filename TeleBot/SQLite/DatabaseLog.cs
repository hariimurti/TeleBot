using System;
using System.Collections.Generic;
using SQLite;

namespace TeleBot.SQLite
{
    public class DatabaseLog
    {
        private static SQLiteAsyncConnection _con;
        private static Queue<LogData> _queueLog = new Queue<LogData>();
        private static bool _queueStart;

        public DatabaseLog()
        {
            try
            {
                if (_con != null) return;

                _con = new SQLiteAsyncConnection(Program.FilePathInData("Logs.db"));
                _con.SetBusyTimeoutAsync(TimeSpan.FromSeconds(20));
                _con.CreateTableAsync<LogData>();
            }
            catch (SQLiteException ex)
            {
                LogError(ex.Message);
            }
        }

        public void Insert(LogData data)
        {
            _queueLog.Enqueue(data);
            StartQueue();
        }

        private async void StartQueue()
        {
            try
            {
                if (_queueStart) return;

                _queueStart = true;

                while (_queueLog.Count > 0)
                {
                    var data = _queueLog.Dequeue();
                    await _con.InsertAsync(data);
                }

                _queueStart = false;
            }
            catch (Exception ex)
            {
                LogError("WriteLog: {0}", ex.Message);
            }
        }

        private void LogError(string message, params Object[] args)
        {
            message = string.Format(message, args);
            var timenow = DateTime.Now.ToLongTimeString();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{timenow} » Error » Database » {message}");
            Console.ResetColor();
        }
    }
}