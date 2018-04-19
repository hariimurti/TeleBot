using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using TeleBot.BotClient;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.SQLite
{
    public class Database
    {
        private static Log _log = new Log("Database");
        private static SQLiteAsyncConnection _con;

        public Database()
        {
            try
            {
                if (_con != null) return;
                
                _con = new SQLiteAsyncConnection(Program.FilePathInData("Database.db"));
                _con.SetBusyTimeoutAsync(TimeSpan.FromSeconds(20));
                _con.CreateTableAsync<Contact>();
                _con.CreateTableAsync<MessageIncoming>();
                _con.CreateTableAsync<MessageOutgoing>();
                _con.CreateTableAsync<Token>();
            }
            catch(SQLiteException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public async Task InsertOrReplaceContact(Message data)
        {
            try
            {
                var contact = new Contact()
                {
                    Id = data.Chat.Id,
                    Name = data.ChatName(),
                    UserName = data.Chat.Username,
                    Private = (data.Chat.Type == ChatType.Private)
                };
                
                // cari kontak yg sudah ada
                var findContacts = await _con.Table<Contact>().Where(c => c.Id == data.Chat.Id).ToListAsync();
                if (findContacts.Count > 0)
                {
                    var isBlocked = findContacts.FirstOrDefault()?.Blocked;
                    contact.Blocked = isBlocked == true;
                }
                
                // update contacts
                await _con.InsertOrReplaceAsync(contact);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        public async Task<bool> InsertMessageIncoming(Message data)
        {
            try
            {
                // tambah/perbarui contact
                await InsertOrReplaceContact(data);
                
                // cari pesan yg sama
                var exists = await _con.Table<MessageIncoming>()
                    .Where(m => m.MessageId == data.MessageId && m.ChatId == data.Chat.Id)
                    .ToListAsync();
                if (exists.Count > 0) return false;
                
                // MessageIncoming
                var message = new MessageIncoming()
                {
                    MessageId = data.MessageId,
                    ChatId = data.Chat.Id,
                    ChatName = data.ChatName(),
                    FromId = data.From.Id,
                    FromName = data.FromName(),
                    Text = data.Text,
                    DateTime = data.Date
                };
                
                // insert message
                await _con.InsertAsync(message);
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return false;
            }
        }

        public async Task InsertMessageOutgoing(Message data)
        {
            try
            {
                var message = new MessageOutgoing()
                {
                    MessageId = data.MessageId,
                    ChatId = data.Chat.Id,
                    ChatName = data.ChatName(),
                    Text = data.Type == MessageType.Text ? data.Text : data.Type.ToString(),
                    DateTime = data.Date
                };
                
                // insert message
                await _con.InsertAsync(message);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        public async Task InsertOrReplaceToken(Token token)
        {
            await _con.InsertOrReplaceAsync(token);
        }

        public async Task<Token> FindToken(string key)
        {
            var list = await _con.Table<Token>()
                .Where(t => t.Key == key)
                .ToListAsync();
            
            return list.FirstOrDefault();
        }

        public async Task<List<Token>> GetTokens()
        {
            return await _con.Table<Token>()
                .Where(t => t.Expired > DateTime.Now)
                .ToListAsync();
        }
    }
}