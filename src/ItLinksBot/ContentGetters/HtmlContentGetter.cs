using Serilog;
using System;
using System.Net.Http;

namespace ItLinksBot.ContentGetters
{
    class HtmlContentGetter : IContentGetter<string>
    {
        //public string ContentType => "html";
        public string GetContent(string resourceUrl)
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");
                var archiveContent = httpClient.GetAsync(resourceUrl).Result;
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
