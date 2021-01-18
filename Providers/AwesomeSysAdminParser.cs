using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ItLinksBot.Providers
{
    class AwesomeSysAdminParser : IParser
    {
        private readonly Provider _awesomeSysAdminProvider;
        readonly Uri baseUri = new Uri("https://sysadmin.libhunt.com/");

        public AwesomeSysAdminParser(Provider provider)
        {
            _awesomeSysAdminProvider = provider;
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
            var archiveContent = httpClient.GetAsync(_awesomeSysAdminProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'main-content')]//table/tr").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//td[1]");
                var digestDate = DateTime.Parse(dateNode.InnerText.Trim());
                var linkNode = digestNode.SelectSingleNode(".//td[2]/a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //Awesome SysAdmin doesn't have description for digest itself
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _awesomeSysAdminProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//li[contains(@class,'story')]");
            //var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.SelectSingleNode(".//a[contains(@class,'title')]").InnerText;
                var href = link.SelectSingleNode(".//a[contains(@class,'title')]")?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descritionNode = link.SelectSingleNode(".//p[contains(@class,'description')]");
                string description;
                if (descritionNode != null)
                {
                    description = descritionNode.InnerText.Trim();
                }
                else
                {
                    description = "";//not every link in this digest has description
                }

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
