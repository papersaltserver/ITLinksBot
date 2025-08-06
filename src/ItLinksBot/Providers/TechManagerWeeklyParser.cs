using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class TechManagerWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Tech Manager Weekly";
        readonly Uri baseUri = new("https://www.techmanagerweekly.com/");

        public TechManagerWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htlmContentGetter = cg;
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
            var stringResult = htlmContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'post')]/article").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//time");
                var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
                var titleNode = digestNode.SelectSingleNode(".//h2");
                var digestTitle = titleNode.InnerText.Trim();
                var descriptionNode = digestNode.SelectSingleNode(".//div[contains(@class,'feed-excerpt')]");
                var digestDetails = descriptionNode.InnerText.Trim();
                var linkNode = digestNode.SelectSingleNode("./a");
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestTitle,
                    DigestDescription = digestDetails,
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
            var digestContent = htlmContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            HtmlNodeCollection linksInDigest = linksHtml.DocumentNode.SelectNodes("//main/article/.//figure");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode linkNode = linksInDigest[i];

                var titleNode = linkNode.SelectSingleNode(".//div[contains(@class,'kg-bookmark-title')]");
                var title = titleNode.InnerText.Trim();
                var hrefNode = linkNode.SelectSingleNode("./a");
                var href = hrefNode.GetAttributeValue("href", "Not found");
                Uri uriHref = new(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                var descriptionNodeOriginal = linkNode.SelectSingleNode(".//div[contains(@class,'kg-bookmark-description')]");
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);
                string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                var categoryNode = linkNode.SelectSingleNode("./preceding-sibling::h2[1]");
                string categoryText = null;
                if (categoryNode != null)
                {
                    categoryText = categoryNode.InnerText.Trim();
                }
                links.Add(new Link
                {
                    URL = href,
                    Category = categoryText,
                    Title = title,
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
