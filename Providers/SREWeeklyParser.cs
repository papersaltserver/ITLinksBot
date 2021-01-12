using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace ItLinksBot.Providers
{
    class SREWeeklyParser : IParser
    {
        private readonly Provider _sreWeeklyProvider;
        readonly Uri baseUri = new Uri("http://sreweekly.com/");

        public SREWeeklyParser(Provider provider)
        {
            _sreWeeklyProvider = provider;
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
            var archiveContent = httpClient.GetAsync(_sreWeeklyProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//article").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//div[contains(@class,'entry-date')]");
                var digestDate = DateTime.Parse(dateNode.InnerText.Trim());
                var linkNode = digestNode.SelectSingleNode(".//header//a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);
                var descriptionNode = digestNode.SelectSingleNode(".//div[contains(@class,'entry-content')]/p");
                string description;
                if (descriptionNode != null)
                {
                    description = descriptionNode.InnerText.Trim();
                }
                else
                {
                    description = "";
                }
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = description,
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _sreWeeklyProvider
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
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var linksHtml = new HtmlDocument();
            linksHtml.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'sreweekly-entry')]");
            var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            foreach (var link in linksInDigest)
            {
                var linkNode = link.SelectSingleNode(".//div[contains(@class,'sreweekly-title')]/a");
                var title = linkNode.InnerText;
                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var descriptionNodeOriginal = link.SelectSingleNode(".//div[contains(@class,'sreweekly-description')]");
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(descriptionNodeOriginal.Clone());
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

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionNode.InnerHtml,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
