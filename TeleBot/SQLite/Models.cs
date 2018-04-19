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
    
    [Table("Contacts")]
    public class Contact
    {
        [PrimaryKey, NotNull]
        public long Id { get; set; }
        
        public string Name { get; set; }
        public string UserName { get; set; }
        public bool Private { get; set; }
        public bool Blocked { get; set; }
    }

    [Table("Incomings")]
    public class MessageIncoming
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [NotNull]
        public int MessageId { get; set; }
        public long ChatId { get; set; }
        public long FromId { get; set; }
        public string ChatName { get; set; }
        public string FromName { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
    }
    
    [Table("Outgoings")]
    public class MessageOutgoing
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [NotNull]
        public int MessageId { get; set; }
        public long ChatId { get; set; }
        public string ChatName { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
    }
    
    [Table("Simsimi")]
    public class Token
    {
        [PrimaryKey, NotNull]
        public string Key { get; set; }

        public DateTime Expired { get; set; }
        public DateTime LimitExceed { get; set; }
    }
    
    [Table("Schedules")]
    public class ScheduleData
    {
        public enum Type { Delete, Edit, Done }

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [NotNull]
        public long ChatId { get; set; }
        [NotNull]
        public int MessageId { get; set; }
        public Type Operation { get; set; }
        public DateTime DateTime { get; set; }
        public string Text { get; set; }
    }
    
    [Table("Bookmark")]
    public class Hashtag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public long ChatId { get; set; }
        [NotNull]
        public int MessageId { get; set; }

        public string KeyName { get; set; }
    }
}