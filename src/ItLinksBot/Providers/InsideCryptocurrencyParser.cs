using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ItLinksBot.Providers
{
    class InsideCryptocurrencyParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Inside Cryptocurrency";
        readonly Uri baseUri = new("https://inside.com/");

        private readonly Dictionary<string, string> additionaHeaders = new() { { "X-Application-Secret", "de9289e42b6863e0878e0c0d60dca05810623cb1d7680ed493854f0137e027bf15b4ed2a9dc899075255b6d49130d2d8d65f59dab38100ecfd9df215c66ba8e1" } };
        private readonly int numbersToProcess = 5;
        private readonly string campaignApiUrl = "https://community-api.inside.com/v1/campaigns/";
        private readonly string campaignUrlBase = "https://inside.com/campaigns/";

        public InsideCryptocurrencyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            Uri digestStartingUri = new(provider.DigestURL);
            string nextPage = provider.DigestURL;
            HashSet<int> issues = new();
            JObject resultPageObject;
            do
            {
                var stringResult = htmlContentGetter.GetContent(nextPage, additionaHeaders);
                resultPageObject = JObject.Parse(stringResult);
                JArray dataItems = (JArray)resultPageObject["data"];
                foreach (JObject dataEntry in dataItems)
                {
                    JToken campaignToken = dataEntry["campaign"];
                    if (campaignToken != null && campaignToken.Type != JTokenType.Null)
                    {
                        issues.Add((int)dataEntry["campaign"]["id"]);
                    }
                    if (issues.Count >= numbersToProcess)
                    {
                        break;
                    }
                }
                nextPage = new Uri(digestStartingUri, (string)resultPageObject["meta"]["next"]).AbsoluteUri + "&list=cryptocurrency";
            } while (issues.Count < numbersToProcess && (string)resultPageObject["meta"]["next"] != null);
            foreach( int digestId in issues)
            {
                string digestApiUrl = campaignApiUrl + digestId.ToString();
                string digestReadableUrl = campaignUrlBase + digestId.ToString();
                string digestBodyString = htmlContentGetter.GetContent(digestApiUrl, additionaHeaders);
                if(digestBodyString == null || digestBodyString == "")
                {
                    Log.Warning("Inside Cryptocurrency issue {issue} doesn't exist. API Link: {apiLink}. Real link: {realLink}", digestId, digestApiUrl, digestReadableUrl);
                    continue;
                }
                JObject digestBodyObject = JObject.Parse(digestBodyString);
                string digestName = (string)digestBodyObject["data"]["title"];
                var dayString = Regex.Matches(digestName, @"\((.*)\)")[0].Groups[1].Value;
                dayString = Regex.Replace(dayString, @"(\d+)\w+,", "$1,");
                var digestDate = DateTime.Parse(dayString);
                string digestDescription = (string)digestBodyObject["data"]["subject"];
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = digestDescription,
                    DigestURL = digestReadableUrl,
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
            string apiDigestLink = campaignApiUrl + digest.DigestURL.Split('/').LastOrDefault();
            var digestContentJson = htmlContentGetter.GetContent(apiDigestLink, additionaHeaders);
            var digestContent = (string)JObject.Parse(digestContentJson)["data"]["content"];
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'column-content')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var linkNode = link.SelectSingleNode(".//comment()[. = ' STORY FOOTER : START ']/following-sibling::p/a");
                if (linkNode == null)
                {
                    linkNode = link.SelectSingleNode("../div[contains(@class,'column-share')]//p[1]/a");
                }

                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;

                href = (new Uri(baseUri, href)).AbsoluteUri;
                href = Utils.UnshortenLink(href);

                var descriptionNodeOriginal = link.SelectSingleNode(".//div[contains(@class,'story-body')]");
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal);

                string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                links.Add(new Link
                {
                    URL = href,
                    Title = "", //no separate title
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
