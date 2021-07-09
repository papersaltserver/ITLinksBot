using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class KubeWeeklyParser: IParser
    {
        public string CurrentProvider => "KubeWeekly";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentGetter<byte[]> binContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("https://www.cncf.io/");

        public KubeWeeklyParser(IContentGetter<string> hcg, IContentGetter<byte[]> bcg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = hcg;
            binContentGetter = bcg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return $"<strong>{digest.DigestName} - {digest.DigestDay:yyyy-MM-dd}</strong>\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            if (link.Medias == null || !link.Medias.Any()) 
            {
                return $"<strong>{link.Title}</strong>\n\n{link.Description}";
            }
            else
            {
                string title="";
                string description = "";
                if(link.Title != null && link.Title != "")
                {
                    title = $"{link.Title}\n\n";
                }
                if (link.Description != null && link.Description != "")
                {
                    description = $"{link.Description}\n\n";
                }
                return $"{title}{description}{link.URL}";
            }
            
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'kubeweekly-box')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var hrefNode = digestNode.SelectSingleNode(".//a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                var digestUrl = new Uri(baseUri, digestHref);
                var fullHref = Utils.UnshortenLink(digestUrl.AbsoluteUri);
                var dateNode = digestNode.SelectSingleNode(".//div[contains(@class,'sent')]");
                var dateText = dateNode.InnerText.Trim();
                var digestDate = DateTime.Parse(dateText, new CultureInfo("en-US", false));

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //no description in Kubeweekly
                    DigestURL = fullHref,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            return digest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            //KubeWeekly has 2 types of information blocks: Tweets and Informaion, we'll analyze them 1 by 1
            //Informational posts
            
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='column-1-1']/table//div[@data-hs-cos-type='rich_text']");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode(".//h2[1]");
                string title = "";
                if(titleNode != null)
                {
                    title = textSanitizer.Sanitize(titleNode.InnerText.Trim());
                }
                
                //HtmlNode hrefNode = link.SelectSingleNode(".//a[1]");
                var href = $"{digest.DigestURL}#section{i}";
                //href = new Uri(baseUri, href).AbsoluteUri;
                //href = Utils.UnshortenLink(href);

                string descriptionText;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(link);
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            int globalLinkCounter = linksInDigest.Count;
            var tweets = linksHtml.DocumentNode.SelectNodes("//div[@id='column-1-1']/div[descendant::a]");
            for(int i = 0; i < tweets.Count; i++)
            {
                HtmlNode tweetNode = tweets[i];
                HtmlNode hrefNode = tweetNode.SelectSingleNode(".//a");
                string href = hrefNode.GetAttributeValue("href", "Not found");

                HtmlNode imgNode = hrefNode.SelectSingleNode(".//img");
                string imgHref = imgNode.GetAttributeValue("src", "Not found");
                string imgName = HttpUtility.UrlDecode(imgHref.Split('/').Last().Split('?').First());
                byte[] imgFile = binContentGetter.GetContent(imgHref);
                var currentImg = new Photo { 
                    ContentBytes = imgFile,
                    FileName = imgName
                };

                links.Add(new Link
                {
                    URL = href,
                    Title = "", //no title for tweets
                    Description = "", //no description for tweets
                    LinkOrder = i+globalLinkCounter,
                    Digest = digest,
                    Medias = new List<Media> { currentImg }
                });

            }
            return links;
        }
    }
}
