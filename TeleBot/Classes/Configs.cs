using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TeleBot.BotClient;

namespace TeleBot.Classes
{
    public static class Configs
    {
        private static Log _log = new Log("Configs");
        private static readonly string ConfigFile = Program.FilePathInData("Configs.json");

        public static BotKeys LoadKeys()
        {
            try
            {
                var configsRaw = File.ReadAllText(ConfigFile);
                var json = JsonConvert.DeserializeObject<BotKeys>(configsRaw);
                
                _log.Debug("Sukses membaca pengaturan...");
                
                return json;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return null;
            }
        }
    }
}