using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TeleBot.BotClass;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public class KeyGenerator
    {
        private Message _message;

        public KeyGenerator(Message message)
        {
            _message = message;
        }
        
        public async void Generate(string data)
        {
            try
            {
                var result = string.Empty;
                using (var csp = new AesCryptoServiceProvider())
                {
                    csp.Mode = CipherMode.CBC;
                    csp.Padding = PaddingMode.PKCS7;
                    var spec = new Rfc2898DeriveBytes(
                        Encoding.UTF8.GetBytes("C0L1-T3RU5-S4MP3-P3G3L"),
                        Encoding.UTF8.GetBytes("CR4CK3R?FUCK-Y0U-KAL14N"),
                        65536);
                    csp.Key = spec.GetBytes(16);
                    csp.IV = Encoding.UTF8.GetBytes("2019032662309102");
                    var encryptor = csp.CreateEncryptor();
                    var inputBuffer = Encoding.UTF8.GetBytes(data);
                    var output = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                    result = Convert.ToBase64String(output);
                }
                await BotClient.SendTextAsync(_message, $"Serial : `{data}`\nKey : `{result}`", parse:ParseMode.Markdown);
            }
            catch (Exception e)
            {
                await BotClient.SendTextAsync(_message, e.Message);
            }
        }
    }
}