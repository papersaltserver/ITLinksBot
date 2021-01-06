using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ItLinksBot.Providers
{
    public class TLDRparser : IParser
    {
        readonly Provider _tldrProvider;
        readonly Uri baseUri = new Uri("https://www.tldrnewsletter.com/");
        public TLDRparser(Provider provider)
        {
            _tldrProvider = provider;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}", digest.DigestName, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_tldrProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@id='doc-container']//div[contains(@class, 'd-lg-none')]//a").Take(50);
            foreach(var digestNode in digestsInArchive)
            {
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Replace("Daily Update","")),
                    DigestName = HttpUtility.HtmlDecode(digestNode.InnerText).Trim(),
                    DigestDescription = "", //tldr doesn't have description for digest itself
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _tldrProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//tr[@eo-body]//table[@eo-block='code']");
            foreach (var link in linksInDigest)
            {
                var titleNode = link.SelectSingleNode(".//div/span/a");
                var title = titleNode.InnerText;
                var href = titleNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var description = link.SelectSingleNode(".//div/span/span").InnerText;
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
