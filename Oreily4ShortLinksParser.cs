using ItLinksBot.Models;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Collections.Generic;
//using Microsoft.Extensions.Configuration.FileExtensions;
//using Microsoft.Extensions.Configuration.Json;

namespace ItLinksBot
{
    class Oreily4ShortLinksParser : IParser
    {
        readonly Provider _oreilyProvider;
        public Oreily4ShortLinksParser(Provider provider)
        {
            _oreilyProvider = provider;
        }
        public void GetCurrentDigests(out List<Digest> digests, out List<Link> links)
        {
            var reader = XmlReader.Create(_oreilyProvider.DigestURL);
            var feed = SyndicationFeed.Load(reader);
            digests = new List<Digest>();
            links = new List<Link>();
            foreach (var feedItem in feed.Items)
            {
                var feedElementContent = feedItem.ElementExtensions.ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/").FirstOrDefault();
                Digest currentDigest = new Digest
                {
                    DigestDay = feedItem.PublishDate.DateTime,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = feedItem.Summary.Text,
                    DigestURL = feedItem.Links[0].Uri.AbsoluteUri,
                    Provider = _oreilyProvider
                };
                digests.Add(currentDigest);

                var htmlLinks = new HtmlDocument();
                htmlLinks.LoadHtml(feedElementContent);
                var listItmesArray = htmlLinks.DocumentNode.Descendants("li");
                foreach (var listItem in listItmesArray)
                {
                    var linkTag = listItem.Descendants("a").FirstOrDefault();
                    if (linkTag != null)
                    {
                        links.Add(new Link
                        {
                            URL = linkTag.GetAttributeValue("href", "Not found"),
                            Description = HttpUtility.HtmlDecode(listItem.InnerText),
                            Digest = currentDigest
                        });
                    }
                }
            }
        }
        public string FormatDigestPost(Digest digest) {
            return string.Format("{0}\n{1}\n{2}",digest.DigestName,digest.DigestDescription,digest.DigestURL);
        }
        public string FormatLinkPost(Link link)
        {
            return string.Format("{0}\n{1}", link.Description, link.URL);
        }
    }
}
