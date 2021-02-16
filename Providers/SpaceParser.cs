using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class SpaceParser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Space";
        readonly Uri baseUri = new Uri("https://www.getrevue.co/");

        public SpaceParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
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
            List<Digest> digests = new List<Digest>();
            var stringResult = contentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//section[@id='issues']/div[@id='issues-covers' or @id='issues-holder']//a").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1);
                var digestHref = digestNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = "", //will be added during next request
                    DigestDescription = "", //description will be added later
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
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']//time");
            var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
            var nameNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']/header/h1");
            var nameText = nameNode.InnerText.Trim();

            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'introduction')]");
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
                DigestName = nameText,
                DigestDescription = normalizedDescription,
                DigestURL = digest.DigestURL,
                Provider = digest.Provider
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new List<Link>();
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            HtmlNodeCollection linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='issue-frame']/div/div[position()>5]//div[contains(@class,'revue-p')]/../../../..|//div[@id='issue-frame']//body/div/div[position()>5]//div[contains(@class,'revue-p')]/../../../..|//div[contains(@class,'text-description')]//ul[contains(@class,'revue-ul')]/li/a");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                HtmlNode linkNode;
                if (link.Name.ToUpper() == "A")
                {
                    linkNode = link;
                }
                else
                {
                    linkNode = link.SelectSingleNode(".//a[not(img)][1]|./tr[2]//a|.//div[@class='link-title']//a");
                }
                if (linkNode == null) continue;
                var title = linkNode.InnerText.Trim();
                var href = linkNode.GetAttributeValue("href", "Not found");
                if (href == "Not found") continue;

                Uri uriHref = new Uri(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                var descriptionNodeOriginal = link.SelectSingleNode(".//div[@class='revue-p']/..");
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);
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
