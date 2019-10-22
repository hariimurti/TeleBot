using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Syn.Bot.Siml;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;

namespace TeleBot.Plugins
{
    public class ChatBot
    {
        private static Log _log = new Log("ChatBot");
        private static SimlBot _siml;
        private static Yandex _yandex;
        private BotUser _user;
        private Message _message;

        public static async Task LoadLibraryAsync()
        {
            if (_siml != null) return;
            await Task.Run(() =>
            {
                _log.Debug("Creating new chatbot...");
                _yandex = new Yandex();
                _siml = new SimlBot();
                var simlDir = Program.FilePathInData("Siml");
                var simlFiles = Directory.GetFiles(simlDir, "*.siml", SearchOption.AllDirectories);
                foreach (var simlFile in simlFiles)
                {
                    var name = Path.GetFileName(simlFile);
                    try
                    {
                        _log.Debug($"Loading {name}");

                        var simlDocument = XDocument.Load(simlFile);
                        _siml.Import(simlDocument);
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message);
                    }
                }
            });
        }

        public ChatBot(Message message)
        {
            _message = message;

            LoadLibraryAsync().GetAwaiter();
            
            _user = _siml.CreateUser(message.Chat.Id.ToString());
        }

        public async void Reload()
        {
            if (!_message.IsGodMode())
            {
                await BotClient.SendTextAsync(_message, "Kamu siapa? Kok berani-beraninya nyuruh aku 😡😡");
                return;
            }
            
            _message = await BotClient.SendTextAsync(_message, "Please wait...", true);

            await LoadLibraryAsync();

            await BotClient.EditOrSendTextAsync(_message, _message.MessageId, "Okey, it's done...");
        }

        public async void SendResponse()
        {
            var chatRequest = new ChatRequest(_message.Text, _user);
            var chatResult = _siml.Chat(chatRequest);
            if (!chatResult.Success)
            {
                var noResponse = BotClass.BotResponse.SimsimiNullResult();
                await BotClient.SendTextAsync(_message, noResponse);
                return;
            }

            await BotClient.SendTextAsync(_message, chatResult.BotMessage);
        }
    }
}