using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ItLinksBot.Providers
{
    class ProgrammingDigestParser : IParser
    {
        private readonly Provider _programmingDigestProvider;
        readonly Uri baseUri = new Uri("https://programmingdigest.net/");
        public ProgrammingDigestParser(Provider provider)
        {
            _programmingDigestProvider = provider;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_programmingDigestProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='main']/h3").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode("./a");
                var href = urlNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }

                var dateNode = digestNode.NextSibling.NextSibling;
                var digestDate = DateTime.Parse(dateNode.InnerText);
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = urlNode.InnerText.Trim(),
                    DigestDescription = "", //no description for this digest
                    DigestURL = href,
                    Provider = _programmingDigestProvider
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

            HttpClient httpClient = new HttpClient();
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'digest-article')]");
            //var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./p[contains(@class,'title')]/a");
                var title = titleNode.InnerText.Trim();
                var descriptionNode = link.SelectSingleNode("./p[contains(@class,'description')]");
                var description = descriptionNode.InnerText;

                var href = titleNode.GetAttributeValue("href", "Not found");

                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

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
