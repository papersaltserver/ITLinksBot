using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ItLinksBot.Providers
{
    class SREWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "SRE Weekly";
        readonly Uri baseUri = new("https://sreweekly.com/");

        public SREWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}\n{3}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);
            foreach (var feedItem in feed.Items.Take(5))
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
            var digestNode = feed.Items.SingleOrDefault(n => n.Title.Text == digest.DigestName && n.Links[0].Uri.AbsoluteUri == digest.DigestURL);
            var feedElementContent = digestNode.ElementExtensions.ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/").FirstOrDefault();
            var htmlLinks = new HtmlDocument();
            htmlLinks.LoadHtml(feedElementContent);
            var listItemsArray = htmlLinks.DocumentNode.SelectNodes("//div[contains(@class,'sreweekly-entry')]");
            for (int i = 0; i < listItemsArray.Count; i++)
            {
                HtmlNode listItem = listItemsArray[i];
                HtmlNode linkTag = listItem.SelectSingleNode(".//div[contains(@class,'sreweekly-title')]//a");
                if (linkTag != null)
                {
                    var href = linkTag.GetAttributeValue("href", "Not found");
                    if (!href.Contains("://") && href.Contains('/'))
                    {
                        href = new Uri(baseUri, href).AbsoluteUri;
                    }
                    href = Utils.UnshortenLink(href);

                    var descriptionNode = listItem.SelectSingleNode(".//div[contains(@class,'sreweekly-description')]");
                    var normalizedDescriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    var descriptionText = textSanitizer.Sanitize(normalizedDescriptionNode.InnerHtml.Trim());

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
    }
}
