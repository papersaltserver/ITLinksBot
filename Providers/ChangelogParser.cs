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
            throw new NotImplementedException();
        }

        public string FormatLinkPost(Link link)
        {
            throw new NotImplementedException();
        }

        public void GetCurrentDigests(out List<Digest> digests, out List<Link> links)
        {
            digests = new List<Digest>();
            links = new List<Link>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync("https://changelog.com/weekly/archive").Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var htmlLinks = new HtmlDocument();
            htmlLinks.LoadHtml(stringResult);
            var listItmesArray = htmlLinks.DocumentNode.SelectNodes("//article[@class='article']//li");
            foreach (var digest in listItmesArray)
            {
                var digestUrl = digest.Descendants("a").FirstOrDefault().GetAttributeValue("href", "Not found");
                digests.Add(new Digest {
                    DigestDay = DateTime.Parse(HttpUtility.HtmlDecode(digest.InnerText).Split('—')[1].Trim()),
                    DigestName = HttpUtility.HtmlDecode(digest.InnerText),
                    DigestDescription = "", //changelog doesn't have description for digest itself
                    DigestURL = digestUrl,
                    Provider = _chagelogProvider
                });
            }
            throw new NotImplementedException();
        }
    }
}
