using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TeleBot.Plugins
{
    public class Qwant
    {
        private static Log _log = new Log("Qwant");
        private static Random _random = new Random();
        private const string apiUrl = "https://api.qwant.com/api/search/";
        private Message _message;

        #region ApiClass

        private class Response
        {
            public string Status { get; set; }
            public Data Data { get; set; }
        }

        private class Data
        {
            public DataQuery Query { get; set; }
            public DataResult Result { get; set; }
        }

        private class DataQuery
        {
            public string Locale { get; set; }
            public string Query { get; set; }
            public int Offset { get; set; }
        }

        private class DataResult
        {
            public Items[] Items { get; set; }
        }

        private class Items
        {
            public string Title { get; set; }
            public string Favicon { get; set; }
            public string Url { get; set; }
            public string Source { get; set; }
            public string Desc { get; set; }
            public string _id { get; set; }
            public int Position { get; set; }

            // image
            public string Type { get; set; }

            public string Media { get; set; }
            public string Thumbnail { get; set; }

            [JsonProperty("thumb_type")]
            public string Extension { get; set; }

            public int Count { get; set; }
        }

        #endregion

        public Qwant(Message message)
        {
            _message = message;
        }

        private async Task<Response> GetResponse(string address)
        {
            var request = new WebRequest()
            {
                Url = address,
                Method = WebMethod.Get
            };
            var response = await WebClient.GetOrPostStringAsync(request);
            return JsonConvert.DeserializeObject<Response>(response);
        }

        public async void SearchWeb(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            _log.Debug("Pencarian : {0}", query);
            
            var address = apiUrl + "web?count=10&locale=id_ID&q=" + query.EncodeUrl();
            Response result;
            
            try
            {
                result = await GetResponse(address);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);

                await BotClient.SendTextAsync(_message, "Mohon maaf...\nPlugin qwant sedang mengalami gangguan.\nCobalah beberapa saat lagi.", true);
                return;
            }
            
            var items = result.Data.Result.Items;
            if (items.Length == 0)
            {
                _log.Warning("Pencarian tidak membuahkan hasil...");
                
                await BotClient.SendTextAsync(_message, $"Maaf kak... {Bot.Name} gak nemu yang dicari..", true);
                return;
            }
            
            _log.Debug("Hasil : {0}", items.Length);

            var count = 0;
            var padding = items.Length.ToString().Length;
            var respon = $"Pencarian : {query}\n—— —— —— —— —— ——";
            foreach (var item in items)
            {
                count++;
                var num = count.ToString().PadLeft(padding, '0');
                respon += $"\n{num}. <a href=\"{item.Url}\">{item.Title.RemoveHtmlTag().Trim()}</a>";
            }
            
            await BotClient.SendTextAsync(_message, respon, parse: ParseMode.Html, preview: false);
        }

        public async void SearchImage(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            _log.Debug("Pencarian : {0}", query);
            
            var address = apiUrl + "images?count=10&offset=1&q=" + query.EncodeUrl();
            Response result;
            
            try
            {
                result = await GetResponse(address);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);

                await BotClient.SendTextAsync(_message, "Mohon maaf...\nPlugin qwant sedang mengalami gangguan.\nCobalah beberapa saat lagi.", true);
                return;
            }
            
            var items = result.Data.Result.Items;
            if (items.Length == 0)
            {
                _log.Warning("Pencarian tidak membuahkan hasil...");
                
                await BotClient.SendTextAsync(_message, $"Maaf kak... {Bot.Name} gak nemu yang dicari..", true);
                return;
            }
            
            _log.Debug("Hasil : {0}", items.Length);

            var queue = new Queue<Items>();
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }

            while (queue.Count > 0)
            {
                var item = items[_random.Next(0, items.Length)];
                var title = item.Title.DecodeUrl();
                var desc = item.Desc;
                
                _log.Debug("Kirim image : {0}", item.Url);
                
                var image = new InputOnlineFile(item.Media);
                var caption = $"Hasil pencarian : {query}";
                if (!string.IsNullOrWhiteSpace(title))
                    caption += $"\nJudul : {title}";
                if (!string.IsNullOrWhiteSpace(desc))
                    caption += $"\nDeskripsi : {desc}";
                
                var sentPhoto = await BotClient.SendPhotoAsync(_message, image, caption);
                
                if (sentPhoto != null) break;
            }
        }
    }
}