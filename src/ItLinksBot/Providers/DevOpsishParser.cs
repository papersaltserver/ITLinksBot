using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class DevOpsishParser : IParser
    {
        public string CurrentProvider => "DevOps'ish";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("https://devopsish.com");

        public DevOpsishParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return $"<b>{digest.DigestName} - {digest.DigestDay.ToString("yyyy-MM-dd")}</b>\n{digest.DigestDescription}\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            return link.Description;
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article[contains(@class,'post-entry')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var hrefNode = digestNode.SelectSingleNode("./a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var titleNode = digestNode.SelectSingleNode(".//h2[contains(@class,'entry-hint-parent')]");
                var digestName = titleNode.InnerText.Trim();
                var digestUrl = new Uri(baseUri, digestHref);
                var fullHref = Utils.UnshortenLink(digestUrl.AbsoluteUri);
                var digestDate = new DateTime(1900, 1, 1); //we'll fill it later

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,//we'll fill it l
                    DigestName = digestName,
                    DigestDescription = "", //we'll fill it later
                    DigestURL = fullHref,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            string digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);
            HtmlNodeCollection digestDescription = digestDocument.DocumentNode.SelectNodes("//section[contains(@class,'post-description')]");
            HtmlNode descriptionNode = HtmlNode.CreateNode("<div></div>");
            string descriptionText = "";
            if (digestDescription != null)
            {
                foreach (HtmlNode digestParagraph in digestDescription)
                {
                    descriptionNode.AppendChild(digestParagraph.Clone());
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
            }

            var dateNode = digestDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'post-meta')]/span[1]");
            string dateText = dateNode.InnerText.Trim();
            var digestDate = DateTime.Parse(dateText);

            digest.DigestDay = digestDate;
            digest.DigestDescription = descriptionText;

            return digest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'post-content')]/p");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var descriptionNode = contentNormalizer.NormalizeDom(link);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = $"{digest.DigestURL}#section{i}", // we'll not be saving real links in sake of simplicity
                    Title = "",
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
