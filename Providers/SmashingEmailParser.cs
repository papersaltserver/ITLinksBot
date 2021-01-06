using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ItLinksBot.Providers
{
    class SmashingEmailParser : IParser
    {
        private readonly Provider _smashingEmailProvider;
        readonly Uri baseUri = new Uri("https://www.smashingmagazine.com/");

        public SmashingEmailParser(Provider provider)
        {
            _smashingEmailProvider = provider;
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
            var archiveContent = httpClient.GetAsync(_smashingEmailProvider.DigestURL).Result;
            var stringResult = archiveContent.Content.ReadAsStringAsync().Result;
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//ol[contains(@class,'internal__toc--newsletter')]/li/a").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                //var relativePathNode = digestNode.SelectSingleNode(".//a");
                var digestUrl = new Uri(baseUri, digestNode.GetAttributeValue("href", "Not found"));
                //var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestNode.InnerText).Split('—')[1].Trim());
                var digestDate = new DateTime(1900,1,1);  //Smashing Magazine doesn't have this info in digest list, will populate later
                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = HttpUtility.HtmlDecode(digestNode.InnerText).Trim(),
                    DigestDescription = "", //smashing magazine doesn't have description in digest list, will populate later
                    DigestURL = digestUrl.AbsoluteUri,
                    Provider = _smashingEmailProvider
                };
                digests.Add(currentDigest);
            }
            return digests;
            //throw new NotImplementedException();
        }
        public Digest GetDigestDetails(Digest digest)
        {
            HttpClient httpClient = new HttpClient();
            var digestContent = httpClient.GetAsync(digest.DigestURL).Result;
            var digestDetails = new HtmlDocument();
            digestDetails.LoadHtml(digestContent.Content.ReadAsStringAsync().Result);
            var digestName = HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//h1[contains(@class,'header--indent')]").InnerText).Replace("\n"," ");
            var digestDate = DateTime.Parse(HttpUtility.HtmlDecode(digestDetails.DocumentNode.SelectSingleNode("//span[contains(@class,'header__title-desc')]").InnerText));
            
            var sibling = digestDetails.DocumentNode.SelectSingleNode("//h3[@id='editorial']").NextSibling;
            var descriptionNode = HtmlNode.CreateNode("<div></div>");

            //copying nodes related to the current link to a new abstract node
            while (sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper() != "OL" )
            {
                descriptionNode.AppendChild(sibling.Clone());
                sibling = sibling.NextSibling;
            }

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
            }

            var currentDigest = new Digest
            {
                DigestDay = digestDate,
                DigestName = digestName,
                DigestDescription = descriptionNode.InnerHtml.Trim(),
                DigestURL = digest.DigestURL,
                Provider = _smashingEmailProvider
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
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'internal__content--newsletter')]/h3[not(@id='editorial')]");
            var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            foreach (var link in linksInDigest)
            {
                var title = HttpUtility.HtmlDecode(link.InnerText);
                                
                var sibling = link.NextSibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");

                //copying nodes related to the current link to a new abstract node
                while (sibling != null && sibling.Name.ToUpper() != "H3" && sibling.Name.ToUpper() != "H2" && sibling.GetAttributeValue("class", "not found").ToLower() != "promo-newsletter--newsletter")
                {
                    descriptionNode.AppendChild(sibling.Clone());
                    //siblingTextSb.AppendLine(sibling.InnerHtml);
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

                var href = descriptionNode.SelectSingleNode(".//a")?.GetAttributeValue("href", "Not found");
                if (href == null) throw new NullReferenceException();
                if (!href.Contains("://") && href.Contains("/"))
                {
                    var digestUrl = new Uri(digest.DigestURL);
                    var digestBase = new Uri(digestUrl.Scheme + "://" + digestUrl.Authority);
                    href = (new Uri(digestBase, href)).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionNode.InnerHtml.Trim(),
                    Digest = digest
                });
            }
            return links;
        }
    }
}
