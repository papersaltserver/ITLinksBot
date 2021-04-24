using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ItLinksBot.Providers
{
    class TechmemeParser : IParser
    {
        public string CurrentProvider => "Techmeme";
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("https://us14.campaign-archive.com/");

        public TechmemeParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
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
                var dateNode = digestNode.ChildNodes[0];
                string dateText = dateNode.InnerText.Split('-')[0].Trim();
                var digestDate = DateTime.Parse(dateText, new CultureInfo("en-US", false));
                var hrefNode = digestNode.SelectSingleNode("./a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                //var digestUrl = new Uri(baseUri, digestHref);
                var fullHref = Utils.UnshortenLink(digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //Techmeme doesn't provide one
                    DigestURL = fullHref,
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
            var digestContent = contentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'section')]/div[contains(@class,'story') and not(contains(@class,'sponsor'))]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode(".//td/span[contains(@class,'title')][1]");
                var title = titleNode.InnerText;
                var href = titleNode.SelectSingleNode("./a").GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = new Uri(baseUri, href).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var contentNodes = link.SelectNodes("./table[not(contains(@class,'leading_item'))]");
                string descriptionText;
                if (contentNodes != null)
                {
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChildren(contentNodes);
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                }
                else
                {
                    descriptionText = "";
                }

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
