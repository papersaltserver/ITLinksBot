using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class StatusCodeWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "StatusCode Weekly";
        readonly Uri baseUri = new("https://weekly.statuscode.com/");

        public StatusCodeWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='issue']").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var relativePathNode = digestNode.SelectSingleNode(".//a");
                var digestUrl = new Uri(baseUri, relativePathNode.GetAttributeValue("href", "Not found"));
                var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Split('—')[1].Trim());
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = relativePathNode.InnerText.Trim(),
                    DigestDescription = "", //statuscode weekly doesn't have description for digest itself
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//table[contains(@class,'el-item')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.SelectSingleNode(".//span[@class='mainlink']/a").InnerText;
                var href = link.SelectSingleNode(".//span[@class='mainlink']/a")?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var sibling = link.SelectSingleNode(".//span[@class='mainlink']").NextSibling;

                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                //copying nodes related to the current link to a new abstract node
                while (sibling != null)
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    sibling = sibling.NextSibling;
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

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
