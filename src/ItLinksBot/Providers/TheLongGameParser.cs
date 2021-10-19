using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ItLinksBot.Providers
{
    class TheLongGameParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "The Long Game";
        readonly Uri baseUri = new("https://www.getrevue.co/");

        public TheLongGameParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<strong>{digest.DigestName} - {digest.DigestDay:yyyy-MM-dd}</strong>\n{digest.DigestDescription}\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            return $"<strong>{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            IEnumerable<HtmlNode> digestsInArchive;
            try
            {
                digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//section[@id='issues']/div[@id='issues-covers' or @id='issues-holder']//a").Take(5);
            }
            catch (Exception e)
            {
                Log.Warning("Problem {exception} with link {url} which returned the following content:\n{content}\n", e.Message, provider.DigestURL, stringResult);
                throw;
            }
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
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent);
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//time");
            var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));

            //this newsletter marked up incorrectly, they have second <html> document inside div#issue-frame, we need to work with this
            var digestRealContent = digestDetails.DocumentNode.SelectSingleNode("//div[html]").InnerHtml;
            var digestRealDetails = new HtmlDocument();
            digestRealDetails.LoadHtml(digestRealContent);

            var nameNode = digestRealDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'introduction-subject')]");
            var nameText = nameNode.InnerText.Trim();

            var descriptionNodeOriginal = digestRealDetails.DocumentNode.SelectSingleNode("html/body/div/div[3]/table//table//tr[3]");
            string normalizedDescription;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal.Clone());
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
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            //this newsletter has broken mark up, so we are forced to re-evaluate document root
            digestContent = linksHtml.DocumentNode.SelectSingleNode("//div[html]").InnerHtml;
            linksHtml.LoadHtml(digestContent);
            HtmlNodeCollection linksInDigest = linksHtml.DocumentNode.SelectNodes("//table//table[.//div[contains(@class,'link-description')]]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                HtmlNode hrefNode = link.SelectSingleNode(".//div[contains(@class,'link-title')]/a");
                if (hrefNode == null)
                {
                    hrefNode = link.SelectSingleNode(".//a");
                }
                string href = hrefNode.GetAttributeValue("href", "Not found");
                string title = hrefNode.InnerText.Trim();

                HtmlNode originalDescriptionNode = link.SelectSingleNode(".//div[contains(@class,'link-description')]");
                string descriptionText;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(originalDescriptionNode.Clone());
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Category = "",
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest,
                    Medias = null
                });
            }
            return links;
        }
    }
}
