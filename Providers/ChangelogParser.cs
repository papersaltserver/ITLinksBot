using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

        public void GetCurrentDigests(out List<Digest> digests, out List<Link> links)
        {
            digests = new List<Digest>();
            links = new List<Link>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_chagelogProvider.DigestURL).Result;
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
                var digestContent = httpClient.GetAsync(digestUrl).Result;
                var linksHtml = new HtmlDocument();
                linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
                var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@class='news_item']");
                foreach(var link in linksInDigest)
                {
                    var titleNode = link.SelectSingleNode(".//h2[@class='news_item-title']");
                    var title = titleNode.InnerText;
                    var href = titleNode.Descendants("a").FirstOrDefault().GetAttributeValue("href", "Not found");
                    var description = link.SelectSingleNode(".//div[@class='news_item-content']").InnerText;
                    links.Add(new Link
                    {
                        URL = href,
                        Title = title,
                        Description = description,
                        Digest = currentDigest
                    });
                }
            }
        }
    }
}
