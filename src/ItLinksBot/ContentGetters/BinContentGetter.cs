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
        public byte[] GetContent(string resourceUrl)
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");
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
