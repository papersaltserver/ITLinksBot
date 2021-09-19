using Serilog;
using System;
using System.Linq;
using System.Net;

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
            HttpWebRequest req;
            CookieContainer cookieContainer = new();
            try
            {
                req = (HttpWebRequest)WebRequest.Create(linkUrl);
            }
            catch (Exception)
            {
                Log.Warning("Malformed URL {url}", linkUrl);
                return linkUrl;
            }
            //if current url doesn't need to be unshortened
            if (exceptionList.Contains(req.RequestUri.Host))
            {
                return linkUrl;
            }
            req.CookieContainer = cookieContainer;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75";
            req.AllowAutoRedirect = false;
            string realUrl = linkUrl;
            while (true)
            {
                try
                {
                    if (nonBrowserList.Contains(req.RequestUri.Host))
                    {
                        req.UserAgent = ".NET unshortening client v0.01";
                    }
                    var resp = (HttpWebResponse)req.GetResponse();
                    if (resp.StatusCode == HttpStatusCode.Ambiguous ||
                    resp.StatusCode == HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == HttpStatusCode.Found ||
                    resp.StatusCode == HttpStatusCode.RedirectMethod ||
                    resp.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        if (!resp.Headers["Location"].Contains("://"))
                        {
                            realUrl = (new Uri(req.RequestUri, resp.Headers["Location"])).AbsoluteUri;
                        }
                        else
                        {
                            realUrl = resp.Headers["Location"];
                        }

                        if (realUrl == req.RequestUri.AbsoluteUri)
                        {
                            break;
                        }

                        req = (HttpWebRequest)WebRequest.Create(realUrl);

                        //if current url doesn't need to be unshortened
                        if (exceptionList.Contains(req.RequestUri.Host))
                        {
                            return realUrl;
                        }
                        req.AllowAutoRedirect = false;
                        req.CookieContainer = cookieContainer;
                        req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75";
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Problem {exception} with link {original} which leads to {realUrl} ", e.Message, linkUrl, realUrl);
                    break;
                }

            }
            return realUrl;
        }
    }
}