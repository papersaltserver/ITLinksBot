using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class AwesomeSysAdminParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Awesome SysAdmin Newsletter";
        readonly Uri baseUri = new("https://sysadmin.libhunt.com/");

        public AwesomeSysAdminParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
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
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'main-content')]//table/tr").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//td[1]");
                var digestDate = DateTime.Parse(dateNode.InnerText.Trim());
                var linkNode = digestNode.SelectSingleNode(".//td[2]/a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //Awesome SysAdmin doesn't have description for digest itself
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//li[contains(@class,'story') and not(@id='saashub-newsletter') and not(@id='sponsored')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.SelectSingleNode(".//a[contains(@class,'title')]").InnerText;
                var href = link.SelectSingleNode(".//a[contains(@class,'title')]")?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descritionNode = link.SelectSingleNode(".//p[contains(@class,'description')]");
                descritionNode = contentNormalizer.NormalizeDom(descritionNode);
                string description;
                if (descritionNode != null)
                {
                    description = descritionNode.InnerHtml.Trim();
                    description = textSanitizer.Sanitize(description);
                }
                else
                {
                    description = "";//not every link in this digest has description
                }

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
