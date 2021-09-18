using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ItLinksBot.Providers
{
    class DenseDiscoveryParser : IParser
    {
        public string CurrentProvider => "Dense Discovery";
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        //readonly Uri baseUri = new("https://www.densediscovery.com/");
        public DenseDiscoveryParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);

            var digestsInArchive = feed.Items.Take(5);
            foreach (var digestNode in digestsInArchive)
            {
                var digestDate = new DateTime(1900, 1, 1); //we'll fill it later
                var digestName = digestNode.Title.Text;
                var fullHref = digestNode.Links[0].Uri.AbsoluteUri; ;

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
            //Initial link leading to a decorated page with iframe, let's get actual link
            string stubContent = htmlContentGetter.GetContent(digest.DigestURL);
            HtmlDocument stubDocument = new();
            stubDocument.LoadHtml(stubContent);
            HtmlNode iframeNode = stubDocument.DocumentNode.SelectSingleNode("//iframe[@id='iframe']");
            string realLink = iframeNode.GetAttributeValue("src", "not found");

            //getting real content
            string digestContent = htmlContentGetter.GetContent(realLink);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);

            //getting description of the digest
            HtmlNodeCollection descriptionNodes = digestDocument.DocumentNode.SelectNodes("//tr[preceding-sibling::comment()[contains(.,' INTRO Start ')]][following-sibling::comment()[contains(.,' INTRO End ')]]");
            var descriptionNode = HtmlNode.CreateNode("<div></div>");
            descriptionNode.AppendChildren(descriptionNodes);
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
            //Initial link leading to a decorated page with iframe, let's get actual link
            string stubContent = htmlContentGetter.GetContent(digest.DigestURL);
            HtmlDocument stubDocument = new();
            stubDocument.LoadHtml(stubContent);
            HtmlNode iframeNode = stubDocument.DocumentNode.SelectSingleNode("//iframe[@id='iframe']");
            string realLink = iframeNode.GetAttributeValue("src", "not found");
            var baseUri = new Uri(realLink);

            //getting real content
            string digestContent = htmlContentGetter.GetContent(realLink);
            HtmlDocument digestDocument = new();
            digestDocument.LoadHtml(digestContent);

            //Apps & Sites
            var appsSitesLinks = digestDocument.DocumentNode.SelectNodes("//tr[preceding-sibling::comment()[contains(.,'DIGITAL Start')]][following-sibling::comment()[contains(.,'DIGITAL End')]]/td[not(contains(@class,'spacer') or contains(@class,'headline'))]");
            int linkPosition = 0;
            for (int i = 0; i < appsSitesLinks.Count; i++)
            {
                HtmlNode link = appsSitesLinks[i];
                HtmlNode hrefNode = link.SelectSingleNode(".//h2/a");
                string href = hrefNode.GetAttributeValue("href", "not found");
                string title = hrefNode.InnerText;
                HtmlNode descriptionNode = link.SelectSingleNode(".//p");
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = linkPosition,
                    Digest = digest
                });
                linkPosition++;
            }
            //Worthy Five
            HtmlNode worthyFiveNode = digestDocument.DocumentNode.SelectSingleNode("//tr[preceding-sibling::comment()[contains(.,'BOX WORTHY Start')]][following-sibling::comment()[contains(.,'BOX WORTHY End')]]/td[not(contains(@class,'spacer') or contains(@class,'headline'))]");
            string worthyFiveTitle = worthyFiveNode.SelectSingleNode(".//table[1]//p[2]").InnerText;
            HtmlNode worthyFiveDescNode = worthyFiveNode.SelectSingleNode(".//table[2]");
            worthyFiveDescNode = contentNormalizer.NormalizeDom(worthyFiveDescNode);
            var worthyFiveImageLink = new Uri(baseUri, "worthy-five.jpg").AbsoluteUri;
            string worthyFiveDescText = worthyFiveImageLink + "\n" + textSanitizer.Sanitize(worthyFiveDescNode.InnerHtml.Trim());
            links.Add(new Link
            {
                URL = worthyFiveImageLink,
                Title = worthyFiveTitle,
                Description = worthyFiveDescText,
                LinkOrder = linkPosition,
                Digest = digest
            });
            linkPosition++;

            //Books & Accessories
            HtmlNodeCollection bookNodes = digestDocument.DocumentNode.SelectNodes("//tr[preceding-sibling::comment()[contains(.,'ACCESSORIES Start')]][following-sibling::comment()[contains(.,'ACCESSORIES End')]]/td[not(contains(@class,'spacer') or contains(@class,'headline'))]");
            for (int i = 0; i < bookNodes.Count; i++)
            {
                HtmlNode link = bookNodes[i];
                HtmlNode hrefNode = link.SelectSingleNode(".//h2/a");
                string href = hrefNode.GetAttributeValue("href", "not found");
                string title = hrefNode.InnerText;
                HtmlNode descriptionNode = link.SelectSingleNode("./table[2]//p");
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = linkPosition,
                    Digest = digest
                });
                linkPosition++;
            }

            //Overheard on Twitter + Food For Thought
            HtmlNodeCollection foodNodes = digestDocument.DocumentNode.SelectNodes("//tr[preceding-sibling::comment()[contains(.,'BOX TWEET Start')]][following-sibling::comment()[contains(.,'FOOD FOR THOUGHT End')]]/td[not(contains(@class,'spacer') or contains(@class,'headline'))]");
            for (int i = 0; i < foodNodes.Count; i++)
            {
                HtmlNode link = foodNodes[i];
                HtmlNode hrefNode = link.SelectSingleNode(".//h2/a");
                string href = hrefNode.GetAttributeValue("href", "not found");
                string title = hrefNode.InnerText;
                HtmlNode descriptionNode = link.SelectSingleNode(".//p");
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());

                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = linkPosition,
                    Digest = digest
                });
                linkPosition++;
            }

            //Aesthetically Pleasing
            HtmlNodeCollection aestheticNodes = digestDocument.DocumentNode.SelectNodes("//tr[preceding-sibling::comment()[contains(.,'BOX VISUAL INSP Start')]][following-sibling::comment()[contains(.,'BOX VISUAL INSP End')]]/td[not(contains(@class,'spacer') or contains(@class,'headline'))]");
            for (int i = 0; i < aestheticNodes.Count; i++)
            {
                HtmlNode link = aestheticNodes[i];
                HtmlNode hrefNode = link.SelectSingleNode("./table[2]//p//b/..");
                string href = hrefNode.GetAttributeValue("href", "not found");
                string title = hrefNode.InnerText;
                HtmlNode descriptionNode = link.SelectSingleNode("./table[2]//p");
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                string descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                links.Add(new Link
                {
                    URL = href,
                    Title = title,
                    Description = descriptionText,
                    LinkOrder = linkPosition,
                    Digest = digest
                });
                linkPosition++;

                //images for this link
                HtmlNodeCollection images = link.SelectNodes(".//img");
                for (int j = 0; j < images.Count; j++)
                {
                    HtmlNode img = images[j];
                    string imgHref = img.GetAttributeValue("src", "not found");
                    imgHref = new Uri(new Uri(realLink), imgHref).AbsoluteUri;
                    links.Add(new Link
                    {
                        URL = imgHref,
                        Title = "",
                        Description = "",
                        LinkOrder = linkPosition,
                        Digest = digest
                    });
                    linkPosition++;
                }

            }
            //The Week in a GIF
            
            var gifImageLink = new Uri(baseUri, "gif.gif").AbsoluteUri;
            links.Add(new Link
            {
                URL = gifImageLink,
                Title = "The Week in a GIF",
                Description = "",
                LinkOrder = linkPosition,
                Digest = digest
            });
            return links;
        }
    }
}
