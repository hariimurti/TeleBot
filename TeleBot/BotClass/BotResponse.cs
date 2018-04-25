using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TeleBot.Classes;
using File = System.IO.File;

namespace TeleBot.BotClass
{
    public static class BotResponse
    {
        private static Log _log = new Log("BotResponse");
        private static readonly string JsonFile = Program.FilePathInData("BotResponse.json");
        private static Random _random = new Random();
        private static BotResponseKeys Keys;

        private class BotResponseKeys
        {
            public List<string> BadWord { get; set; }
            public List<string> NoAccessToButton { get; set; }
            public List<string> ReplyOwner { get; set; }
            public List<string> ReplyAdmins { get; set; }
            public List<string> ReplyOthers { get; set; }
            public string SupportedBy { get; set; }
            public string SayHello { get; set; }
            public string WelcomeToGroup { get; set; }
            public string SimsimiHowToGetToken { get; set; }
            public List<string> SimsimiNoResponse { get; set; }
            public List<string> SimsimiOutOfToken { get; set; }
        }

        public static bool Loaded()
        {
            try
            {
                if (Keys != null) return true;

                string jsonObject;
                if (!File.Exists(JsonFile))
                {
                    _log.Debug("Menyiapkan contoh pengaturan...");
                    
                    // buat key baru
                    var newKeys = new BotResponseKeys()
                    {
                        BadWord = new List<string>(),
                        NoAccessToButton = new List<string>(),
                        ReplyOwner = new List<string>(),
                        ReplyAdmins = new List<string>(),
                        ReplyOthers = new List<string>(),
                        SupportedBy = string.Empty,
                        SayHello = string.Empty,
                        WelcomeToGroup = string.Empty,
                        SimsimiHowToGetToken = string.Empty,
                        SimsimiNoResponse = new List<string>(),
                        SimsimiOutOfToken = new List<string>()
                    };
                    jsonObject = JsonConvert.SerializeObject(newKeys, Program.JsonSettings);

                    File.WriteAllText(JsonFile, jsonObject);
                    
                    _log.Error("Silahkan isi pengaturan: {0}", Path.GetFileName(JsonFile));
                    
                    return false;
                }
                
                _log.Debug("Membaca pengaturan: {0}", Path.GetFileName(JsonFile));
                
                // buka file json
                jsonObject = File.ReadAllText(JsonFile);
                
                // parsing ke BotResponseKeys
                Keys = JsonConvert.DeserializeObject<BotResponseKeys>(jsonObject);

                // string dan list tidak boleh kosong
                var result = true;
                foreach (var prop in typeof(BotResponseKeys).GetProperties())
                {
                    var value = prop.GetValue(Keys, null);
                    if (prop.PropertyType == typeof(List<string>))
                    {
                        var list = (List<string>)value;
                        if (list.Count > 0) continue;
                    }
                    else if (!string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        continue;
                    }
                    
                    _log.Error("{0} : tidak boleh kosong!", prop.Name);
                    result = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
        }

        private static string GetStringFromList(object keys)
        {
            var respons = keys as List<string>;
            if (respons == null) return string.Empty;
            
            // Next(0, 5) = antara 0 sampai 4
            var rand = _random.Next(0, respons.Count);
            
            return respons[rand]
                .ReplaceWithBotValue();
        }

        public static string BadWord()
        {
            return GetStringFromList(Keys.BadWord);
        }

        public static string NoAccessToButton()
        {
            return GetStringFromList(Keys.NoAccessToButton);
        }

        public static string ReplyToOwner()
        {
            return GetStringFromList(Keys.ReplyOwner);
        }

        public static string ReplyToAdmins()
        {
            return GetStringFromList(Keys.ReplyAdmins);
        }

        public static string ReplyToOthers()
        {
            return GetStringFromList(Keys.ReplyOthers);
        }

        public static string SayHello()
        {
            return Keys.SayHello
                .ReplaceWithBotValue();
        }

        public static string WelcomeToGroup()
        {
            return Keys.WelcomeToGroup
                .ReplaceWithBotValue();
        }

        public static string SupportedBy()
        {
            return Keys.SupportedBy
                .ReplaceWithBotValue();
        }

        public static string SimsimiHowToGetToken()
        {
            return Keys.SimsimiHowToGetToken
                .ReplaceWithBotValue();
        }

        public static string SimsimiNullResult()
        {
            return GetStringFromList(Keys.SimsimiNoResponse);
        }

        public static string SimsimiOutOfToken()
        {
            return GetStringFromList(Keys.SimsimiOutOfToken);
        }
    }
}