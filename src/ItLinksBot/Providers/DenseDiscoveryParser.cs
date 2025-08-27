using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace ItLinksBot.Providers
{
    class DenseDiscoveryParser : IParser
    {
        public string CurrentProvider => "Dense Discovery";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public DenseDiscoveryParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format($"<b>{digest.DigestName} - {digest.DigestDay:yyyy-MM-dd}</b>\n{digest.DigestDescription}\n{digest.DigestURL}");
        }

        public string FormatLinkPost(Link link)
        {
            if (link.Title == "SINGLE_IMAGE")
            {
                return link.URL;
            }
            return $"[{link.Category}]\n{link.Description}";
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);

            var digestsInArchive = feed.Items.Take(40);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //we'll fill it later
                var digestName = digestNode.Title.Text;
                var fullHref = digestNode.Links[0].Uri.AbsoluteUri;

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //We'll fill it later
                    DigestURL = fullHref,
                    Provider = provider
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            //Initial link leading to a loader page, let's get actual link
            var issueNumber = Regex.Matches(digest.DigestURL, @"https:\/\/www\.densediscovery\.com\/issues\/(\d+)")[0].Groups[1].Value;
            string realLink = $"https://www.densediscovery.com/archive/{issueNumber}/";

            //getting real content
            string digestContent = htmlContentGetter.GetContent(realLink);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);

            //getting description of the digest
            var descriptionCurrentNode = digestDocument.DocumentNode.SelectSingleNode("//table[contains(@class,'body')]//tr[td[contains(@class,'spacer') or contains(@class,'spc')]]/following-sibling::tr[.//h1]");
            // this will throw if description was not found, let it be this way
            descriptionCurrentNode = descriptionCurrentNode.NextSibling;
            var descriptionNode = HtmlNode.CreateNode("<div></div>");
            while (descriptionCurrentNode != null && descriptionCurrentNode.SelectSingleNode("./td[contains(@class,'spacer') or contains(@class,'spc')]") == null)
            {
                descriptionNode.AppendChild(descriptionCurrentNode.Clone());
                descriptionCurrentNode = descriptionCurrentNode.NextSibling;
            }

            descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
            string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

            //very dirty hack to get date, may be broken any time, no way to get something more suitable so far
            HttpClient imgClient = new();
            imgClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");
            var baseUri = new Uri(realLink);
            var headImageLink = new Uri(baseUri, "head.jpg").AbsoluteUri;
            var imgContent = imgClient.GetAsync(headImageLink).Result;
            var fileModifiedDate = imgContent.Content.Headers.LastModified.Value.DateTime;

            var currentDigest = new Digest
            {
                DigestDay = fileModifiedDate,
                DigestName = digest.DigestName,
                DigestDescription = descriptionText,
                DigestURL = digest.DigestURL,
                Provider = digest.Provider
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new();
            //this provider has many different sections with different content, so it should be processed separately
            //Initial link leading to a loader page, let's get actual link
            var issueNumber = Regex.Matches(digest.DigestURL, @"https:\/\/www\.densediscovery\.com\/issues\/(\d+)")[0].Groups[1].Value;
            string realLink = $"https://www.densediscovery.com/archive/{issueNumber}/";

            //getting real content
            string digestContent = htmlContentGetter.GetContent(realLink);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);

            var categories = digestDocument.DocumentNode.SelectNodes("//table[contains(@class,'body')]//tr[td[contains(@class,'space') or contains(@class,'spc')]][count(preceding-sibling::tr[td[contains(@class,'space') or contains(@class,'spc')]])>1]/following-sibling::tr[.//h1][1]");
            int i = 0;
            foreach (var cat in categories)
            {
                var currentNode = cat.NextSibling;
                while (currentNode != null && currentNode.SelectSingleNode("./td[contains(@class,'spacer') or contains(@class,'spc')]") == null)
                {
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    descriptionNode.AppendChild(currentNode.Clone());
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                    var images = currentNode.SelectNodes(".//img[contains(@class,'img')]");

                    if (normalizedDescription == "")
                    {
                        currentNode = currentNode.NextSibling;
                        continue;
                    }
                    links.Add(new Link
                    {
                        URL = $"{digest.DigestURL}#section{i}", // we'll not be saving real links in sake of simplicity
                        Title = "", // Title skipped as well
                        Category = cat.InnerText.Trim(),
                        Description = normalizedDescription,
                        LinkOrder = i,
                        Digest = digest
                    });
                    i++;
                    if (images != null)
                    {
                        for (int j = 0; j < images.Count; j++)
                        {
                            HtmlNode img = images[j];
                            string imgHref = img.GetAttributeValue("src", "not found");
                            imgHref = new Uri(new Uri(realLink), imgHref).AbsoluteUri;
                            links.Add(new Link
                            {
                                URL = imgHref,
                                Title = "SINGLE_IMAGE",
                                Description = "",
                                LinkOrder = i,
                                Digest = digest
                            });
                            i++;
                        }
                    }
                    currentNode = currentNode.NextSibling;
                }
            }


            return links;
        }
    }
}
