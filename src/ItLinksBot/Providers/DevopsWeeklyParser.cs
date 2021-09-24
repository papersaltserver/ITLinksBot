using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ItLinksBot.ContentGetters;

namespace ItLinksBot.Providers
{
    class DevopsWeeklyParser : IParser
    {
        public string CurrentProvider => "Devops Weekly";
        private readonly IContentGetter<string> htmlContentGetter;
        //private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        //readonly Uri baseUri = new("https://us2.campaign-archive.com");

        public DevopsWeeklyParser(IContentGetter<string> cg, /*IContentNormalizer cn,*/ ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            //contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}\n{3}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("{0}\n{1}", link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//li[contains(@class,'campaign')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.ChildNodes[0];
                string dateText = dateNode.InnerText.Split('-')[0].Trim();
                var digestDate = DateTime.Parse(dateText, new CultureInfo("en-US", false));
                var hrefNode = digestNode.SelectSingleNode("./a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                //var digestUrl = new Uri(baseUri, digestHref);
                var fullHref = Utils.UnshortenLink(digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //Devops Weekly doesn't provide one
                    DigestURL = fullHref,
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
            var linkMatches = Regex.Matches(digestContent, @"([^\<\>\r\n\=]+?)\<br\>\r\n\<br\>\r\n(.+?)\<br\>\r\n\<br\>\r\n\<br\>", RegexOptions.Singleline);
            for (int i = 1; i < linkMatches.Count; i++)
            {
                string title = "";
                string description = linkMatches[i].Groups[1].Value;
                string descriptionText = textSanitizer.Sanitize(description.Trim());
                string hrefText = linkMatches[i].Groups[2].Value;
                string[] hrefSplitArray = hrefText.Split(new string[] { "<br>" }, StringSplitOptions.None);
                string href;
                if (hrefSplitArray.Length == 1)
                {
                    href = Utils.UnshortenLink(hrefSplitArray[0].Trim());
                }
                else
                {
                    href = hrefSplitArray[^1];
                    for (int j = 0; j < hrefSplitArray.Length - 1; j++)
                    {
                        descriptionText += $"\n{hrefSplitArray[j]}";
                    }
                }
                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }

    }
}
