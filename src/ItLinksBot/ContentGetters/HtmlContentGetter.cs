using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ItLinksBot.ContentGetters
{
    class HtmlContentGetter : IContentGetter<string>
    {
        private const int MaxLoggedBodyLength = 8192;
        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "Api-Key"
        };

        public string GetContent(string resourceUrl, Dictionary<string, string> requestHeaders = null)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36 Edg/139.0.0.0");
                if (requestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> hdr in requestHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(hdr.Key, hdr.Value);
                    }
                }
                using var archiveContent = httpClient.GetAsync(resourceUrl).Result;
                var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
                if (!archiveContent.IsSuccessStatusCode)
                {
                    var bodyPreview = stringResult.Length > MaxLoggedBodyLength
                        ? stringResult.Substring(0, MaxLoggedBodyLength)
                        : stringResult;

                    Log.Warning(
                        "Url \"{url}\" returned error [{statusCode} {reasonPhrase}], HTTP/{httpVersion}. " +
                        "Final URL: {finalUrl}\n" +
                        "Request headers: {@requestHeaders}\n" +
                        "Response headers: {@responseHeaders}\n" +
                        "Content headers: {@contentHeaders}\n" +
                        "Response body ({bodyLength} characters, truncated: {bodyTruncated}):\n{responseBody}",
                        resourceUrl, (int)archiveContent.StatusCode, archiveContent.ReasonPhrase, archiveContent.Version,
                        archiveContent.RequestMessage?.RequestUri,
                        GetHeadersForLogging(archiveContent.RequestMessage?.Headers ?? httpClient.DefaultRequestHeaders),
                        GetHeadersForLogging(archiveContent.Headers),
                        GetHeadersForLogging(archiveContent.Content.Headers),
                        stringResult.Length, stringResult.Length > MaxLoggedBodyLength, bodyPreview);
                }
                return stringResult;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Problem with getting {resourceUrl}", resourceUrl);
                return "";
            }
        }

        private static Dictionary<string, string> GetHeadersForLogging(HttpHeaders headers)
        {
            return headers.ToDictionary(
                header => header.Key,
                header => SensitiveHeaders.Contains(header.Key) ? "[REDACTED]" : string.Join(", ", header.Value));
        }
    }
}
