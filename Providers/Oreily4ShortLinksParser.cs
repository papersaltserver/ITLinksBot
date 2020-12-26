using ItLinksBot.Models;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Collections.Generic;
//using Microsoft.Extensions.Configuration.FileExtensions;
//using Microsoft.Extensions.Configuration.Json;

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
            //digests = new List<Digest>();
            //links = new List<Link>();
            foreach (var feedItem in feed.Items.Take(50))
            {
                //var feedElementContent = feedItem.ElementExtensions.ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/").FirstOrDefault();
                Digest currentDigest = new Digest
                {
                    DigestDay = feedItem.PublishDate.DateTime,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = feedItem.Summary.Text,
                    DigestURL = feedItem.Links[0].Uri.AbsoluteUri,
                    Provider = _oreilyProvider
                };
                digests.Add(currentDigest);

                /*var htmlLinks = new HtmlDocument();
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
                            Title = linkTag.InnerText,
                            Description = HttpUtility.HtmlDecode(listItem.InnerText),
                            Digest = currentDigest
                        });
                    }
                }*/
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
                    links.Add(new Link
                    {
                        URL = linkTag.GetAttributeValue("href", "Not found"),
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
