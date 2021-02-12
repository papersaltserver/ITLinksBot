using Serilog;
using System;
using System.Net;

namespace ItLinksBot
{
    public static class Utils
    {
        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static string UnshortenLink(string linkUrl)
        {
            HttpWebRequest req;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(linkUrl);
            }
            catch (Exception)
            {
                Log.Warning("Malformed URL {url}", linkUrl);
                return linkUrl;
            }
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75";
            req.AllowAutoRedirect = false;
            string realUrl = linkUrl;
            while (true)
            {
                try
                {
                    var resp = (HttpWebResponse)req.GetResponse();
                    if (resp.StatusCode == HttpStatusCode.Ambiguous ||
                    resp.StatusCode == HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == HttpStatusCode.Found ||
                    resp.StatusCode == HttpStatusCode.RedirectMethod ||
                    resp.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        if (!resp.Headers["Location"].Contains("://"))
                        {
                            //var baseRedirUri = new Uri(req.RequestUri.Scheme + "://" + req.RequestUri.Authority);
                            realUrl = (new Uri(req.RequestUri, resp.Headers["Location"])).AbsoluteUri;
                            if (realUrl == req.RequestUri.AbsoluteUri)
                            {
                                break;
                            }
                        }
                        else
                        {
                            realUrl = resp.Headers["Location"];
                            if (realUrl == req.RequestUri.AbsoluteUri)
                            {
                                break;
                            }
                        }
                        req = (HttpWebRequest)WebRequest.Create(realUrl);
                        req.AllowAutoRedirect = false;
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