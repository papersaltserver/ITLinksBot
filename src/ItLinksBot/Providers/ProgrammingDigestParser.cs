using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class ProgrammingDigestParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "programming digest";
        readonly Uri baseUri = new("https://programmingdigest.net/");
        public ProgrammingDigestParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='main']/ul/li").Take(6);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode("./a");
                var href = urlNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains('/'))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }

                var dateNode = urlNode.NextSibling.NextSibling;
                var digestDate = DateTime.Parse(dateNode.InnerText);
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = urlNode.InnerText.Trim(),
                    DigestDescription = "", //no description for this digest
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

            var digestContent = htlmContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var currentNode = linksHtml.DocumentNode.SelectSingleNode("//article/*[1]");
            int i = 0;
            while (currentNode != null)
            {
                // Mostly now Programming digest built as Header node -> Tldr node, but not every header contains link
                var hrefNode = currentNode.SelectSingleNode(".//a");
                string href;
                if (hrefNode != null)
                {
                    href = hrefNode.GetAttributeValue("href", "Not found");
                    href = Utils.UnshortenLink(href);
                }
                else
                {
                    href = $"{digest.DigestURL}#section{i}";
                }
                string title = currentNode.InnerText;
                do
                {
                    currentNode = currentNode.NextSibling;
                } while (currentNode != null && currentNode.Name == "#text");

                string description = "";
                if (currentNode != null)
                {
                    var descriptionNode = currentNode.Clone();
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    description = textSanitizer.Sanitize(descriptionNode.InnerHtml);
                }

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = description,
                    LinkOrder = i,
                    Digest = digest
                });
                if (currentNode != null)
                {
                    do
                    {
                        currentNode = currentNode.NextSibling;
                    } while (currentNode != null && (currentNode.Name == "#text" || currentNode.Name == "#comment"));
                }
                i++;
            }
            return links;
        }
    }
}
