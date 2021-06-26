using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class DataIsPluralParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Data Is Plural";
        readonly Uri baseUri = new("https://www.data-is-plural.com/");

        public DataIsPluralParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//li[contains(@class,'edition')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode("./span[contains(@class,'edition-date')]");
                var dateTimeNode = dateNode.SelectSingleNode(".//time");
                var dateTimeText = dateTimeNode.GetAttributeValue("datetime", "not found");
                var digestDate = DateTime.Parse(dateTimeText.Replace(" UTC", "Z"));
                var linkNode = dateNode.SelectSingleNode("./a");
                var digestNameNode = digestNode.SelectSingleNode("./span[contains(@class,'edition-summary')]");
                var digestName = digestNameNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);
                //var descriptionNode = digestNode.SelectSingleNode("./p[contains(@class,'message-snippet')]");
                //var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "",
                    DigestURL = digestUrl.AbsoluteUri,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'edition-body')]//p");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = ""; //this digest doesn't have separate header
                var href = link.SelectSingleNode("./a[1]")?.GetAttributeValue("href", "Not found");
                if (href == null)
                {
                    Log.Warning("Data is Plural digest {digestUrl} has a paragraph without link {p}",digest.DigestURL, link.InnerText);
                    continue;
                }
                /*if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }*/
                var linkUrl = new Uri(baseUri, href);
                href = Utils.UnshortenLink(linkUrl.AbsoluteUri);

                var descriptionNode = contentNormalizer.NormalizeDom(link);
                var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
