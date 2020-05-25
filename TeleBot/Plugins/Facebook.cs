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

            var matchUrl = Regex.Match(data, @"(https?://www.facebook\.com/.*[0-9]+/?)");
            if (!matchUrl.Success)
            {
                await BotClient.SendTextAsync(_message, "Link facebook salah?", true);
                return;
            }
            var facebookUrl = matchUrl.Groups[1].Value;

            _log.Debug("Found link : {0}", facebookUrl);

            _message = await BotClient.SendTextAsync(_message, "Tunggu sebentar...", true);

            try
            {
                var videourl = await GetVideoUrl(facebookUrl);
                
                if (string.IsNullOrWhiteSpace(videourl))
                {
                    _log.Debug("Video not available!");

                    await BotClient.EditOrSendTextAsync(_message.Chat.Id, _message.MessageId, "Gak ada videonya!");
                    return;
                }

                _log.Debug("Found video : {0}", videourl);

                await BotClient.SendVideoAsync(_message, videourl);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                await BotClient.EditOrSendTextAsync(_message.Chat.Id, _message.MessageId, e.Message);
            }
        }

        private async Task<string> GetSourceText(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.GET;
            request.AddHeader("User-Agent", App.UserAgent);
            request.AddHeader("content-type", "text/html");

            var response = await client.ExecuteTaskAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.StatusDescription);

            return response.Content;
        }

        private async Task<string> GetVideoUrl(string url)
        {
            if (url.Contains("comment_id="))
            {
                _log.Debug("Get link video in comment : {0}", url);

                var commentSrc = await GetSourceText(url);
                var comment = Regex.Match(commentSrc, @"(https?://www\.facebook\.com/[a-zA-Z0-9\.\-_]+/videos/[0-9]+/)");
                if (comment.Success) url = comment.Groups[1].Value.Replace("\\", "");
            }

            _log.Debug("Scrapping video : {0}", url);

            var src = await GetSourceText(url);

            var mHd = Regex.Match(src, "hd_src_no_ratelimit\"?:\"(.*?)\"");
            if (mHd.Success) return mHd.Groups[1].Value.Replace("\\", "");

            var mSd = Regex.Match(src, "sd_src_no_ratelimit\"?:\"(.*?)\"");
            if (mSd.Success) return mSd.Groups[1].Value.Replace("\\", "");

            return string.Empty;
        }
    }
}
