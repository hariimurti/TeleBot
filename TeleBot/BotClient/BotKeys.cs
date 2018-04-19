using System.Collections.Generic;

namespace TeleBot.BotClient
{
    public class BotKeys
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
}