using HtmlAgilityPack;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class SmashingEmailParser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Smashing Email Newsletter";
        readonly Uri baseUri = new("https://www.smashingmagazine.com/");

        public SmashingEmailParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            contentGetter = cg;
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
            var stringResult = contentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//ol[contains(@class,'internal__toc--newsletter')]/li/a").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                var digestDate = new DateTime(1900, 1, 1);  //Smashing Magazine doesn't have this info in digest list, will populate later
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = HttpUtility.HtmlDecode(digestNode.InnerText).Trim(),
                    DigestDescription = "", //smashing magazine doesn't have description in digest list, will populate later
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }
        public Digest GetDigestDetails(Digest digest)
        {
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent);
            var digestName = HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//h1[contains(@class,'header--indent')]").InnerText).Replace("\n", " ");
            var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//span[contains(@class,'header__title-desc')]").InnerText));

            var sibling = digestDetails.DocumentNode.SelectSingleNode("//h3[@id='editorial']").NextSibling;
            var descriptionNode = HtmlNode.CreateNode("<div></div>");

            //copying nodes related to the current link to a new abstract node
            while (sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper() != "OL")
            {
                descriptionNode.AppendChild(sibling.Clone());
                sibling = sibling.NextSibling;
            }

            descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
            string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digestName,
                DigestDescription = descriptionText,
                DigestURL = digest.DigestURL,
                Provider = digest.Provider
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'internal__content--newsletter')]/h3[not(@id='editorial')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = HttpUtility.HtmlDecode(link.InnerText);

                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper() != "H2" && sibling.GetAttributeValue("class", "not found").ToLower() != "promo-newsletter--newsletter")
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    sibling = sibling.NextSibling;
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                var href = descriptionNode.SelectSingleNode(".//a")?.GetAttributeValue("href", "Not found");
                if (href == null) {
                    Log.Warning("SmashingMagazin section {title} in digest {url} doesn't have links", title, digest.DigestURL);
                    continue;
                };
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

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
