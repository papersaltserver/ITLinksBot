using ItLinksBot.Models;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Collections.Generic;
using System;

namespace ItLinksBot.Providers
{
    class Oreily4ShortLinksParser : IParser
    {
        readonly Provider _oreilyProvider;
        public Oreily4ShortLinksParser(Provider provider)
        {
            _oreilyProvider = provider;
        }
        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            var reader = XmlReader.Create(_oreilyProvider.DigestURL);
            var feed = SyndicationFeed.Load(reader);
            foreach (var feedItem in feed.Items.Take(50))
            {
                Digest currentDigest = new Digest
                {
                    DigestDay = feedItem.PublishDate.DateTime,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = feedItem.Summary.Text,
                    DigestURL = feedItem.Links[0].Uri.AbsoluteUri,
                    Provider = _oreilyProvider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }
        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new List<Link>();
            var reader = XmlReader.Create(_oreilyProvider.DigestURL);
            var feed = SyndicationFeed.Load(reader);
            var digestNode = feed.Items.Where(n => n.Title.Text == digest.DigestName && n.Links[0].Uri.AbsoluteUri == digest.DigestURL).SingleOrDefault();
            var feedElementContent = digestNode.ElementExtensions.ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/").FirstOrDefault();
            var htmlLinks = new HtmlDocument();
            htmlLinks.LoadHtml(feedElementContent);
            var listItmesArray = htmlLinks.DocumentNode.Descendants("li");
            foreach (var listItem in listItmesArray)
            {
                var linkTag = listItem.Descendants("a").FirstOrDefault();
                if (linkTag != null)
                {
                    var href = linkTag.GetAttributeValue("href", "Not found");
                    if (!href.Contains("://") && href.Contains("/"))
                    {
                        var digestUrl = new Uri(digest.DigestURL);
                        var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                        href = (new Uri(digestBase, href)).AbsoluteUri;
                    }
                    href = Utils.UnshortenLink(href);

                    links.Add(new Link
                    {
                        URL = href,
                        Title = linkTag.InnerText,
                        Description = HttpUtility.HtmlDecode(listItem.InnerText),
                        Digest = digest
                    });
                }
            }
            return links;
        }
        public string FormatDigestPost(Digest digest) {
            return string.Format("<b>{0}</b>\n{1}\n{2}",digest.DigestName,digest.DigestDescription,digest.DigestURL);
        }
        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }
    }
}
