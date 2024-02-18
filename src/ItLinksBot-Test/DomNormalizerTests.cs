using HtmlAgilityPack;
using ItLinksBot;
using Xunit;

namespace ItLinksBot_Tests
{
    public class DomNormalizerTests
    {
        [Fact]
        public void NormalizeDom_ReplacesEmptyAnchorTagsWithTextNodes()
        {
            // Arrange
            var html = "<div><a href=\"https://example.com/\"></a></div>";
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            DomNormlizer normlizer = new();
            // Act
            var normalizedNode = normlizer.NormalizeDom(doc.DocumentNode);

            // Assert
            var anchorNode = normalizedNode.SelectSingleNode("//a");
            Assert.Null(anchorNode); // The <a> tag should be replaced with a text node

            // Verify that the text node contains the href attribute value
            var textNode = normalizedNode.SelectSingleNode("//text()");
            Assert.NotNull(textNode);
            Assert.Equal("https://example.com/", textNode.InnerHtml);
        }

        [Fact]
        public void NormalizeDom_ReplacesNontextAnchorTagsWithTextNodes()
        {
            // Arrange
            var html = "<div><a href=\"https://example.com/\"><img src=\"https://example.com/image.jpg\" alt=\"Description of the image\" width=\"300\" height=\"200\">\r\n</a></div>";
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            DomNormlizer normlizer = new();
            // Act
            var normalizedNode = normlizer.NormalizeDom(doc.DocumentNode);

            // Assert
            var anchorNode = normalizedNode.SelectSingleNode("//a");
            Assert.Null(anchorNode); // The <a> tag should be replaced with a text node

            // Verify that the text node contains the href attribute value
            var textNode = normalizedNode.SelectSingleNode("//text()");
            Assert.NotNull(textNode);
            Assert.Equal("https://example.com/", textNode.InnerHtml);
        }

        [Fact]
        public void NormalizeDom_DoesNotThrowForNodeWithoutAnchorTags()
        {
            // Arrange
            var html = "<div><p>This is some content.</p></div>"; // Example HTML without <a> tags
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Act & Assert
            DomNormlizer normlizer = new();
            var exception = Record.Exception(() => normlizer.NormalizeDom(doc.DocumentNode));
            Assert.Null(exception);
        }
    }
}
