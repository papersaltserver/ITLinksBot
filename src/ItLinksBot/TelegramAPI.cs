using ItLinksBot.DTO;
using ItLinksBot.TelegramDTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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
            string urlString = "https://api.telegram.org/bot{0}/sendMessage";
            urlString = string.Format(urlString, _botKey);
            HttpClient httpClient = new();
            Dictionary<string, string> requestBody = new()
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
            StringContent content = new(json, Encoding.UTF8, "application/json");
            var resp = httpClient.PostAsync(urlString, content).Result;
            //dumb Telegram API doesn't guarantee message order when it is sent in batches, 1s should be enough to complete posting
            System.Threading.Thread.Sleep(1000);
            return resp.Content.ReadAsStringAsync().Result;
        }

        public string SendMediaGroup(string Channel, ITelegramMedia[] telegramMedias, IMediaDTO[] files)
        {
            string urlString = "https://api.telegram.org/bot{0}/sendMediaGroup";
            urlString = string.Format(urlString, _botKey);

            MultipartFormDataContent form = new()
            {
                { new StringContent(Channel, Encoding.UTF8), "chat_id" }
            };

            string jsonMedia = JsonConvert.SerializeObject(
                telegramMedias,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            form.Add(new StringContent(jsonMedia, Encoding.UTF8), "media");
            foreach (var file in files)
            {
                form.Add(new ByteArrayContent(file.ContentBytes), file.FileName, file.FileName);
            }
            HttpClient httpClient = new();
            var resp = httpClient.PostAsync(urlString, form).Result;
            //dumb Telegram API doesn't guarantee message order when it is sent in batches, 1s should be enough to complete posting
            System.Threading.Thread.Sleep(1000);
            return resp.Content.ReadAsStringAsync().Result;
        }

        public string UpdateMessage(string channel, int messageId, string newMessage)
        {
            string urlString = "https://api.telegram.org/bot{0}/editMessageText";
            urlString = string.Format(urlString, _botKey);
            HttpClient httpClient = new();
            Dictionary<string, string> requestBody = new()
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
            StringContent content = new(json, Encoding.UTF8, "application/json");
            var resp = httpClient.PostAsync(urlString, content).Result;
            return resp.Content.ReadAsStringAsync().Result;
        }

        public string GetChat(string chatId)
        {
            string urlString = "https://api.telegram.org/bot{0}/getChat";
            urlString = string.Format(urlString, _botKey);
            HttpClient httpClient = new();
            Dictionary<string, string> requestBody = new()
            {
                { "chat_id", chatId }
            };
            string json = JsonConvert.SerializeObject(requestBody, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
            StringContent content = new(json, Encoding.UTF8, "application/json");
            var resp = httpClient.PostAsync(urlString, content).Result.Content.ReadAsStringAsync().Result;

            return resp;
        }
    }
}
