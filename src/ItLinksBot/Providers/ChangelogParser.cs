using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;

namespace ItLinksBot.Providers
{
    class ChangelogParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Changelog Weekly";
        public ChangelogParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}", digest.DigestName, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return link.Title == "" ? link.Description : $"<b>{link.Title}</b>\n{link.Description}";
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);
            foreach (var feedItem in feed.Items.Take(2))
            {
                var digestDate = new DateTime(1900, 1, 1);
                var baseLink = feedItem.Links[0].Uri.AbsoluteUri;
                var digestLink = $"{baseLink}/email";
                Digest currentDigest = new()
                {
                    DigestDay = digestDate,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = "", // We'll fill it later
                    DigestURL = digestLink,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            var titleNode = linksHtml.DocumentNode.SelectSingleNode("//head/title");
            var digestName = titleNode.InnerText;
            var dayString = Regex.Matches(digestName, @"\((.*)\)")[0].Groups[1].Value;
            var digestDate = DateTime.Parse(dayString);
            var currentNode = linksHtml.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]/p[1]");
            var descriptionNode = HtmlNode.CreateNode("<div></div>");
            while (currentNode != null && currentNode.Name.ToUpper() != "H2" && currentNode.Name.ToUpper() != "HR")
            {
                descriptionNode.AppendChild(currentNode.Clone());
                currentNode = currentNode.NextSibling;
            }

            descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
            string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digestName,
                DigestDescription = descriptionText,
                DigestURL = digest.DigestURL,
                Provider = digest.Provider
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent);
            // Skipping intro
            var currentNode = linksHtml.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]/p[1]");
            while (currentNode != null && currentNode.Name.ToUpper() != "H2" && currentNode.Name.ToUpper() != "HR")
            {
                currentNode = currentNode.NextSibling;
            }
            int sectionId = 0;
            while (currentNode != null)
            {
                var sectionNode = HtmlNode.CreateNode("<div></div>");
                while (currentNode.Name.ToUpper() == "HR")
                {
                    currentNode = currentNode.NextSibling;
                }
                string title = "";
                string titleHref = "";
                do
                {
                    if (currentNode.Name.ToUpper() == "H2")
                    {
                        title = currentNode.InnerText;
                        var titleAnchor = currentNode.SelectSingleNode(".//a");
                        if (titleAnchor != null)
                        {
                            titleHref = titleAnchor.GetAttributeValue("href", "Not found");
                        }
                    }
                    else
                    {
                        sectionNode.AppendChild(currentNode.Clone());
                    }
                    currentNode = currentNode.NextSibling;
                } while (currentNode != null && currentNode.Name.ToUpper() != "H2" && currentNode.Name.ToUpper() != "HR");
                if (titleHref != "")
                {
                    var headerLink = HtmlTextNode.CreateNode(titleHref);
                    sectionNode.AppendChild(headerLink);
                }
                sectionNode = contentNormalizer.NormalizeDom(sectionNode);
                string sectionText = textSanitizer.Sanitize(sectionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = $"{digest.DigestURL}#{sectionId}",
                    Title = title,
                    Description = sectionText,
                    LinkOrder = sectionId,
                    Digest = digest
                });
                sectionId++;
            }

            return links;
        }
    }
}
