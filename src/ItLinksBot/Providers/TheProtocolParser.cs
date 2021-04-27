using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.Providers
{
    class TheProtocolParser : IParser
    {
        public string CurrentProvider => "THE PROTOCOL";
        private readonly IContentGetter contentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        //readonly Uri baseUri = new("https://us13.campaign-archive.com");

        public TheProtocolParser(IContentGetter cg, IContentNormalizer cn, ITextSanitizer ts)
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
            return string.Format("<strong>{0}</strong>\n\n{1}", link.Title, link.Description);
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
                    DigestDescription = "", //TheProtocol doesn't provide one
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//*[@id='templateBody']//table//table//table//td[contains(@class,'mcnTextContent')][not(div)]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./ancestor::table[contains(@class,'mcnTextBlock')]/preceding-sibling::table[1]//div");
                string title;
                if (titleNode != null)
                {
                    title = titleNode.InnerText; //no titles
                }
                else
                {
                    title = "";
                }
                var href = digest.DigestURL + "#section-" + i;
                
                //var contentNodes = link.SelectNodes("./table[not(contains(@class,'leading_item'))]");
                string descriptionText;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(link);
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

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
