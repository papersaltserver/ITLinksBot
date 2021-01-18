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
    class TechProductivityParser : IParser
    {
        private readonly Provider _digest;
        readonly Uri baseUri = new Uri("https://techproductivity.co/");

        public TechProductivityParser(Provider provider)
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//ul[contains(@class,'archive')]//li/a").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900,1,1);
                var digestName = digestNode.InnerText.Trim();
                var digestHref = digestNode.GetAttributeValue("href", "Not found");
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
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
            var titleText = digestDetails.DocumentNode.SelectSingleNode("//div[contains(text(), 'Issue #')]").InnerText.Trim();
            var dateText = HttpUtility.HtmlDecode(titleText).Split('•')[1].Trim();
            var digestDate = DateTime.Parse(dateText);
            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectSingleNode("(//*[contains(@class,'outlook-group-fix')]//div[p])[1]");
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
                DigestName = digest.DigestName,
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("(//*[contains(@class,'outlook-group-fix')]//div[a or p])[position()>2 and position()<last()-2]/p/a|(//*[contains(@class,'outlook-group-fix')]//div[a or p])[position()>2 and position()<last()-2]/a");
            var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.InnerText.Trim(); //this digest doesn't have separate header
                var href = link.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                Uri uriHref = new Uri(baseUri, href);
                href = Utils.UnshortenLink(uriHref.AbsoluteUri);

                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "BR")
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
