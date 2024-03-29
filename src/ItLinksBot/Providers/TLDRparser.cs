﻿using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ItLinksBot.Providers
{
    public class TLDRparser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "TLDR";
        readonly Uri baseUri = new("https://tldr.tech/");
        public TLDRparser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>[{digest.DigestDay}] {digest.DigestName}</b>\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            if (link.Category != null && link.Category != "")
            {
                return $"<strong>[{link.Category}] {link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
            else
            {
                return $"<strong>{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes(".//a[div[contains(@class,'mb-4')]]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode;
                var href = urlNode.GetAttributeValue("href", "Not found");
                Uri digestUri = new Uri(baseUri, href);
                if (!href.Contains("://") && href.Contains('/'))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }

                var dateText = digestUri.Segments.LastOrDefault().TrimEnd('/');
                var digestDate = DateTime.Parse(dateText);
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
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var categories = linksHtml.DocumentNode.SelectNodes(".//div[div[h6]]");
            int linkOrder = 0;
            foreach (var category in categories)
            {
                var categoryNode = category.SelectSingleNode("./div[h6]");
                var categoryText = categoryNode?.InnerText.Replace("\r\n", "\n").Replace("\n", "").Trim();
                var linkNodes = category.SelectNodes("./div[a]");
                foreach (var link in linkNodes)
                {
                    var hrefNode = link.SelectSingleNode("./a");
                    var hrefText = hrefNode?.InnerText.Trim();
                    var hrefLink = hrefNode.GetAttributeValue("href", "Not found");
                    var descriptionNode = link.SelectSingleNode("./div");
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    var description = textSanitizer.Sanitize(descriptionNode.InnerHtml);
                    links.Add(new Link
                    {
                        URL = hrefLink,
                        Title = hrefText,
                        Category = categoryText,
                        Description = description,
                        LinkOrder = linkOrder,
                        Digest = digest
                    });
                    linkOrder++;
                }
            }

            return links;
        }
    }
}
