using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
//using Microsoft.Extensions.Configuration.FileExtensions;
//using Microsoft.Extensions.Configuration.Json;

namespace ItLinksBot
{
    class TelegramAPI
    {
        readonly string _botKey;
        public TelegramAPI(string BotKey)
        {
            _botKey = BotKey;
        }
        public string SendMessage(string Channel, string message)
        {
            string urlString = "https://api.telegram.org/bot{0}/sendMessage";
            urlString = String.Format(urlString, _botKey);
            HttpClient httpClient = new HttpClient();
            Dictionary<string, string> requestBody = new Dictionary<string, string>
            {
                { "chat_id", Channel },
                { "parse_mode", "HTML" },
                { "text", message }
            };


            string json = JsonConvert.SerializeObject(requestBody, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = httpClient.PostAsync(urlString, content).Result;
            return resp.Content.ReadAsStringAsync().Result;
        }
    }
}
