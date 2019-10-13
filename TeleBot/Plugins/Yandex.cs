using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using TeleBot.Classes;

namespace TeleBot.Plugins
{
    public class Yandex
    {
        private static Log _log = new Log("Yandex");
        private static readonly string JsonFile = Program.FilePathInData("Yandex.json");
        private string apiKey;

        public class Result
        {
            public bool Success { get; set; }
            public string Text { get; set; }
            public string Message { get; set; }
        }

        private class Response
        {
            public int Code { get; set; }
            public string Lang { get; set; }
            public string[] Text { get; set; }
        }
        
        private class JsonKey
        {
            public string ApiKey { get; set; }
        }

        public Yandex()
        {
            if (!File.Exists(JsonFile))
            {
                var key = new JsonKey();
                var json = JsonConvert.SerializeObject(key, Program.JsonSettings);

                File.WriteAllText(JsonFile, json);
            }
            else
            {
                var json = File.ReadAllText(JsonFile);
                var key = JsonConvert.DeserializeObject<JsonKey>(json);
                apiKey = key.ApiKey;
            }
        }

        public async Task<Result> Translate(string text)
        {
            return await Translate("id", text);
        }

        public async Task<Result> Translate(string fromLangId, string toLangId, string text)
        {
            return await Translate($"{fromLangId}-{toLangId}", text);
        }

        public async Task<Result> Translate(string direction, string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new Exception("Empty API Key");

                var client = new RestClient("https://translate.yandex.net");
                var request = new RestRequest("api/v1.5/tr.json/translate");
                request.AddParameter("key", apiKey);
                request.AddParameter("text", text);
                request.AddParameter("lang", direction);
                request.Method = Method.POST;
                request.RequestFormat = DataFormat.Json;

                _log.Debug($"Translate: {direction}: {text}");
                
                var response = await client.ExecuteTaskAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(response.StatusDescription);

                var json = JsonConvert.DeserializeObject<Response>(response.Content);
                switch (json.Code)
                {
                    case 401: throw new Exception("Invalid API key");
                    case 402: throw new Exception("Blocked API key");
                    case 404: throw new Exception("Exceeded the daily limit on the amount of translated text");
                    case 413: throw new Exception("Exceeded the maximum text size");
                    case 422: throw new Exception("The text cannot be translated");
                    case 501: throw new Exception("The specified translation direction is not supported");
                }
                
                _log.Debug($"Result: {json.Text.First()}");

                return new Result()
                {
                    Success = true,
                    Text = json.Text.First()
                };
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return new Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        public async Task<Result> DetectLanguage(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new Exception("Empty API Key");

                var client = new RestClient("https://translate.yandex.net");
                var request = new RestRequest("api/v1.5/tr.json/detect");
                request.AddParameter("key", apiKey);
                request.AddParameter("text", text);
                request.Method = Method.POST;
                request.RequestFormat = DataFormat.Json;

                _log.Debug($"Detect Language: {text}");
                
                var response = await client.ExecuteTaskAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(response.StatusDescription);

                var json = JsonConvert.DeserializeObject<Response>(response.Content);
                switch (json.Code)
                {
                    case 401: throw new Exception("Invalid API key");
                    case 402: throw new Exception("Blocked API key");
                    case 404: throw new Exception("Exceeded the daily limit on the amount of translated text");
                }

                _log.Debug($"Result : {json.Lang}");
                
                return new Result()
                {
                    Success = true,
                    Text = json.Lang
                };
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return new Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }
    }
}