using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.ContentGetters
{
    class BinContentGetter : IContentGetter<byte[]>
    {
        public byte[] GetContent(string resourceUrl, Dictionary<string, string> requestHeaders = null)
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
                var binResult = archiveContent.Content.ReadAsByteArrayAsync().Result;
                return binResult;
            }
            catch (Exception e)
            {
                Log.Warning("Problem {exception} with getting {resourceUrl}", e.Message, resourceUrl);
                return Array.Empty<byte>();
            }
        }
    }
}
