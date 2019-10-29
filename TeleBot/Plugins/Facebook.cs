using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;

namespace TeleBot.Plugins
{
    class Facebook
    {
        private static Log _log = new Log("Facebook");
        private Message _message;

        public Facebook(Message message)
        {
            _message = message;
        }

        public async void GetMedia(string data)
        {
            if (!_message.IsPrivateChat()) return;

            var matchUrl = Regex.Match(data, "(http.*facebook.com/.*[0-9]+/?)");
            if (!matchUrl.Success)
            {
                await BotClient.SendTextAsync(_message, "Link facebook salah?", true);
                return;
            }

            _message = await BotClient.SendTextAsync(_message, "Tunggu sebentar...", true);

            try
            {
                var videourl = await GetVideoUrl(matchUrl.Groups[1].Value);
                
                if (string.IsNullOrWhiteSpace(videourl))
                {
                    await BotClient.EditOrSendTextAsync(_message.Chat.Id, _message.MessageId, "Gak ada videonya!");
                    return;
                }

                await BotClient.SendVideoAsync(_message, videourl);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await BotClient.EditOrSendTextAsync(_message.Chat.Id, _message.MessageId, e.Message);
            }
        }

        private async Task<string> GetVideoUrl(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.GET;
            request.AddHeader("User-Agent", App.UserAgent);
            request.AddHeader("content-type", "text/html");

            var response = await client.ExecuteTaskAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.StatusDescription);

            var mHd = Regex.Match(response.Content, "hd_src_no_ratelimit\"?:\"(.*?)\"");
            if (mHd.Success) return mHd.Groups[1].Value;

            var mSd = Regex.Match(response.Content, "sd_src_no_ratelimit\"?:\"(.*?)\"");
            if (mSd.Success) return mSd.Groups[1].Value;

            return string.Empty;
        }
    }
}
