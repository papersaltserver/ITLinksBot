using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ItLinksBot.Providers
{
    class ProductParser : IParser
    {
        private readonly Provider _digest;
        readonly Uri baseUri = new Uri("https://www.getrevue.co/");

        public ProductParser(Provider provider)
        {
            _digest = provider;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}\n{3}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_digest.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//section[@id='issues']/div[@id='issues-covers' or @id='issues-holder']//a").Take(50);
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
                    Provider = _digest
                };
                digests.Add(currentDigest);
            }
            return digests;
        }

        public Digest GetDigestDetails(Digest digest)
        {
            HttpClient httpClient = new HttpClient();
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']//time");
            var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
            var nameNode = digestDetails.DocumentNode.SelectSingleNode("//section[@id='issue-display']/header/h1");
            var nameText = nameNode.InnerText.Trim();

            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'introduction')]");
            string normalizedDescription;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(descriptionNodeOriginal.Clone());

                //removing all the tags not allowed by telegram
                var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
                var nodesToAnalyze = new Queue<HtmlNode>(descriptionNode.ChildNodes);
                while (nodesToAnalyze.Count > 0)
                {
                    var node = nodesToAnalyze.Dequeue();
                    var parentNode = node.ParentNode;

                    if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                    {
                        var childNodes = node.SelectNodes("./*|./text()");

                        if (childNodes != null)
                        {
                            foreach (var child in childNodes)
                            {
                                nodesToAnalyze.Enqueue(child);
                                parentNode.InsertBefore(child, node);
                            }
                        }
                        parentNode.RemoveChild(node);
                    }
                    else
                    {
                        var childNodes = node.SelectNodes("./*|./text()");
                        if (childNodes != null)
                        {
                            foreach (var child in childNodes)
                            {
                                nodesToAnalyze.Enqueue(child);
                            }
                        }
                    }
                }
                normalizedDescription = Regex.Replace(descriptionNode.InnerHtml.Trim(), "( )\\1+", "$1", RegexOptions.Singleline);
                normalizedDescription = normalizedDescription.Replace("\t", "");
                normalizedDescription = normalizedDescription.Replace("\r", "");
                normalizedDescription = Regex.Replace(normalizedDescription, @"[\n]{3,}", "\n\n", RegexOptions.Singleline);
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
                Provider = _digest
            };
            return currentDigest;
        }

        public List<Link> GetDigestLinks(Digest digest)
        {
            List<Link> links = new List<Link>();
            HttpClient httpClient = new HttpClient();
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            HtmlNodeCollection linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='issue-frame']/div/div[position()>5]//div[contains(@class,'revue-p')]/../../../..|//div[@id='issue-frame']//body/div/div[position()>5]//div[contains(@class,'revue-p')]/../../../..|//div[contains(@class,'text-description')]//ul[contains(@class,'revue-ul')]/li/a");
            var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                HtmlNode linkNode;
                if (link.Name.ToUpper() == "A")
                {
                    linkNode = link;
                }
                else
                {
                    linkNode = link.SelectSingleNode(".//a[not(img)][1]|./tr[2]//a|.//div[@class='link-title']//a");
                }
                if (linkNode == null) continue;
                var title = linkNode.InnerText.Trim();
                var href = linkNode.GetAttributeValue("href", "Not found");
                if (href == "Not found") continue;

                Uri uriHref = new Uri(baseUri, href);
                href = Utils.UnshortenLink(href);

                var descriptionNodeOriginal = link.SelectSingleNode(".//div[@class='revue-p']/..");
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                if (descriptionNodeOriginal != null)
                {
                    descriptionNode.AppendChild(descriptionNodeOriginal.Clone());
                }

                //removing all the tags not allowed by telegram
                var nodesToAnalyze = new Queue<HtmlNode>(descriptionNode.ChildNodes);
                while (nodesToAnalyze.Count > 0)
                {
                    var node = nodesToAnalyze.Dequeue();
                    var parentNode = node.ParentNode;

                    if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                    {
                        var childNodes = node.SelectNodes("./*|./text()");

                        if (childNodes != null)
                        {
                            foreach (var child in childNodes)
                            {
                                nodesToAnalyze.Enqueue(child);
                                parentNode.InsertBefore(child, node);
                            }
                        }
                        parentNode.RemoveChild(node);
                    }
                    else
                    {
                        var childNodes = node.SelectNodes("./*|./text()");
                        if (childNodes != null)
                        {
                            foreach (var child in childNodes)
                            {
                                nodesToAnalyze.Enqueue(child);
                            }
                        }
                    }
                }
                string normalizedDescription = Regex.Replace(descriptionNode.InnerHtml.Trim(), "( )\\1+", "$1", RegexOptions.Singleline);
                normalizedDescription = normalizedDescription.Replace("\t", "");
                normalizedDescription = normalizedDescription.Replace("\r", "");
                normalizedDescription = Regex.Replace(normalizedDescription, @"[\n]{3,}", "\n\n", RegexOptions.Singleline);
                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
