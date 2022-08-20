using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ItLinksBot.Providers
{
    class HowCuriousParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentGetter<byte[]> binContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "How Curious!";
        readonly Uri baseUri = new("https://www.getrevue.co/");

        public HowCuriousParser(IContentGetter<string> cg, IContentGetter<byte[]> bcg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            binContentGetter = bcg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<strong>{digest.DigestName} - {digest.DigestDay:yyyy-MM-dd}</strong>\n{digest.DigestDescription}\n{digest.DigestURL}";
        }

        public string FormatLinkPost(Link link)
        {
            string linkCategory = "";
            if (link.Category != null && link.Category != "")
            {
                linkCategory = $"[{link.Category}]";
            }

            if (link.Medias == null || !link.Medias.Any())
            {
                if (Regex.IsMatch(link.URL, @"^.*#section\d+$"))
                {
                    return $"<strong>{linkCategory}{link.Title}</strong>\n\n{link.Description}";
                }
                else
                {
                    return $"<strong>{linkCategory}{link.Title}</strong>\n\n{link.Description}\n{link.URL}";
                }

            }
            else
            {
                string title = "";
                string description = "";
                if (link.Title != null && link.Title != "")
                {
                    title = $"{link.Title}\n\n";
                }
                if (link.Description != null && link.Description != "")
                {
                    description = $"{link.Description}\n\n";
                }
                return $"{linkCategory}{title}{description}";
            }
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//section[@id='issues']/div[@id='issues-covers' or @id='issues-holder']//a|//div[contains(@class,'component__profile-issues-list')]/a").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1);
                var digestHref = digestNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = "", //will be added during next request
                    DigestDescription = "", //description will be added later
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent);
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']//time");
            var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
            var nameNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']/header/h1");
            var nameText = nameNode.InnerText.Trim();

            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("//td[contains(concat(' ',@class,' '),' description ')]");
            string normalizedDescription;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = contentNormalizer.NormalizeDom(descriptionNodeOriginal.Clone());
                normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
            }
            else
            {
                normalizedDescription = "";
            }

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = nameText,
                DigestDescription = normalizedDescription,
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
            HtmlNodeCollection linksInDigest = linksHtml.DocumentNode.SelectNodes("//tr[preceding-sibling::tr[td[contains(concat(' ',@class,' '),' description ')]] and following-sibling::tr[.//div[contains(text(),'Did you enjoy this issue')]] and ./td/div[not(contains(@class,'item-header'))]]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];

                //detecting if this is an Image node
                HtmlNode firstChild = link.SelectSingleNode("./td/*[1]");
                if (firstChild.Name.ToUpper() == "IMG")
                {
                    string imgHref = firstChild.GetAttributeValue("src", "Not found");
                    string imgName = HttpUtility.UrlDecode(imgHref.Split('/').Last().Split('?').First());
                    byte[] imgFile = binContentGetter.GetContent(imgHref);
                    var currentImg = new Photo
                    {
                        ContentBytes = imgFile,
                        FileName = imgName
                    };

                    string description = firstChild.GetAttributeValue("alt", "Not found");

                    links.Add(new Link
                    {
                        URL = $"{digest.DigestURL}#section{i}",
                        Category = "",
                        Title = "",
                        Description = description,
                        LinkOrder = i,
                        Digest = digest,
                        Medias = new List<Media> { currentImg }
                    });
                }
                //detecting normal links nodes
                else if (firstChild.Name.ToUpper() == "A")
                {
                    string href = firstChild.GetAttributeValue("href", "Not found");

                    HtmlNode originalDescriptionNode = link.SelectSingleNode("./td/div[contains(@class,'item-link-description')]");
                    string descriptionText;
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChild(originalDescriptionNode.Clone());
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                    HtmlNode titleNode = link.SelectSingleNode("./td/span[contains(@class,'item-link-title')]");
                    string titleText = titleNode.InnerText.Trim();

                    HtmlNode categoryNode = link.SelectSingleNode("./preceding-sibling::tr[./td/div[contains(@class,'item-header')]][1]");
                    string categoryText = "";
                    if (categoryNode != null)
                    {
                        categoryText = categoryNode.InnerText.Trim();
                    }

                    links.Add(new Link
                    {
                        URL = href,
                        Category = categoryText,
                        Title = titleText,
                        Description = descriptionText,
                        LinkOrder = i,
                        Digest = digest,
                        Medias = null
                    });
                }
                else if (firstChild.Name.ToUpper() == "DIV" && firstChild.SelectSingleNode("./*[1]")?.Name.ToUpper() == "TABLE")
                {
                    HtmlNode hrefNode = firstChild.SelectSingleNode("./table//table//td[1]/a");
                    string href = hrefNode.GetAttributeValue("href", "Not found");
                    HtmlNode originalDescriptionNode = firstChild.SelectSingleNode("./table//tr[2]");
                    string descriptionText;
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChild(originalDescriptionNode.Clone());
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                    links.Add(new Link
                    {
                        URL = href,
                        Category = "",
                        Title = "",
                        Description = descriptionText,
                        LinkOrder = i,
                        Digest = digest,
                        Medias = null
                    });
                }
                else
                {
                    HtmlNode categoryNode = link.SelectSingleNode("./preceding-sibling::tr[./td/div[contains(@class,'item-header')]][1]");
                    string categoryText = categoryNode.InnerText.Trim();

                    string descriptionText;
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChild(link.Clone());
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                    links.Add(new Link
                    {
                        URL = $"{digest.DigestURL}#section{i}",
                        Category = categoryText,
                        Title = "",
                        Description = descriptionText,
                        LinkOrder = i,
                        Digest = digest,
                        Medias = null
                    });
                }
            }
            return links;
        }
    }
}
