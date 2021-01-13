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
    class BetterDevLinkParser : IParser
    {
        private readonly Provider _betterDevLinkProvider;
        readonly Uri baseUri = new Uri("https://betterdev.link/");

        public BetterDevLinkParser(Provider provider)
        {
            _betterDevLinkProvider = provider;
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
            var archiveContent = httpClient.GetAsync(_betterDevLinkProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//a[contains(@class,'finder-item-link')]").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //we'll get more info in digest itself later
                var digestName = digestNode.InnerText.Trim();
                var digestHref = digestNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //we'll populate this later
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _betterDevLinkProvider
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
            var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//h2[contains(@class,'subtitle')]").InnerText.Split('-')[1].Trim()));
            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'issue-intro')]");
            string descriptionText;
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
                descriptionText = descriptionNode.InnerHtml.Trim();
            }
            else
            {
                descriptionText = "";
            }

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digest.DigestName,
                DigestDescription = descriptionText,
                DigestURL = digest.DigestURL,
                Provider = _betterDevLinkProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'issue-link')]");
            var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            foreach (var link in linksInDigest)
            {
                var linkNode = link.SelectSingleNode("./a[1]");
                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;

                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                var linkTitle = linkNode.InnerText.Trim();

                var descriptionNodeOriginal = link.SelectSingleNode("./p[2]");
                string descriptionText;
                if (descriptionNodeOriginal != null)
                {
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
                    descriptionText = descriptionNode.InnerHtml;
                }
                else
                {
                    descriptionText = "";
                }

                links.Add(new Link
                {
                    URL = href,
                    Title = linkTitle,
                    Description = descriptionText,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
