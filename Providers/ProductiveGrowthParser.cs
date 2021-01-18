﻿using HtmlAgilityPack;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Serilog;

namespace ItLinksBot.Providers
{
    class ProductiveGrowthParser : IParser
    {
        private readonly Provider _digest;
        readonly Uri baseUri = new Uri("https://productivegrowth.substack.com/");

        public ProductiveGrowthParser(Provider provider)
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
            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//div[contains(@class,'post-preview-content')]").Take(50);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //hack to fill description later
                var hrefNode = digestNode.SelectSingleNode("./a[1]");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                var digestName = hrefNode.InnerText.Trim();
                var digestUrl = new Uri(baseUri, digestHref);

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName, //will be added during next request
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
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//div[@id='main']//script");
            var dateText = Regex.Match(dateNode.InnerText, "\"datePublished\".*?\"(.*?)\"").Groups[1].Value;
            var digestDate = DateTime.Parse(dateText);

            var sibling = digestDetails.DocumentNode.SelectSingleNode("//div[contains(@class,'body')]/h3[1]").NextSibling;
            var descriptionNode = HtmlNode.CreateNode("<div></div>");

            //copying nodes related to the current link to a new abstract node
            while (sibling != null 
                && sibling.Name.ToUpper() != "H3" 
                //&& sibling.Name.ToUpper() != "DIV" 
                && !sibling.ChildNodes.Where(ch => ch.Name.ToUpper() == "FORM" || ch.Name.ToUpper() == "HR").Any()
                && !sibling.InnerText.ToUpper().Contains("HAVE FEEDBACK?"))
            {
                descriptionNode.AppendChild(sibling.Clone());
                sibling = sibling.NextSibling;
            }

            //removing all the tags not allowed by telegram
            var acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            var nodesProhibited = new string[] { "style", "script" };
            var nodesNewLines = new string[] { "div", "p", "h1", "h2", "h3", "h4" };
            var nodesToAnalyze = new Queue<HtmlNode>(descriptionNode.ChildNodes);
            while (nodesToAnalyze.Count > 0)
            {
                var node = nodesToAnalyze.Dequeue();
                var parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null && !nodesProhibited.Contains(node.Name))
                    {
                        foreach (var child in childNodes)
                        {
                            nodesToAnalyze.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }
                    if (nodesNewLines.Contains(node.Name))
                    {
                        parentNode.InsertBefore(HtmlNode.CreateNode("<br>"), node);
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
            var normalizedDescription = descriptionNode.InnerHtml.Replace("<br>", "\n").Trim();
            normalizedDescription = Regex.Replace(normalizedDescription, "( )\\1+", "$1", RegexOptions.Singleline);
            normalizedDescription = normalizedDescription.Replace("\t", "");
            normalizedDescription = normalizedDescription.Replace("\r", "");
            normalizedDescription = Regex.Replace(normalizedDescription, @"[\n]{3,}", "\n\n", RegexOptions.Singleline);


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
            var digestSections = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'body')]/p[em[contains(text(),'First time')]]//following-sibling::div[hr]");
            if (digestSections == null)
            {
                //if digest contains of just 1 section
                digestSections = linksHtml.DocumentNode.SelectNodes("//div[contains(@class,'body')]");
            }
            if (digestSections == null)
            {
                Log.Error("Digest '{digest}' beginning in Product Growth could not be found. Digest id {id}", digest.DigestName, digest.DigestId);
                throw new NullReferenceException();
            }
            var acceptableTags = new String[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
            var nodesProhibited = new string[] { "style", "script" };
            var nodesNewLines = new string[] { "div", "p", "h1", "h2", "h3", "h4" };
            for (int i = 0; i < digestSections.Count; i++)
            {
                HtmlNode section = (HtmlNode)digestSections[i];
                HtmlNode sibling;
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                if (digestSections.Count == 1)
                {
                    sibling = section;
                }
                else
                {
                    sibling = section.NextSibling;
                    while (sibling != null && (!(sibling.Name.ToUpper() == "DIV") || !sibling.ChildNodes.Where(ch => ch.Name.ToUpper() == "HR").Any()))
                    {
                        descriptionNode.AppendChild(sibling.Clone());
                        sibling = sibling.NextSibling;
                    }
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

                        if (childNodes != null && !nodesProhibited.Contains(node.Name))
                        {
                            foreach (var child in childNodes)
                            {
                                nodesToAnalyze.Enqueue(child);
                                parentNode.InsertBefore(child, node);
                            }
                        }
                        if (nodesNewLines.Contains(node.Name))
                        {
                            parentNode.InsertBefore(HtmlNode.CreateNode("<br>"), node);
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
                var normalizedDescription = descriptionNode.InnerHtml.Replace("<br>", "\n").Trim();
                normalizedDescription = Regex.Replace(normalizedDescription, "( )\\1+", "$1", RegexOptions.Singleline);
                normalizedDescription = normalizedDescription.Replace("\t", "");
                normalizedDescription = normalizedDescription.Replace("\r", "");
                normalizedDescription = Regex.Replace(normalizedDescription, @"[\n]{3,}", "\n\n", RegexOptions.Singleline);
                if (normalizedDescription.Contains("What did you think of this issue?")
                    || normalizedDescription.Contains("Write a guest post, share your story")
                    || normalizedDescription.Contains("See you next week")
                    || normalizedDescription == "") continue;

                var href = $"{digest.DigestURL}#section{i}"; //href is fake there

                links.Add(new Link
                {
                    URL = href,
                    Title = "", //in this digest we post whole sections and not every has caption
                    Description = normalizedDescription,
                    LinkOrder = i,
                    Digest = digest
                });
            }

            

            return links;
        }
    }
}
