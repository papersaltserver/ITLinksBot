using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class CSharpDigestParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "c# digest";
        readonly Uri baseUri = new("https://newsletter.csharpdigest.net/");
        public CSharpDigestParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>{digest.DigestName} - {digest.DigestDay}</b>\n{digest.DigestDescription}\n{digest.DigestURL}";
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'group')][.//figure]").Take(6);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode("./a");
                var href = urlNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains('/'))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }

                var dateNode = urlNode.SelectSingleNode(".//time");
                var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));

                var textNode = urlNode.SelectSingleNode(".//div[h2]");
                var titleNode = textNode.SelectSingleNode("./h2");
                var descriptionNode = textNode.SelectSingleNode("./p");


                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = titleNode.InnerText.Trim(),
                    DigestDescription = descriptionNode.InnerText.Trim(), //no description for this digest
                    DigestURL = href,
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
            var currentNode = linksHtml.DocumentNode.SelectSingleNode("//div[@id='content-blocks']/div[1]");
            int i = 0;
            while (currentNode != null && !currentNode.InnerText.Contains("how did you like this issue?"))
            {
                // Mostly now Programming digest built as Header node -> Tldr node, but not every header contains link
                var hrefNode = currentNode.SelectSingleNode("./p/a[contains(@class,'link')]");
                string href;
                string title;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                if (hrefNode != null)
                {
                    // Link section
                    href = hrefNode.GetAttributeValue("href", "Not found");
                    href = Utils.UnshortenLink(href);
                    title = hrefNode.InnerText;
                    var subTitle = hrefNode.NextSibling;
                    while (subTitle != null)
                    {
                        descriptionNode.AppendChild(subTitle.Clone());
                        subTitle = subTitle.NextSibling;
                    }
                    descriptionNode.AppendChild(HtmlNode.CreateNode("\n"));
                    currentNode = currentNode.NextSibling;
                }
                else
                {
                    // bla bla bla section
                    href = $"{digest.DigestURL}#section{i}";
                    title = "";
                }

                do
                {
                    descriptionNode.AppendChild(currentNode.Clone());
                    currentNode = currentNode.NextSibling;
                }
                while (currentNode != null && currentNode.SelectSingleNode("./p/a[contains(@class,'link')]") == null && !currentNode.InnerText.Contains("how did you like this issue?"));

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
                i++;
            }
            return links;
        }
    }
}
