using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TeleBot.Classes;

namespace TeleBot.BotClass
{
    public static class Bot
    {
        private static Log _log = new Log("BotConfigs");
        private static readonly string JsonFile = Program.FilePathInData("Bot.json");
        public static BotKeys Keys;

        public static string Name = string.Empty;
        public static string Username = string.Empty;

        public class BotKeys
        {
            public string Token { get; set; }
            public string Alias { get; set; }
            public string AliasExcept { get; set; }
            public string BadWords { get; set; }
            public long OwnerId { get; set; }
            public List<long> AdminIds { get; set; }
            public long GroupId { get; set; }
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
                    var newKeys = new BotKeys
                    {
                        Token = string.Empty,
                        Alias = string.Empty,
                        AliasExcept = string.Empty,
                        BadWords = string.Empty,
                        AdminIds = new List<long>(),
                        GroupId = 0
                    };
                    jsonObject = JsonConvert.SerializeObject(newKeys, Program.JsonSettings);

                    File.WriteAllText(JsonFile, jsonObject, Encoding.UTF8);

                    _log.Error("Silahkan isi pengaturan: {0}", Path.GetFileName(JsonFile));

                    return false;
                }

                _log.Debug("Membaca pengaturan: {0}", Path.GetFileName(JsonFile));

                // buka file json
                jsonObject = File.ReadAllText(JsonFile);

                // parsing ke BotKeys
                Keys = JsonConvert.DeserializeObject<BotKeys>(jsonObject);

                // string dan list tidak boleh kosong
                var result = true;
                foreach (var prop in typeof(BotKeys).GetProperties())
                {
                    var value = prop.GetValue(Keys, null);
                    if (prop.PropertyType == typeof(List<string>))
                    {
                        var list = (List<string>) value;
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
    }
}