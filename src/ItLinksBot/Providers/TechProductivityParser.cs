using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class TechProductivityParser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Tech Productivity";
        readonly Uri baseUri = new("https://techproductivity.co/");

        public TechProductivityParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            contentGetter = cg;
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
            var stringResult = contentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//li[contains(@class,'campaign')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1);
                var hrefNode = digestNode.SelectSingleNode("./a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                var fullHref = Utils.UnshortenLink(digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //description will be added later
                    DigestURL = fullHref,
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
            var titleText = digestDetails.DocumentNode.SelectSingleNode("//div[contains(text(), 'Issue #')]").InnerText.Trim();
            var dateText = HttpUtility.HtmlDecode(titleText).Split('•')[1].Trim();
            var digestDate = DateTime.Parse(dateText);
            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("(//*[contains(@class,'outlook-group-fix')]//div[p])[1]");
            string normalizedDescription;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);
                normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
            }
            else
            {
                normalizedDescription = "";
            }

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digest.DigestName,
                DigestDescription = normalizedDescription,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("(//*[contains(@class,'outlook-group-fix')]//div[a or p])[position()>2 and position()<last()-2]/p/a|(//*[contains(@class,'outlook-group-fix')]//div[a or p])[position()>2 and position()<last()-2]/a");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.InnerText.Trim(); //this digest doesn't have separate header
                var href = link.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                Uri uriHref = new(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "BR")
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    sibling = sibling.NextSibling;
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                links.Add(new Link
                {
                    URL = href,
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
