using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace ItLinksBot.ContentGetters
{
    class HtmlContentGetter : IContentGetter<string>
    {
        public string GetContent(string resourceUrl, Dictionary<string, string> requestHeaders = null)
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");
                if (requestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> hdr in requestHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(hdr.Key, hdr.Value);
                    }
                }
                var archiveContent = httpClient.GetAsync(resourceUrl).Result;
                if (!archiveContent.IsSuccessStatusCode)
                {
                    Log.Warning("Url \"{url}\" returned error [{errorId}] \"{error}\"", resourceUrl, archiveContent.StatusCode.ToString(), archiveContent.Content);
                }
                var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
                return stringResult;
            }
            catch (Exception e)
            {
                Log.Warning("Problem {exception} with getting {resourceUrl}", e.Message, resourceUrl);
                return "";
            }
        }
    }
}
