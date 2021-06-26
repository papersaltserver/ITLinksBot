using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ItLinksBot.Providers
{
    class ReactNewsletterParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "React Newsletter";
        readonly Uri baseUri = new("https://reactnewsletter.com/");
        public ReactNewsletterParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htlmContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}\n{2}", digest.DigestName, digest.DigestDescription, digest.DigestURL);
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
            var allDigestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'masonry')]//li/a");
            var digestsInArchive = allDigestsInArchive.OrderByDescending(l => DateTime.Parse(l.SelectSingleNode(".//p[contains(@class,'text-gray')]").InnerText, new CultureInfo("en-US", false))).Take(10);
            foreach (var digestNode in digestsInArchive)
            {
                //var relativePathNode = digestNode.SelectSingleNode(".//a");
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                var digestDate = DateTime.Parse(digestNode.SelectSingleNode(".//p[contains(@class,'text-gray')]").InnerText, new CultureInfo("en-US", false));
                var digestDescriptionNode = contentNormalizer.NormalizeDom(digestNode.SelectSingleNode("./p"));
                var digestDescriptionText = textSanitizer.Sanitize(digestDescriptionNode.InnerHtml.Trim());
                var digestName = digestNode.SelectSingleNode(".//p[contains(@class,'text-xl')]").InnerText;
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = digestDescriptionText,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'Content_container')]/div/h3");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.InnerText;
                var href = link.SelectSingleNode(".//a")?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper() != "H2" && sibling.Name.ToUpper() != "HR")
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    sibling = sibling.NextSibling;
                }

                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
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
