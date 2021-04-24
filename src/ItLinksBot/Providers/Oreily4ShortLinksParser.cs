using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ItLinksBot.Providers
{
    class Oreily4ShortLinksParser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "O'Reily Four Short Links";
        readonly Uri baseUri = new("https://www.oreilly.com/");
        public Oreily4ShortLinksParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            contentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = contentGetter.GetContent(provider.DigestURL);
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);
            foreach (var feedItem in feed.Items.Take(50))
            {
                Digest currentDigest = new()
                {
                    DigestDay = feedItem.PublishDate.DateTime,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = feedItem.Summary.Text,
                    DigestURL = feedItem.Links[0].Uri.AbsoluteUri,
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
            var reader = XmlReader.Create(digest.Provider.DigestURL);
            var feed = SyndicationFeed.Load(reader);
            var digestNode = feed.Items.Where(n => n.Title.Text == digest.DigestName && n.Links[0].Uri.AbsoluteUri == digest.DigestURL).SingleOrDefault();
            var feedElementContent = digestNode.ElementExtensions.ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/").FirstOrDefault();
            var htmlLinks = new HtmlDocument();
            htmlLinks.LoadHtml(feedElementContent);
            var listItmesArray = htmlLinks.DocumentNode.Descendants("li").ToArray();
            for (int i = 0; i < listItmesArray.Length; i++)
            {
                HtmlNode listItem = listItmesArray[i];
                var linkTag = listItem.Descendants("a").FirstOrDefault();
                if (linkTag != null)
                {
                    var href = linkTag.GetAttributeValue("href", "Not found");
                    if (!href.Contains("://") && href.Contains("/"))
                    {
                        href = (new Uri(baseUri, href)).AbsoluteUri;
                    }
                    href = Utils.UnshortenLink(href);

                    var descriptionNode = contentNormalizer.NormalizeDom(listItem);
                    var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                    links.Add(new Link
                    {
                        URL = href,
                        Title = linkTag.InnerText,
                        Description = descriptionText,
                        LinkOrder = i,
                        Digest = digest
                    });
                }
            }
            return links;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}\n{2}", digest.DigestName, digest.DigestDescription, digest.DigestURL);
        }
        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }
    }
}
