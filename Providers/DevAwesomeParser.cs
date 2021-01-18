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
    class DevAwesomeParser : IParser
    {
        private readonly Provider _devAwesomeProvider;
        private readonly Uri baseUri = new Uri("https://devawesome.io/");
        public DevAwesomeParser(Provider provider)
        {
            _devAwesomeProvider = provider;
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
            var archiveContent = httpClient.GetAsync(_devAwesomeProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
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
                    DigestName = HttpUtility.HtmlDecode(digestNode.InnerText).Trim(),
                    DigestDescription = "", //description is hard to get and is always the same
                    DigestURL = digestUrl,
                    Provider = _devAwesomeProvider
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
            var preDigestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var preIframe = new HtmlDocument();
            preIframe.LoadHtml(preDigestContent.Content.ReadAsStringAsync().Result);
            var iframeNode = preIframe.DocumentNode.SelectSingleNode("//iframe[@id='newsletter-demo']");
            var realLink = iframeNode.GetAttributeValue("src", "Not found");

            var digestContent = httpClient.GetAsync(realLink).Result;
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//table[@class='container']//table//tr[position()>1]//a");
            //var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode(".//p[1]");
                var title = HttpUtility.HtmlDecode(titleNode.InnerText).Trim();
                var descriptionNode = link.SelectSingleNode(".//p[2]");
                var description = HttpUtility.HtmlDecode(descriptionNode.InnerText.Trim());

                var href = link.GetAttributeValue("href", "Not found");
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
