using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TeleBot.Classes
{
    public class ConfigKeys
    {
        public string Token { get; set; }
        public string Alias { get; set; }
        public string AliasExcept { get; set; }
        public string BadWords { get; set; }
        public List<string> BadWordResponse { get; set; }
        public string SupportedBy { get; set; }
        public long OwnerId { get; set; }
        public List<long> AdminIds { get; set; }
        public long GroupId { get; set; }
        public List<string> ReplyOwner { get; set; }
        public List<string> ReplyAdmins { get; set; }
        public List<string> ReplyOthers { get; set; }
        public string SayHello { get; set; }
        public string SayHelloNewMember { get; set; }
        public List<string> SimsimiNoResponse { get; set; }
        public string HowToGetToken { get; set; }
    }
    
    public static class Configs
    {
        private static Log _log = new Log("Configs");
        private static readonly string ConfigFile = Program.FilePathInData("Configs.json");

        public static ConfigKeys LoadKeys()
        {
            try
            {
                var configsRaw = File.ReadAllText(ConfigFile);
                var json = JsonConvert.DeserializeObject<ConfigKeys>(configsRaw);
                
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