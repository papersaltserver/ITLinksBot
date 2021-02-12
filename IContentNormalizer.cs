using HtmlAgilityPack;
namespace ItLinksBot
{
    public interface IContentNormalizer
    {
        public HtmlNode NormalizeDom(HtmlNode originalNode, string[] accptableTags, string[] nodesProhibited, string[] nodesNewLines);
        public HtmlNode NormalizeDom(HtmlNode originalNode, string[] accptableTags);
        public HtmlNode NormalizeDom(HtmlNode originalNode);
    }
}