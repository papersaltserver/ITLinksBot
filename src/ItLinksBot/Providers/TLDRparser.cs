using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ItLinksBot.Providers
{
    public class TLDRparser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "TLDR";
        readonly Uri baseUri = new("https://tldr.tech/");
        public TLDRparser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>[{digest.DigestDay}]{digest.DigestName}</b>\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            if (link.Category != null && link.Category != "")
            {
                return $"<strong>[{link.Category}]{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
            else
            {
                return $"<strong>{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
            }
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            string nextDataNode = digestArchiveHtml.DocumentNode.SelectNodes("//script[@id='__NEXT_DATA__']").First().InnerText;
            var buildId = (string)JObject.Parse(nextDataNode)["buildId"];
            var archivesJsonString = htmlContentGetter.GetContent($"https://tldr.tech/_next/data/{buildId}/tech/archives.json");
            JObject archivesObject = JObject.Parse(archivesJsonString);
            JArray latestIssues = (JArray)archivesObject["pageProps"]["campaigns"];

            foreach (JObject digestNode in latestIssues.Take(5))
            {
                var digestUrl = new Uri(baseUri, $"/_next/data/{buildId}/tech/{(string)digestNode["date"]}.json?date={(string)digestNode["date"]}");
                var dateText = (string)digestNode["date"];
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(dateText),
                    DigestName = (string)digestNode["subject"],
                    DigestDescription = "", //tldr doesn't have description for digest itself
                    DigestURL = digestUrl.AbsoluteUri,
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
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            JObject digestObject = JObject.Parse(digestContent);
            JArray linksInDigest = (JArray)digestObject["pageProps"]["stories"];
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                JObject link = (JObject)linksInDigest[i];
                var categories = new Dictionary<string, string>();
                categories["sponsor"] = "Sponsor";
                categories["big"] = "\uD83D\uDCF1 Big Tech & Startups";
                categories["future"] = "\uD83D\uDE80 Science & Futuristic Technology";
                categories["programming"] = "\uD83D\uDCBB Programming, Design & Data Science";
                categories["miscellaneous"] = "\uD83C\uDF81 Miscellaneous";
                categories["quick"] = "⚡Quick Links";
                categories["cryptosponsor"] = "Sponsor";
                categories["markets"] = "\uD83D\uDCC8 Markets & Business";
                categories["innovation"] = "\uD83D\uDE80 Innovation & Launches";
                categories["guides"] = "\uD83D\uDCA1 Guides & Resources";
                categories["crypto"] = "\uD83E\uDD84 Miscellaneous";
                categories["cryptoquick"] = "⚡Quick Links";
                categories["jobs"] = "\uD83D\uDCBC Jobs";

                var descriptionHtml = new HtmlDocument();
                descriptionHtml.LoadHtml((string)link["tldr"]);
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionHtml.DocumentNode);
                string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                string safeCategory;
                categories.TryGetValue((string)link["category"], out safeCategory);
                links.Add(new Link
                {
                    URL = (string)link["url"],
                    Title = Regex.Replace((string)link["title"], "<.*?>", string.Empty),
                    Category = safeCategory,
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
