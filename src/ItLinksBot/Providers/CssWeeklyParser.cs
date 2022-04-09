using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class CssWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "CSS Weekly";
        readonly Uri baseUri = new("https://css-weekly.com/");
        public CssWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
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
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode(".//h3[@class='title']/a");
                var digestUrl = urlNode.GetAttributeValue("href", "Not found");
                var dateNode = digestNode.SelectSingleNode(".//time");
                var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));

                var originalDescriptionNode = HtmlNode.CreateNode("<div></div>");
                originalDescriptionNode.AppendChildren(digestNode.SelectNodes(".//ul//li"));
                var descriptionNode = contentNormalizer.NormalizeDom(originalDescriptionNode);
                var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = urlNode.InnerText.Trim(),
                    DigestDescription = descriptionText,
                    DigestURL = digestUrl,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//article[contains(@class,'newsletter-article')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./header//a");
                var title = titleNode.InnerText.Trim();

                var originalDescriptionNode = HtmlNode.CreateNode("<div></div>");
                originalDescriptionNode.AppendChildren(link.SelectNodes("./p[position()<last()]"));
                var descriptionNode = contentNormalizer.NormalizeDom(originalDescriptionNode);
                var description = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                var href = titleNode.GetAttributeValue("href", "Not found");
                Uri uriHref = new(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = description,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
