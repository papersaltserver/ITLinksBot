using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ItLinksBot.Providers
{
    class InsideCryptocurrencyParser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Inside Cryptocurrency";
        readonly Uri baseUri = new Uri("https://inside.com/");

        public InsideCryptocurrencyParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            contentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new List<Digest>();
            var stringResult = contentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//table[contains(@class,'table')]//tr").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode("./td[1]");
                var digestDate = DateTime.Parse(dateNode.InnerText.Trim());
                var linkNode = digestNode.SelectSingleNode("./td[2]/a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //no description there
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
            List<Link> links = new List<Link>();
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'column-content')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var linkNode = link.SelectSingleNode(".//comment()[. = ' STORY FOOTER : START ']/following-sibling::p/a");
                if (linkNode == null)
                {
                    linkNode = link.SelectSingleNode("../div[contains(@class,'column-share')]//p[1]/a");
                }

                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;

                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                var descriptionNodeOriginal = link.SelectSingleNode(".//div[contains(@class,'story-body')]");
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);

                string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                links.Add(new Link
                {
                    URL = href,
                    Title = "", //no separate title
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
