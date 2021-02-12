using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    public class TLDRparser : IParser
    {
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "TLDR";
        readonly Uri baseUri = new Uri("https://www.tldrnewsletter.com/");
        public TLDRparser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            contentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}", digest.DigestName, digest.DigestURL);
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@id='doc-container']//div[contains(@class, 'd-lg-none')]//a").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Replace("Daily Update", "")),
                    DigestName = HttpUtility.HtmlDecode(digestNode.InnerText).Trim(),
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
            List<Link> links = new List<Link>();
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//td[contains(@class,'container')]/div[contains(@class,'text-block')]/span/a//strong/../../..");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./a");
                if(titleNode == null)
                {
                    continue;
                }
                var title = titleNode.InnerText;
                var href = titleNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descriptionNode = contentNormalizer.NormalizeDom(link.SelectSingleNode("./span"));
                var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
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
