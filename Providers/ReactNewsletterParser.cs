using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ItLinksBot.Providers
{
    class ReactNewsletterParser : IParser
    {
        readonly Provider _reactNewsletterProvider;
        readonly Uri baseUri = new Uri("https://ui.dev/");
        public ReactNewsletterParser(Provider provider)
        {
            _reactNewsletterProvider = provider;
        }
        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0}</b>\n{1}\n{2}", digest.DigestName, digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests()
        {
            List<Digest> digests = new List<Digest>();
            HttpClient httpClient = new HttpClient();
            var archiveContent = httpClient.GetAsync(_reactNewsletterProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//ul[contains(@class,'Newsletter__Issues')]/li").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var relativePathNode = digestNode.SelectSingleNode(".//a");
                var digestUrl = new Uri(baseUri, relativePathNode.GetAttributeValue("href", "Not found"));
                var digestDate = DateTime.Parse(digestNode.SelectSingleNode(".//h4").InnerText, new CultureInfo("en-US", false));
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = HttpUtility.HtmlDecode(relativePathNode.InnerText).Trim(),
                    DigestDescription = HttpUtility.HtmlDecode(digestNode.SelectSingleNode(".//p").InnerText),
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _reactNewsletterProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'Issue__Content')]//h3");
            var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                HtmlNode link = linksInDigest[i];
                var title = link.InnerText;
                var href = link.SelectSingleNode(".//a")?.GetAttributeValue("href", "Not found");
                if (href == null) continue;
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                
                //copying nodes related to the current link to a new abstract node
                while(sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper()!="H2" && sibling.Name.ToUpper() != "HR")
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

                links.Add(new Link
                {
                    URL = Utils.UnshortenLink(href),
                    Title = title,
                    Description = descriptionNode.InnerHtml.Trim(),
                    LinkOrder = i,
                    Digest = digest
                });
            }
            return links;
        }
    }
}
