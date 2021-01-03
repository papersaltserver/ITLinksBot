using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Serilog;

namespace ItLinksBot.Providers
{
    class ChangelogParser : IParser
    {
        readonly Provider _chagelogProvider;
        public ChangelogParser(Provider provider)
        {
            _chagelogProvider = provider;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}", digest.DigestName, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            var digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage archiveContent;
            try
            {
                archiveContent = httpClient.GetAsync(_chagelogProvider.DigestURL).Result;
            }catch (Exception e)
            {
                Log.Error("Error getting Changelog digest list: {exception}", e.Message);
                return null;
            }
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article[@class='article']//li").Take(50);
            foreach (var digest in digestsInArchive)
            {
                var digestUrl = digest.Descendants("a").FirstOrDefault().GetAttributeValue("href", "Not found");
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(HttpUtility.HtmlDecode(digest.InnerText).Split('—')[1].Trim()),
                    DigestName = HttpUtility.HtmlDecode(digest.InnerText),
                    DigestDescription = "", //changelog doesn't have description for digest itself
                    DigestURL = digestUrl,
                    Provider = _chagelogProvider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }
        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new List<Link>();
            HttpClient httpClient = new HttpClient();
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@class='news_item']");
            foreach (var link in linksInDigest)
            {
                var titleNode = link.SelectSingleNode(".//h2[@class='news_item-title']");
                var title = titleNode.InnerText;
                var href = titleNode.Descendants("a").FirstOrDefault().GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var description = link.SelectSingleNode(".//div[@class='news_item-content']").InnerText;
                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = description,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
