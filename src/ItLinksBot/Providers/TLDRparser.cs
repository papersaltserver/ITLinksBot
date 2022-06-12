using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ItLinksBot.Providers
{
    public class TLDRparser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "TLDR";
        readonly Uri baseUri = new("https://tldr.tech/");
        public TLDRparser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>{digest.DigestName}</b>\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            if (link.Category != null)
            {
                return $"<strong>[{link.Category}]{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
            else
            {
                return $"<strong>{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            HtmlNodeCollection digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@id='doc-container']//div[contains(@class,'text-left')]//a");
            var latestIssues = digestsInArchive.OrderByDescending(d => d.InnerText).Take(5);

            foreach (var digestNode in latestIssues)
            {
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                var dateText = Regex.Match(digestNode.InnerText.Trim(), @"^(\d\d\d\d-\d\d-\d\d)").Groups[1].Value;
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(dateText),
                    DigestName = digestNode.InnerText,
                    DigestDescription = "", //tldr doesn't have description for digest itself
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='doc-container']//h3[not(@id='subtitle')]/../..");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./a");
                if (titleNode == null)
                {
                    continue;
                }
                var title = titleNode.InnerText.Trim();
                var href = titleNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descriptionNode = contentNormalizer.NormalizeDom(link.SelectSingleNode("./div"));
                var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                var categoryNode = link.SelectSingleNode("./preceding-sibling::div[strong][1]");
                string categoryText = "";
                if (categoryNode != null)
                {
                    var categoryIconNode = categoryNode.SelectSingleNode("./preceding-sibling::div[1]");
                    categoryText = categoryIconNode.InnerText.Replace("\n", "").Replace("\r", "").Trim() + categoryNode.InnerText.Replace("\n", " ").Replace("\r", "").Trim();
                }
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
