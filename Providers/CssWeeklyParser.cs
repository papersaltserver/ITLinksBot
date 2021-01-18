using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;


namespace ItLinksBot.Providers
{
    class CssWeeklyParser : IParser
    {
        private readonly Provider _cssWeeklyProvider;
        readonly Uri baseUri = new Uri("https://css-weekly.com/");
        public CssWeeklyParser(Provider provider)
        {
            _cssWeeklyProvider = provider;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}\n{2}", digest.DigestName, digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_cssWeeklyProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var urlNode = digestNode.SelectSingleNode(".//h3[@class='title']/a");
                var digestUrl = urlNode.GetAttributeValue("href", "Not found");
                var dateNode = digestNode.SelectSingleNode(".//time");
                var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
                var descriptionText = String.Join("\n", digestNode.SelectNodes(".//ul//li").Select(li => li.InnerText));
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = urlNode.InnerText.Trim(),
                    DigestDescription = descriptionText,
                    DigestURL = digestUrl,
                    Provider = _cssWeeklyProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//article[contains(@class,'newsletter-article')]");
            //var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./header//a");
                var title = titleNode.InnerText.Trim();
                var descriptionNode = link.SelectNodes("./p[position()<last()]").Select(art => art.InnerText);
                var description = String.Join("\n", descriptionNode);

                var href = titleNode.GetAttributeValue("href", "Not found");
                Uri uriHref = new Uri(baseUri, href);
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
