using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot.Providers
{
    class DevOpsishParser : IParser
    {
        public string CurrentProvider => "DevOps'ish";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("https://devopsish.com");

        public DevOpsishParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
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
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article[contains(@class,'blog-post')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var hrefNode = digestNode.SelectSingleNode(".//h2[contains(@class,'blog-post-title')]/a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                var digestUrl = new Uri(baseUri, digestHref);
                var fullHref = Utils.UnshortenLink(digestUrl.AbsoluteUri);
                var digestDate = new DateTime(1900, 1, 1); //we'll fill it later

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,//we'll fill it l
                    DigestName = digestName,
                    DigestDescription = "", //we'll fill it later
                    DigestURL = fullHref,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            string digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);
            HtmlNodeCollection digestDescription = digestDocument.DocumentNode.SelectNodes("//article[contains(@class,'blog-post')]/p[preceding-sibling::div[contains(@class,'emailoctopus-form')] and count(preceding-sibling::h2)=0]");
            HtmlNode descriptionNode = HtmlNode.CreateNode("<div></div>");
            string descriptionText = "";
            if (digestDescription != null)
            {
                foreach (HtmlNode digestParagraph in digestDescription)
                {
                    descriptionNode.AppendChild(digestParagraph.Clone());
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
            }

            var dateNode = digestDocument.DocumentNode.SelectSingleNode("//article[contains(@class,'blog-post')]//time");
            string dateText = dateNode.GetAttributeValue("datetime", "Not found");
            var digestDate = DateTime.Parse(dateText);

            digest.DigestDay = digestDate;
            digest.DigestDescription = descriptionText;

            return digest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//article[contains(@class,'blog-post')]/p[count(preceding-sibling::h2)>0 and count(preceding-sibling::hr)=0]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                string title = ""; //no separate titles in devopsish

                HtmlNode hrefNode = link.SelectSingleNode(".//a[1]");
                if (hrefNode == null)
                {
                    Log.Warning("Unable to find <a> element in the following block:\n{href}\n", link.InnerText);
                    continue;
                }
                var href = hrefNode.GetAttributeValue("href", "Not found");
                href = new Uri(baseUri, href).AbsoluteUri;
                href = Utils.UnshortenLink(href);

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
