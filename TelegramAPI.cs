using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Caching;
using System.Linq;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration.FileExtensions;
//using Microsoft.Extensions.Configuration.Json;

namespace ItLinksBot
{
    public class TelegramAPI
    {
        readonly string _botKey;
        public TelegramAPI(string BotKey)
        {
            _botKey = BotKey;
        }
        /// <summary>
        /// Sends a message to a specified channel throuth Telegram Bot Api
        /// </summary>
        /// <param name="Channel">Telegram channel name in format "@channel" to send mesage to</param>
        /// <param name="message">Html formatted string to be posted to chosen channel</param>
        /// <returns></returns>
        public string SendMessage(string Channel, string message)
        {
            Throttle();
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

        public string UpdateMessage(string channel, int messageId, string newMessage)
        {
            Throttle();
            string urlString = "https://api.telegram.org/bot{0}/editMessageText";
            urlString = String.Format(urlString, _botKey);
            HttpClient httpClient = new HttpClient();
            Dictionary<string, string> requestBody = new Dictionary<string, string>
            {
                { "chat_id", channel },
                { "parse_mode", "HTML" },
                { "message_id", messageId.ToString() },
                { "text", newMessage }
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

        /// <summary>
        /// Checks if we've achived Telegram's rate limit and waits if limit achieved, 
        /// before to send another request
        /// </summary>
        private static void Throttle()
        {
            var maxPerPeriod = 20;
            var intervalPeriod = 1*60*1000;//5 minutes
            var sleepInterval = 5000;//period to "sleep" before trying again (if the limits have been reached)
            var recentTransactions = MemoryCache.Default.Count();
            while (recentTransactions >= maxPerPeriod)
            {
                System.Threading.Thread.Sleep(sleepInterval);
                recentTransactions = MemoryCache.Default.Count();
            }
            var key = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmm");
            var existing = MemoryCache.Default.Where(x => x.Key.StartsWith(key));
            if (existing != null && existing.Any())
            {
                var counter = 2;
                var last = existing.OrderBy(x => x.Key).Last();
                var pieces = last.Key.Split('_');
                if (pieces.Length > 1)
                {
                    if (int.TryParse(pieces[1], out int lastCount))
                    {
                        counter = lastCount + 1;
                    }
                }
                key = key + "_" + counter;
            }
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(intervalPeriod)
            };
            MemoryCache.Default.Set(key, 1, policy);
        }
    }
}
