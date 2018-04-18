using System;
using SQLite;

namespace TeleBot.SQLite
{
    [Table("Logs")]
    public class LogData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string DateTime { get; set; }
        public string Level { get; set; }
        public string Header { get; set; }
        public string Message { get; set; }
    }
}