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
        readonly Uri baseUri = new("https://csharpdigest.net/");
        public CSharpDigestParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>{digest.DigestName} - {digest.DigestDay:yyyy-MM-dd}</b>\n{digest.DigestDescription}\n{digest.DigestURL}";
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[h2[a]]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode("./h2/a");
                var href = urlNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains('/'))
                {
                    href = new Uri(baseUri, href).AbsoluteUri;
                }

                var dateNode = digestNode.SelectSingleNode(".//small");
                var digestDate = DateTime.Parse(dateNode.InnerText);

                var titleNode = digestNode.SelectSingleNode("./h2");
                var descriptionNode = digestNode.SelectSingleNode("./p");


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
            var currentNode = linksHtml.DocumentNode.SelectSingleNode("//div[contains(@class,'campaign')]/p[1]");
            int i = 0;
            while (currentNode != null)
            {
                // Mostly now Programming digest built as Header node -> Tldr node, but not every header contains link
                var hrefNode = currentNode.SelectSingleNode("./a");
                string href;
                string title;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                if (hrefNode != null)
                {
                    // Link section
                    href = hrefNode.GetAttributeValue("href", "Not found");
                    href = Utils.UnshortenLink(href);
                    title = hrefNode.InnerText;
                    var subTitle = currentNode.SelectSingleNode("./em");
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
                while (currentNode != null && currentNode.SelectSingleNode("./a") == null);

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
