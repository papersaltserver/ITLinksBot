using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot
{
    public class DomNormlizer : IContentNormalizer
    {
        private string[] acceptableTags = new string[] { "strong", "em", "u", "b", "i", "a", "ins", "s", "strike", "del", "code", "pre" };
        private string[] nodesProhibited = new string[] { "style", "script" };
        private string[] nodesNewLines = new string[] { "div", "p", "h1", "h2", "h3", "h4", "li", "td" };
        public HtmlNode NormalizeDom(HtmlNode originalNode, string[] accptableTags, string[] nodesProhibited, string[] nodesNewLines)
        {
            acceptableTags = accptableTags;
            this.nodesProhibited = nodesProhibited;
            this.nodesNewLines = nodesNewLines;
            return NormalizeDom(originalNode);
        }
        public HtmlNode NormalizeDom(HtmlNode originalNode, string[] accptableTags)
        {
            acceptableTags = accptableTags;
            return NormalizeDom(originalNode);
        }
        public HtmlNode NormalizeDom(HtmlNode originalNode)
        {
            if (originalNode == null) return null;
            var normalizedNode = HtmlNode.CreateNode("<div></div>");
            normalizedNode.AppendChild(originalNode.Clone());

            //removing all the tags not allowed by telegram
            var nodesToAnalyze = new Queue<HtmlNode>(normalizedNode.ChildNodes);
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
                    if (node.Name == "a")
                    {
                        var uselessAttributes = node.Attributes.Where(a => a.Name != "href").Select(a => a.Name).ToArray();
                        foreach (var attr in uselessAttributes)
                        {
                            node.Attributes.Remove(attr);
                        }
                        var currentHref = node.GetAttributeValue("href", "Not found");
                        var newHref = Utils.UnshortenLink(currentHref);
                        node.SetAttributeValue("href", newHref);
                    }
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
            return normalizedNode;
        }
    }
}