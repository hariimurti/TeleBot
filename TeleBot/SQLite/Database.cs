﻿using System;
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
                _con.CreateTableAsync<ScheduleData>();
            }
            catch(SQLiteException ex)
            {
                _log.Error(ex.Message);
            }
        }

        public async Task<Contact> FindContact(long chatId)
        {
            try
            {
                var list = await _con.Table<Contact>().Where(c => c.Id == chatId).ToListAsync();
                return list.FirstOrDefault();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }

        public async Task<MessageIncoming> FindMessageIncoming(int messageId, long chatId)
        {
            try
            {
                var list = await _con.Table<MessageIncoming>()
                    .Where(m => m.MessageId == messageId && m.ChatId == chatId)
                    .ToListAsync();
                return list.FirstOrDefault();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }

        public async Task<MessageIncoming> FindLastMessageIncoming(long chatId, bool secondLast = false)
        {
            try
            {
                var list = await _con.Table<MessageIncoming>()
                    .Where(m => m.ChatId == chatId)
                    .OrderByDescending(m => m.DateTime)
                    .ToListAsync();

                if (list.Count > 1 && secondLast)
                {
                    return list[1];
                }

                return list.FirstOrDefault();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }

        public async Task<MessageOutgoing> FindLastMessageOutgoing(long chatId)
        {
            try
            {
                var list = await _con.Table<MessageOutgoing>()
                    .Where(m => m.ChatId == chatId)
                    .OrderByDescending(m => m.DateTime)
                    .ToListAsync();

                return list.FirstOrDefault();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
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

        public async Task<bool> InsertOrReplaceContact(Message data)
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
                var exist = await FindContact(data.Chat.Id);
                if (exist != null)
                {
                    contact.Blocked = exist.Blocked;
                }
                
                // update contacts
                await _con.InsertOrReplaceAsync(contact);
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return false;
            }
        }

        public async Task<bool> InsertMessageIncoming(Message data)
        {
            try
            {
                // tambah/perbarui contact
                await InsertOrReplaceContact(data);
                
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

        public async Task<List<ScheduleData>> GetAllSchedule()
        {
            try
            {
                return await _con.Table<ScheduleData>()
                    .Where(s => s.Operation != ScheduleData.Type.Done)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }

        public async Task<bool> InsertOrReplaceSchedule(ScheduleData data)
        {
            try
            {
                var list = await GetAllSchedule();
                var schedule = list.Where(s => s.ChatId == data.ChatId && s.MessageId == data.MessageId)
                    .ToList()
                    .FirstOrDefault();
                if (schedule != null)
                {
                    data.Id = schedule.Id;
                    await _con.InsertOrReplaceAsync(data);
                }
                else
                {
                    await _con.InsertAsync(data);
                }
                return true;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return false;
            }
        }
    }
}