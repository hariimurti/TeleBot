using System;
using System.Text.RegularExpressions;
using TeleBot.BotClass;
using TeleBot.Classes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBot.Plugins
{
    class Eset
    {
        private static Log _log = new Log("Eset");
        private const string apiUrl = "https://32fornod.com";
        private Message _message;

        public Eset(Message message)
        {
            _message = message;
        }

        public async void GetKeys()
        {
            _message = await BotClient.SendTextAsync(_message, "Tunggu sebentar ya...", true);

            try
            {
                _log.Debug("Mengakses {0}", apiUrl);

                var request = new WebRequest
                {
                    Url = apiUrl,
                    Method = WebMethod.Get
                };
                var response = await WebClient.GetOrPostStringAsync(request);

                _log.Debug("Parsing key eset...");

                var result = string.Empty;
                var found = false;

                // regex table
                var mTable = Regex.Matches(response, @"(<thead[\s\S]*?</tbody>)");
                _log.Debug("Found : {0} Product", mTable.Count);
                foreach (Match table in mTable)
                {
                    // regex product
                    var rName = Regex.Match(table.Groups[1].Value, "<h3>(.*)</h3>");
                    if (!rName.Success) continue;

                    _log.Debug("Product : {0}", rName.Groups[1].Value);
                    result += string.Format("<b>{0}</b>\n", rName.Groups[1].Value);

                    // regex key
                    var mRow = Regex.Matches(table.Groups[1].Value, @"(<tr>[\s\S]*?</tr>)");
                    foreach (Match row in mRow)
                    {
                        var rKey = Regex.Match(row.Groups[1].Value, @"td.*>(.*)</td.*\s<td.*>(.*)</td(?:.*\s<td.*>(.*)</td)?");
                        if (!rKey.Success) continue;

                        _log.Debug("Key : {0}", rKey.Groups[1].Value);

                        found = true;
                        var key = string.Empty;

                        if (string.IsNullOrWhiteSpace(rKey.Groups[3].Value))
                            key = string.Format("<code>{0}</code> | {1}", rKey.Groups[1].Value, rKey.Groups[2].Value);
                        else
                            key = string.Format("<code>{0} : {1}</code> | {2}", rKey.Groups[1].Value, rKey.Groups[2].Value, rKey.Groups[3].Value);

                        result += string.Format("» {0}\n", key);
                    }
                    result += "\n";
                }

                // footer
                result += "—— —— —— —— —— ——\n";

                // regex title
                var rTitle = Regex.Match(response, "<h2.*title.*>(.*)</h2>");
                if (rTitle.Success)
                {
                    result += rTitle.Groups[1].Value + Environment.NewLine;
                }

                // footer end
                result += "Compatibility keys: <code>ESSP</code> > <code>EIS</code> > <code>ESS</code> > <code>EAV</code>.\n" +
                    "<i>This means that the EIS key is suitable for ESS and EAV. ESSP is a universal key.</i>";

                if (!found) result = "Mohon maaf... Key tidak tersedia!";

                await BotClient.EditOrSendTextAsync(_message, _message.MessageId, result, ParseMode.Html);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);

                await BotClient.EditOrSendTextAsync(_message, _message.MessageId,
                    "Mohon maaf...\nPlugin eset saat ini sedang mengalami gangguan!");

                return;
            }
        }
    }
}
