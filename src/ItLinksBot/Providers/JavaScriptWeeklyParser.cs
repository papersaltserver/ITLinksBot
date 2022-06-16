using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ItLinksBot.Providers
{
    class JavaScriptWeeklyParser : IParser
    {
        private readonly IContentGetter<string> htlmContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "JavaScript Weekly";
        readonly Uri baseUri = new("https://javascriptweekly.com/");

        public JavaScriptWeeklyParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htlmContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestURL);
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
            var stringResult = htlmContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='issue']").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var relativePathNode = digestNode.SelectSingleNode(".//a");
                var digestUrl = new Uri(baseUri, relativePathNode.GetAttributeValue("href", "Not found"));
                var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Split('—')[1].Trim());
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = relativePathNode.InnerText,
                    DigestDescription = "", //javascript weekly doesn't have description for digest itself
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
            var digestContent = htlmContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//table[contains(@class,'el-item') or contains(@class,'miniitem') and not(ancestor::table[contains(@class,'jobs')])]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                string title, href, descriptionText;
                HtmlNode preDescriptionNode;
                if (link.Attributes["class"].Value.Contains("el-item"))
                {
                    // Main link format processing
                    title = link.SelectSingleNode(".//span[@class='mainlink']/a").InnerText;
                    href = link.SelectSingleNode(".//span[@class='mainlink']/a")?.GetAttributeValue("href", "Not found");
                    if (href == null) continue;
                    if (!href.Contains("://") && href.Contains('/'))
                    {
                        href = (new Uri(baseUri, href)).AbsoluteUri;
                    }
                    preDescriptionNode = link.SelectSingleNode(".//span[@class='mainlink']");
                }
                else
                {
                    // Mini links processing
                    title = link.SelectSingleNode("./descendant::a[1]").InnerText;
                    href = link.SelectSingleNode("./descendant::a[1]").GetAttributeValue("href", "Not found");
                    preDescriptionNode = link.SelectSingleNode(".//p[contains(@class,'desc')]/span[1]");

                }
                href = Utils.UnshortenLink(href);
                var category = link.SelectSingleNode("./preceding-sibling::table[contains(@class,'el-heading')][1]")?.InnerText?.Trim();
                var sibling = preDescriptionNode.NextSibling;

                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                //copying nodes related to the current link to a new abstract node
                while (sibling != null)
                {
                    if (sibling.Attributes != null && sibling.Attributes["class"] != null && !sibling.Attributes["class"].Value.Contains("name"))
                    {
                        descriptionNode.AppendChild(sibling.Clone());
                    }
                    sibling = sibling.NextSibling;
                }
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Category = category,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }

            var briefLinksInDigest = linksHtml.DocumentNode.SelectNodes("//table[contains(@class,'content')]//ul//p");
            for (int i = 0; i < briefLinksInDigest.Count; i++)
            {
                HtmlNode link = briefLinksInDigest[i];
                var title = ""; // brief links does not have title
                var href = link.SelectSingleNode(".//a")?.GetAttributeValue("href", "Not found");
                if (href == null)
                {
                    href = $"{digest.DigestURL}#briefLink{i}";
                }
                if (!href.Contains("://") && href.Contains('/'))
                {
                    href = (new Uri(baseUri, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descriptionNode = contentNormalizer.NormalizeDom(link);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                string category = "IN BRIEF";

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Category = category,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
