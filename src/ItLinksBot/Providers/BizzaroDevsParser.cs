using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.Providers
{
    class BizzaroDevsParser : IParser
    {
        public string CurrentProvider => "Bizarro Devs";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        readonly Uri baseUri = new("http://bizzarodevs.com/");
        public BizzaroDevsParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }

        public string FormatDigestPost(Digest digest)
        {
            return string.Format("<b>{0} - {1}</b>\n{2}\n{3}", digest.DigestName, digest.DigestDay.ToString("yyyy-MM-dd"), digest.DigestDescription, digest.DigestURL);
        }

        public string FormatLinkPost(Link link)
        {
            return string.Format("<strong>{0}</strong>\n\n{1}\n{2}", link.Title, link.Description, link.URL);
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            var digestArchiveHtml = new HtmlDocument();
            digestArchiveHtml.LoadHtml(stringResult);

            var digestsInArchive = digestArchiveHtml.DocumentNode.SelectNodes("//li[contains(@class,'item')]").Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //setting to 1900 to fill description later
                
                var hrefNode = digestNode.SelectSingleNode(".//a");
                var digestHref = hrefNode.GetAttributeValue("href", "Not found");
                string fullHref = new Uri(baseUri, digestHref).AbsoluteUri;

                var nameNode = digestNode.SelectSingleNode(".//h2[contains(@class,'heading')]");
                var digestName = nameNode.InnerText.Trim();

                var currentDigest = new Digest
                {
                    DigestDay = digestDate,
                    DigestName = digestName,
                    DigestDescription = "",
                    DigestURL = fullHref,
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
            
            var dateNode = digestDetails.DocumentNode.SelectSingleNode("//article[@id='start']//time[contains(@class,'published')]");
            var digestDate = DateTime.Parse(dateNode.GetAttributeValue("datetime", "not found"));
            
            var descriptionNodeOriginal = digestDetails.DocumentNode.SelectNodes("//div[@id='intro']/div/*[preceding-sibling::a[@name]][following-sibling::span[contains(@class,'item__footer')]]");
            string normalizedDescription;
            if (descriptionNodeOriginal != null)
            {
                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChildren(descriptionNodeOriginal);
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
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
            //article nodes will be parsed first
            var linksInDigest = linksHtml.DocumentNode.SelectNodes("//section[not(contains(@class,'cc-mustsees'))]//div[@id!='intro' and @id!='outro']//div[contains(@class,'item')]");
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                //gettig title
                HtmlNode link = linksInDigest[i];
                var titleNode = link.SelectSingleNode("./h3");
                var title = titleNode?.InnerText;
                //getting link
                var hrefNode = link.SelectSingleNode("./h3/a");
                if (hrefNode == null)
                {
                    hrefNode = link.SelectSingleNode(".//span[contains(@class,'item__footer-link')]/a[1]");
                }
                string href = hrefNode.GetAttributeValue("href", "Not found");
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = new Uri(baseUri, href).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                //getting description
                var contentNodes = link.SelectNodes("./*[preceding-sibling::h3][following-sibling::span[contains(@class,'item__footer')]]");
                if(contentNodes == null)
                {
                    contentNodes = link.SelectNodes("./*[preceding-sibling::a[@name]][following-sibling::span[contains(@class,'item__footer')]]");
                }
                string descriptionText;
                if (contentNodes != null)
                {
                    var descriptionNode = HtmlNode.CreateNode("<div></div>");
                    foreach(var node in contentNodes)
                    {
                        descriptionNode.AppendChild(node.Clone());
                    }

                    //descriptionNode.AppendChildren(contentCopy);
                    descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                    descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                }
                else
                {
                    descriptionText = "";
                    Log.Warning("Description for link {link} in Bizzaro Devs is empty",href);
                }

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = i,
                    Digest = digest
                });
            }
            int articleLinks = linksInDigest.Count;
            //Interesting links list parsing
            linksInDigest = linksHtml.DocumentNode.SelectNodes("//section[contains(@class,'cc-mustsees')]//div[@id!='intro' and @id!='outro']//div[contains(@class,'item')]/ol/li");
            if(linksInDigest == null)
            {
                linksInDigest = linksHtml.DocumentNode.SelectNodes("//section[contains(@class,'cc-mustsees')]//div[@id!='intro' and @id!='outro']//div[contains(@class,'item')]");
                Log.Information("Broken must see links section in Bizzaro Devs {digest}", digest.DigestURL);
            }
            for (int i = 0; i < linksInDigest.Count; i++)
            {
                //gettig title
                HtmlNode link = linksInDigest[i];
                string title = ""; //this section does not have titles
                //getting link
                var hrefNode = link.SelectSingleNode(".//a[not(@name)]");
                string href;
                if (hrefNode != null)
                {
                    href = hrefNode.GetAttributeValue("href", "Not found");
                }
                else               
                {
                    hrefNode = linksHtml.DocumentNode.SelectSingleNode("//section[contains(@class,'cc-mustsees')]//div[@id!='intro' and @id!='outro']//div[contains(@class,'item')]/span/span/a");
                    href = hrefNode.GetAttributeValue("href", "Not found") + "#link" + articleLinks + i;
                }
                if (!href.Contains("://") && href.Contains("/"))
                {
                    href = new Uri(baseUri, href).AbsoluteUri;
                }
                href = Utils.UnshortenLink(href);
                //getting description
                string descriptionText;

                var descriptionNode = HtmlNode.CreateNode("<div></div>");
                descriptionNode.AppendChild(link.Clone());
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = articleLinks+i,
                    Digest = digest
                });
            }

            return links;
        }
    }
}
