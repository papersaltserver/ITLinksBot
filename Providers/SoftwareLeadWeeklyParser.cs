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
    class SoftwareLeadWeeklyParser : IParser
    {
        private readonly Provider _softwareLeadWeeklyDigest;
        readonly Uri baseUri = new Uri("https://softwareleadweekly.com/");

        public SoftwareLeadWeeklyParser(Provider provider)
        {
            _softwareLeadWeeklyDigest = provider;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_softwareLeadWeeklyDigest.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[@class='table-issue']").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var dateNode = digestNode.SelectSingleNode(".//p[@class='text-table-issue']");
                string[] dateArray = dateNode.InnerText.Trim().Split(" ");
                Regex rgx = new Regex("[^0-9]");
                dateArray[0] = rgx.Replace(dateArray[0], "");
                string fixedDate = string.Join(" ", dateArray);
                var digestDate = DateTime.Parse(fixedDate);
                var linkNode = digestNode.SelectSingleNode(".//p[@class='title-table-issue']/a");
                var digestName = linkNode.InnerText.Trim();
                var digestHref = linkNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "", //no description available
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _softwareLeadWeeklyDigest
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[@id='app']/div/div/div/div/div");
            var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            foreach (var link in linksInDigest)
            {
                var linkNode = link.SelectSingleNode("./a[@class='post-title' or @class='mention']");
                var title = linkNode.InnerText.Trim(); //this digest doesn't have separate header
                var href = linkNode?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                var sibling = linkNode.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "B")
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    sibling = sibling.NextSibling;
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
                string normalizedDescription = Regex.Replace(descriptionNode.InnerHtml.Trim(), "( )\\1+", "$1");
                normalizedDescription = Regex.Replace(normalizedDescription, @"[\r\n]{3,}", "\n\n");
                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = normalizedDescription,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
