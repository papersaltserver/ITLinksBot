﻿using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class DevAwesomeParser : IParser
    {
        private readonly Uri baseUri = new("https://devawesome.io/");
        public string CurrentProvider => "Dev Awesome";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public DevAwesomeParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}\n{2}", digest.DigestName, digest.DigestDescription, digest.DigestURL);
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@id='issues-index']//li/p").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode("./a");
                var digestUrl = urlNode.GetAttributeValue("href", "Not found");
                var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Split('-')[1].Trim());
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestNode.InnerText,
                    DigestDescription = "", //description is hard to get and is always the same
                    DigestURL = digestUrl,
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

            var preDigestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var preIframe = new HtmlDocument();
            preIframe.LoadHtml(preDigestContent);
            var iframeNode = preIframe.DocumentNode.SelectSingleNode("//iframe[@id='newsletter-demo']");
            var realLink = iframeNode.GetAttributeValue("src", "Not found");

            var digestContent = htmlContentGetter.GetContent(realLink);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//table[@class='container']//table//tr[position()>1]//a");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode(".//p[1]");
                var title = titleNode.InnerText;
                var descriptionNode = contentNormalizer.NormalizeDom(link.SelectSingleNode(".//p[2]"));
                var description = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());


                var href = link.GetAttributeValue("href", "Not found");
                Uri uriHref = new(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = description,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
