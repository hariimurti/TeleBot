using System;
using System.Linq;
using System.Text.RegularExpressions;
using LinqToTwitter;
using Newtonsoft.Json;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;

namespace TeleBot.Plugins
{
    public class Twitter
    {
        private static Log _log = new Log("Twitter");
        private static readonly string JsonFile = Program.FilePathInData("Twitter.json");
        private static TwitterContext _twitter;
        private Message _message;
        
        private class AuthTwitter
        {
            public string ConsumerKey { get; set; }
            public string ConsumerSecret { get; set; }
            public string AuthToken { get; set; }
            public string AuthTokenSecret { get; set; }
        }

        public Twitter(Message message)
        {
            _message = message;
            
            if (_twitter != null) return;
            try
            {
                if (!System.IO.File.Exists(JsonFile))
                {
                    var json = JsonConvert.SerializeObject(new AuthTwitter(), Program.JsonSettings);

                    System.IO.File.WriteAllText(JsonFile, json);
                }
                else
                {
                    var json = System.IO.File.ReadAllText(JsonFile);
                    var auth = JsonConvert.DeserializeObject<AuthTwitter>(json);
                    
                    if (string.IsNullOrWhiteSpace(auth.ConsumerKey) ||
                        string.IsNullOrWhiteSpace(auth.ConsumerSecret) ||
                        string.IsNullOrWhiteSpace(auth.AuthToken) ||
                        string.IsNullOrWhiteSpace(auth.AuthTokenSecret))
                        throw new Exception("Auth token can't be null");
                    
                    var authorizer = new SingleUserAuthorizer
                    {
                        CredentialStore = new SingleUserInMemoryCredentialStore
                        {
                            ConsumerKey = auth.ConsumerKey,
                            ConsumerSecret = auth.ConsumerSecret,
                            OAuthToken = auth.AuthToken,
                            OAuthTokenSecret = auth.AuthTokenSecret
                        }
                    };
                    
                    _twitter =new TwitterContext(authorizer);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        public async void GetMedia(string data)
        {
            if (!_message.IsPrivateChat()) return;
            
            if (_twitter == null)
            {
                await BotClient.SendTextAsync(_message,
                    "Tolong, bilang ke admin paijem suruh isi auth token twitter. Terima kasih");
                return;
            }
            
            var regex = Regex.Match(data, @"twitter.com\/.*status\/([0-9]+)");
            if (!regex.Success) return;

            ulong.TryParse(regex.Groups[1].Value, out ulong statusId);
            
            _message = await BotClient.SendTextAsync(_message, "Tunggu sebentar...", true);
            
            var status = await _twitter.Status
                .Where(x => x.ID == statusId && x.Type == StatusType.Show && x.TweetMode == TweetMode.Extended)
                .SingleOrDefaultAsync();

            var foundVideo = false;
            foreach (var media in status?.ExtendedEntities?.MediaEntities)
            {
                if (media.Type != "video") continue;

                foundVideo = true;
                var video = media.VideoInfo?.Variants
                    .Where(x => x.ContentType == "video/mp4")
                    .OrderByDescending(x => x.BitRate)
                    .First();
                
                await BotClient.SendVideoAsync(_message, video.Url, media.MediaUrlHttps, status.FullText);
            }
            
            if (foundVideo)
                await BotClient.DeleteMessageAsync(_message.Chat.Id, _message.MessageId);
            else
                await BotClient.EditOrSendTextAsync(_message, _message.MessageId, "Gak ada videonya!");
        }
    }
}