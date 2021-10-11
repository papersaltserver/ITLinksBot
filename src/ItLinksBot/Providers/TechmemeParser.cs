using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ItLinksBot.Providers
{
    class TechmemeParser : IParser
    {
        public string CurrentProvider => "Techmeme";
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("https://us14.campaign-archive.com/");

        public TechmemeParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htlmContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return $"<b>{digest.DigestName} - {digest.DigestDay.ToString("yyyy-MM-dd")}</b>\n{digest.DigestDescription}\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            return $"<strong>[{link.Category}]{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htlmContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//li[contains(@class,'campaign')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.ChildNodes[0];
                string dateText = dateNode.InnerText.Split('-')[0].Trim();
                var digestDate = DateTime.Parse(dateText, new CultureInfo("en-US", false));
                var hrefNode = digestNode.SelectSingleNode("./a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                var fullHref = Utils.UnshortenLink(digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //Techmeme doesn't provide one
                    DigestURL = fullHref,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'section')]/div[contains(@class,'story') and not(contains(@class,'sponsor'))]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode(".//td/span[contains(@class,'title')][1]");
                var title = titleNode.InnerText;
                var href = titleNode.SelectSingleNode("./a").GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = new Uri(baseUri, href).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var contentNodes = link.SelectNodes("./table[not(contains(@class,'leading_item'))]");
                string descriptionText;
                if (contentNodes != null)
                {
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChildren(contentNodes);
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                }
                else
                {
                    descriptionText = "";
                }

                var categoryNode = link.SelectSingleNode("./preceding-sibling::div[contains(@class,'section_header')][1]");
                string categoryText = categoryNode.InnerText.ToUpper().Replace("\n", " ").Replace("\r", "").Trim();

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Category = categoryText,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
