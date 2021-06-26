using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ItLinksBot.Providers
{
    class SoftwareLeadWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Software Lead Weekly";
        readonly Uri baseUri = new("https://softwareleadweekly.com/");

        public SoftwareLeadWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htlmContentGetter = cg;
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
            var stringResult = htlmContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='table-issue']").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//p[@class='text-table-issue']");
                string[] dateArray = dateNode.InnerText.Trim().Split(" ");
                Regex rgx = new("[^0-9]");
                dateArray[0] = rgx.Replace(dateArray[0], "");
                string fixedDate = string.Join(" ", dateArray);
                var digestDate = DateTime.Parse(fixedDate);
                var linkNode = digestNode.SelectSingleNode(".//p[@class='title-table-issue']/a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //no description available
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
            var digestContent = htlmContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='app']/div/div/div/div/div");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var linkNode = link.SelectSingleNode("./a[@class='post-title' or @class='mention']");
                var title = linkNode.InnerText.Trim(); //this digest doesn't have separate header
                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                var sibling = linkNode.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "B")
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
