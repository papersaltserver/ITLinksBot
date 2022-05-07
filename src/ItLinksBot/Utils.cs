using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace ItLinksBot
{
    public static class Utils
    {
        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static string UnshortenLink(string linkUrl)
        {
            string[] exceptionList = new string[] { "techcrunch.com", "www.bloomberg.com", "www.washingtonpost.com", "www.youtube.com" };
            string[] nonBrowserList = new string[] { "t.co" };
            HttpClientHandler handler = new();
            handler.AllowAutoRedirect = false;
            HttpRequestMessage req;
            Uri requestUri;
            try
            {
                requestUri = new(linkUrl);
            }
            catch (Exception)
            {
                Log.Warning("Malformed URL {url}", linkUrl);
                return linkUrl;
            }
            //if current url doesn't need to be unshortened
            if (exceptionList.Contains(requestUri.Host))
            {
                return linkUrl;
            }
            HttpClient httpClient = new(handler);
            while (true)
            {
                try
                {
                    req = new(HttpMethod.Get, requestUri);
                    if (nonBrowserList.Contains(requestUri.Host))
                    {
                        req.Headers.Add("User-Agent", ".NET unshortening client v0.01");
                    }
                    else
                    {
                        req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");
                    }
                    var resp = httpClient.Send(req);
                    if (resp.StatusCode == HttpStatusCode.Ambiguous ||
                    resp.StatusCode == HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == HttpStatusCode.Found ||
                    resp.StatusCode == HttpStatusCode.RedirectMethod ||
                    resp.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        if (!resp.Headers.Location.AbsoluteUri.Contains("://"))
                        {
                            requestUri = new Uri(requestUri, resp.Headers.Location);
                        }
                        else
                        {
                            requestUri = resp.Headers.Location;
                        }

                        if (requestUri.AbsoluteUri == req.RequestUri.AbsoluteUri)
                        {
                            break;
                        }

                        //if current url doesn't need to be unshortened
                        if (exceptionList.Contains(req.RequestUri.Host))
                        {
                            return requestUri.AbsoluteUri;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Problem {exception} with link {original} which leads to {realUrl} ", e.Message, linkUrl, requestUri.AbsoluteUri);
                    break;
                }

            }
            return requestUri.AbsoluteUri;
        }
    }
}