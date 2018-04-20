﻿using System;
using System.IO;
using Newtonsoft.Json;
using TeleBot.BotClient;

namespace TeleBot.Classes
{
    public static class Configs
    {
        private static Log _log = new Log("Configs");
        private static readonly string ConfigFile = Program.FilePathInData("Configs.json");
        private static readonly string ConfigFileExample = Program.FilePathInWorkingDir("Configs-Example.json");
        private static BotKeys _keys;

        public static BotKeys LoadKeys()
        {
            try
            {
                if (_keys != null) return _keys;

                if (!File.Exists(ConfigFile) && File.Exists(ConfigFileExample))
                {
                    _log.Debug("Menyalin file contoh configurasi...");
                    File.Copy(ConfigFileExample, ConfigFile);
                }
                
                // buka file Configs.json
                var configsRaw = File.ReadAllText(ConfigFile);
                
                // parsing ke BotKeys
                _keys = JsonConvert.DeserializeObject<BotKeys>(configsRaw);
                
                _log.Debug("Sukses membaca pengaturan...");
                
                return _keys;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return new BotKeys();
            }
        }
    }
}