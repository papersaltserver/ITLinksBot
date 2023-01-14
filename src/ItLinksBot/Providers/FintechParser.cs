using HtmlAgilityPack;
using ItLinksBot.ContentGetters;
using ItLinksBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Quill.Delta;

namespace ItLinksBot.Providers
{
    class FintechParser : IParser
    {
        private readonly IContentGetter<string> htmlContentGetter;
        private readonly IContentNormalizer contentNormalizer;
        private readonly ITextSanitizer textSanitizer;
        public string CurrentProvider => "Fintech";
        readonly Uri baseUri = new("https://teal.api.tryletterhead.com/");

        public FintechParser(IContentGetter<string> cg, IContentNormalizer cn, ITextSanitizer ts)
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
            // Fintech uses mixed parts, which are not easy to split, so we're posting them as is without splitting by links
            return link.Description;
        }

        public List<Digest> GetCurrentDigests(Provider provider)
        {
            List<Digest> digests = new();
            var stringResult = htmlContentGetter.GetContent(provider.DigestURL);
            JArray latestIssues = JArray.Parse(stringResult);

            foreach (JObject digestNode in latestIssues.Take(5))
            {
                int digestId = (int)digestNode["id"];
                var digestUrl = new Uri(baseUri, $"https://teal.api.tryletterhead.com/api/v1/brands/330/channels/396/letters/{digestId}");
                var dateText = (string)digestNode["publicationDate"];
                var currentDigest = new Digest
                {
                    DigestDay = DateTime.Parse(dateText),
                    DigestName = (string)digestNode["title"],
                    DigestDescription = (string)digestNode["subtitle"],
                    DigestURL = digestUrl.AbsoluteUri,
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
            List<Link> links = new();
            var digestContent = htmlContentGetter.GetContent(digest.DigestURL);
            JObject digestObject = JObject.Parse(digestContent);
            JArray sections = JArray.Parse((string)digestObject["sections"]);
            int entries = 0;
            foreach (JObject section in sections)
            {
                JArray columns = (JArray)section["columns"];
                if (columns.Count == 0)
                {
                    continue;
                }
                // theoretically there could be multiple columns in one section, but practically it's always one
                foreach (JObject column in columns)
                {
                    JArray deltaOps = (JArray)column["delta"]["ops"];
                    // Letterhead uses Quill as their editor, so we could convert Quill DeltaOps to HTML
                    var htmlConverter = new HtmlConverter(deltaOps);
                    string html = htmlConverter.Convert();
                    var linksHtml = new HtmlDocument();
                    linksHtml.LoadHtml(html);
                    var descriptionNode = contentNormalizer.NormalizeDom(linksHtml.DocumentNode);
                    string normalizedDescription = textSanitizer.Sanitize(descriptionNode.InnerHtml.Trim());
                    if (normalizedDescription == "")
                    {
                        continue;
                    }
                    links.Add(new Link
                    {
                        URL = $"{digest.DigestURL}#section{entries}",
                        Title = "", // no specific title for sections
                        Description = normalizedDescription,
                        LinkOrder = entries,
                        Digest = digest
                    });
                    entries++;
                }
            }
            return links;
        }
    }
}
