using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ItLinksBot.ContentGetters
{
    class HtmlContentGetter : IContentGetter<string>
    {
        internal const string BrowserUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Safari/537.36 Edg/157.0.0.0";
        private const int MaxLoggedBodyLength = 8192;
        private static readonly Dictionary<string, string> BrowserRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["Cache-Control"] = "no-cache",
            ["Pragma"] = "no-cache",
            ["Priority"] = "u=0, i",
            ["Sec-CH-UA"] = "\"Not-A.Brand\";v=\"99\", \"Microsoft Edge\";v=\"157\", \"Chromium\";v=\"157\"",
            ["Sec-CH-UA-Mobile"] = "?0",
            ["Sec-CH-UA-Platform"] = "\"macOS\"",
            ["Sec-Fetch-Dest"] = "document",
            ["Sec-Fetch-Mode"] = "navigate",
            ["Sec-Fetch-Site"] = "none",
            ["Sec-Fetch-User"] = "?1",
            ["Upgrade-Insecure-Requests"] = "1",
            ["User-Agent"] = BrowserUserAgent
        };
        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "Api-Key"
        };

        public string GetContent(string resourceUrl, Dictionary<string, string> requestHeaders = null)
        {
            try
            {
                using var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                };
                using HttpClient httpClient = new(handler)
                {
                    DefaultRequestVersion = HttpVersion.Version20,
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                };
                foreach (KeyValuePair<string, string> hdr in BrowserRequestHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(hdr.Key, hdr.Value);
                }
                if (requestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> hdr in requestHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Remove(hdr.Key);
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
                header => SensitiveHeaders.Contains(header.Key) ? "[REDACTED]" :
                    string.Join(header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ? " " : ", ", header.Value));
        }
    }
}
