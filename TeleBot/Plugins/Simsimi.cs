using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeleBot.BotClient;
using TeleBot.Classes;
using TeleBot.SQLite;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    public class Simsimi
    {
        private static Log _log = new Log("Messages");
        private static Database _db = new Database();
        private Message _message;
        
        private class ResponseModel
        {
            //Result Code
            //100 - OK.
            //400 - Bad Request.
            //401 - Unauthorized | Trial app is expired.
            //404 - Not found.
            //500 - Server Error.
            //509 - Daily Request Query Limit Exceeded.
            
            public string Response { get; set; }
            public string Id { get; set; }
            public int Result { get; set; }
            public string Msg { get; set; }
        }
        
        private static async Task<ResponseModel> GetApiResponse(string key, string text, bool filter = true)
        {
            var ft = filter ? 0 : 1;
            var data = Uri.EscapeDataString(text);
            var apiUrl = $"http://sandbox.api.simsimi.com/request.p?key={key}&lc=id&ft={ft}.0&text={data}";
            
            _log.Debug("Cari jawaban: {0}", text);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(new Uri(apiUrl));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseModel>(content);
            }
        }

        public Simsimi(Message message)
        {
            _message = message;
        }

        public async void SaveToken(string key)
        {
            var verify = Regex.IsMatch(key, @"^\b(\w+-\w+-\w+-\w+-\w+)\b$");
            if (!verify)
            {
                _log.Warning("Token: {0} | Result: Format Salah!", key);
                await Bot.SendTextAsync(_message, $"*Token* : {key}\n*Hasil* : Token Salah!", parse: ParseMode.Markdown);
                return;
            }

            try
            {
                var result = await GetApiResponse(key, "hai, simsimi");
                if (result.Result != 100)
                {
                    _log.Warning("Token: {0} | Result: code {1} - {2}", key, result.Result, result.Msg);
                    await Bot.SendTextAsync(_message, $"*Token* : {key}\n*Hasil* : {result.Result} - {result.Msg}", parse: ParseMode.Markdown);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return;
            }

            try
            {
                var exist = await _db.FindToken(key);
                if (exist != null)
                {
                    _log.Warning("Token: {0} | Result: Exist!", key);
                    await Bot.SendTextAsync(_message, $"*Token* : {key}\n*Hasil* : Sudah ada!", parse: ParseMode.Markdown);
                    return;
                }
                
                var token = new Token()
                {
                    Key = key,
                    Expired = DateTime.Now,
                    LimitExceed = DateTime.Now
                };
                
                await _db.InsertOrReplaceToken(token);
                await Bot.SendTextAsync(_message, "Makasih kaka 😍😘\nSeneng deh dapet token baru..");
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                await Bot.SendTextAsync(_message, $"Error: {ex.Message}");
            }
        }

        public async void SendResponse(string text)
        {
            try
            {
                var tokens = await _db.GetTokens();
                var tokenActive = tokens.Where(t => t.LimitExceed <= DateTime.Now).ToList();

                if (tokenActive.Count == 0)
                {
                    _log.Warning("Token habis!");
                    await Bot.SendTextAsync(_message, "Maaf ya kak...\n" +
                                                      $"{Bot.Name} capek nih, gak ada tenaga buat balesin chat...\n\n" +
                                                      "Bantu isi tenaga pakai /token.\nTengkyu 😘");
                    return;
                }

                foreach (var token in tokenActive)
                {
                    try
                    {
                        var isGodMode = _message.IsGodMode();
                        var result = await GetApiResponse(token.Key, text, isGodMode);
                        
                        // token ok & respon bagus
                        if (result.Result == 100 && !string.IsNullOrWhiteSpace(result.Response))
                        {
                            _log.Debug("Response: {0}", result.Response);
                            break;
                        }
                        
                        // token mati
                        if (result.Result == 401)
                            token.Expired = DateTime.Now;
                        // token melebihi limit
                        else if (result.Result == 509)
                            token.LimitExceed = DateTime.Now.AddHours(24);
                        // kode lain lanjut
                        else
                            continue;
                        
                        // perbaharui token
                        try
                        {
                            await _db.InsertOrReplaceToken(token);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            
            SimsimiNoResponse();
        }

        private async void SimsimiNoResponse()
        {
            var respons = Bot.Keys.SimsimiNoResponse;
            var rand = new Random().Next(0, respons.Count - 1);
            await Bot.SendTextAsync(_message, respons[rand]);
        }
    }
}