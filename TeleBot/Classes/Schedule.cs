using System;
using System.Timers;
using TeleBot.BotClient;
using TeleBot.SQLite;

namespace TeleBot.Classes
{
    public static class Schedule
    {
        private static Log _log = new Log("Schedule");
        private static Database _db = new Database();
        private static bool _oneTime;

        public static async void ReSchedule()
        {
            if (_oneTime) return;
            
            var list = await _db.GetAllSchedule();
            if (list.Count > 0)
                _log.Debug("ReSchedule: {0} items", list.Count);
            
            foreach (var data in list)
            {
                Register(data);
            }

            _oneTime = true;
        }

        public static async void RegisterNew(ScheduleData data)
        {
            var result = await _db.InsertOrReplaceSchedule(data);
            if (!result) return;
            
            Register(data);
        }
        
        private static void Register(ScheduleData data)
        {
            var interval = (data.DateTime - DateTime.Now).TotalMilliseconds;
            if (interval <= 0)
            {
                ExecuteTask(data);
                return;
            }
            
            var timer = new Timer(interval);
            timer.Elapsed += (sender, e) => TimerElapsed(timer, data);
            timer.Start();
        }
        
        private static void TimerElapsed(Timer timer, ScheduleData data)
        {
            timer?.Stop();
            timer?.Dispose();

            ExecuteTask(data);
        }
        
        private static async void ExecuteTask(ScheduleData data)
        {
            _log.Debug("Jalankan tugas: {0}", data.MessageId);

            switch (data.Operation)
            {
                case ScheduleData.Type.Delete:
                    await Bot.DeleteMessageAsync(data.ChatId, data.MessageId);
                    break;

                case ScheduleData.Type.Edit:
                    await Bot.EditOrSendTextAsync(data.ChatId, data.MessageId, data.Text);
                    break;
            }
                
            data.Operation = ScheduleData.Type.Done;
            await _db.InsertOrReplaceSchedule(data);
        }
    }
}