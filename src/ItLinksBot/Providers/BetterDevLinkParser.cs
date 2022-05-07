using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class BetterDevLinkParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Better Dev Link";
        readonly Uri baseUri = new("https://betterdev.link/");

        public BetterDevLinkParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//a[contains(@class,'finder-item-link')]").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //we'll get more info in digest itself later
                var digestName = digestNode.InnerText.Trim();
                var digestHref = digestNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //we'll populate this later
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent);
            var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//h2[contains(@class,'subtitle')]").InnerText.Split('-')[1].Trim()));
            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'issue-intro')]");
            string descriptionText;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
            }
            else
            {
                descriptionText = "";
            }

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digest.DigestName,
                DigestDescription = descriptionText,
                DigestURL = digest.DigestURL,
                Provider = digest.Provider
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'issue-link')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var linkNode = link.SelectSingleNode("./a[1]");
                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;

                if (!href.Contains("://") && href.Contains('/'))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var linkTitle = linkNode.InnerText.Trim();

                var descriptionNodeOriginal = link.SelectSingleNode("./p[2]");
                string descriptionText;
                if (descriptionNodeOriginal != null)
                {
                    var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                }
                else
                {
                    descriptionText = "";
                }

                links.Add(new Link
                {
                    URL = href,
                    Title = linkTitle,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
