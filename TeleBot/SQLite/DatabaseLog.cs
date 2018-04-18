using System;
using System.Collections.Generic;
using SQLite;

namespace TeleBot.SQLite
{
    public class DatabaseLog
    {
        private static SQLiteAsyncConnection _db;
        private static Queue<LogData> _queueLog = new Queue<LogData>();
        private static bool _queueStart;

        public DatabaseLog()
        {
            try
            {
                if (_db != null) return;
                
                _db = new SQLiteAsyncConnection(Program.FilePathInData("logs.db"));
                _db.SetBusyTimeoutAsync(TimeSpan.FromSeconds(20));
                _db.CreateTableAsync<LogData>();
            }
            catch(SQLiteException ex)
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
                    await _db.InsertAsync(data);
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