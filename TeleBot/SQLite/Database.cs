using System;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.SQLite
{
    public class Database
    {
        private static Log _log = new Log("Database");
        private static SQLiteAsyncConnection _db;

        public Database()
        {
            try
            {
                if (_db != null) return;
                
                _db = new SQLiteAsyncConnection(Program.FilePathInData("database.db"));
                _db.SetBusyTimeoutAsync(TimeSpan.FromSeconds(20));
                _db.CreateTableAsync<Contact>();
                _db.CreateTableAsync<MessageIncoming>();
                _db.CreateTableAsync<MessageOutgoing>();
            }
            catch(SQLiteException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public async Task<bool> InsertMessageIncoming(Message data)
        {
            var result = false;
            
            try
            {
                // cari pesan yg sama
                var exists = await _db.Table<MessageIncoming>()
                    .Where(m => m.MessageId == data.MessageId && m.ChatId == data.Chat.Id)
                    .ToListAsync();
                if (exists.Count > 0) return false;
                
                // MessageIncoming
                var message = new MessageIncoming()
                {
                    MessageId = data.MessageId,
                    ChatId = data.Chat.Id,
                    ChatName = data.Chat.FirstName,
                    FromId = data.From.Id,
                    FromName = data.From.FirstName,
                    Text = data.Text,
                    DateTime = data.Date
                };
                
                // insert message
                await _db.InsertAsync(message);
                result = true;
                
                // Contact
                var contact = new Contact()
                {
                    Id = data.Chat.Id,
                    Name = data.Chat.FirstName,
                    UserName = data.Chat.Username,
                    Private = (data.Chat.Type == ChatType.Private)
                };
                
                // cari kontak yg sudah ada
                var findContacts = await _db.Table<Contact>().Where(c => c.Id == data.Chat.Id).ToListAsync();
                if (findContacts.Count > 0)
                {
                    var exist = findContacts.FirstOrDefault();
                    contact.Blocked = exist.Blocked;
                }
                
                // update contacts
                await _db.InsertOrReplaceAsync(contact);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                result = false;
            }

            return result;
        }

        public async Task InsertMessageOutgoing(Message data)
        {
            try
            {
                var message = new MessageOutgoing()
                {
                    MessageId = data.MessageId,
                    ChatId = data.Chat.Id,
                    ChatName = data.Chat.FirstName,
                    Text = data.Text,
                    DateTime = data.Date
                };
                
                // insert message
                await _db.InsertAsync(message);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }
    }
}