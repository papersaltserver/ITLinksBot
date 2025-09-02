using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ItLinksBot.Providers
{
    class WroclawPlDlaMieszkancaParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Wroclaw.pl-dla-mieszkanca";
        readonly Uri baseUri = new("https://www.wroclaw.pl/");
        public WroclawPlDlaMieszkancaParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
        {
            htmlContentGetter = cg;
            contentNormalizer = cn;
            textSanitizer = ts;
        }
        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            XmlReader reader = XmlReader.Create(new StringReader(stringResult));
            var feed = SyndicationFeed.Load(reader);
            foreach (var feedItem in feed.Items.Take(25))
            {
                var descriptionNode = HtmlNode.CreateNode($"<div>{feedItem.Summary.Text}</div>");
                descriptionNode = contentNormalizer.NormalizeDom(descriptionNode);
                var descriptionText = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                Digest currentDigest = new()
                {
                    DigestDay = feedItem.PublishDate.DateTime,
                    DigestName = feedItem.Title.Text,
                    DigestDescription = descriptionText,
                    DigestURL = feedItem.Links[0].Uri.AbsoluteUri,
                    Provider = provider
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
            List<Link> links = [];
            return links;
        }
        public string FormatDigestPost(Digest digest)
        {
            return $"<b>[{digest.DigestDay:yyyy-MM-dd HH:mm}] {digest.DigestName}</b>\n{digest.DigestDescription}\n{digest.DigestURL}";
        }
        public string FormatLinkPost(Link link)
        {
            // This provider does not have links
            return "";
        }
    }
}
