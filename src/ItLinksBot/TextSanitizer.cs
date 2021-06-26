using System.Text.RegularExpressions;

namespace ItLinksBot
{
    public class TextSanitizer : ITextSanitizer
    {
        public string Sanitize(string rawText)
        {
            var normalizedDescription = rawText.Replace("<br>", "\n").Trim();
            normalizedDescription = Regex.Replace(normalizedDescription, "( )\\1+", "$1", RegexOptions.Singleline);
            normalizedDescription = normalizedDescription.Replace("\t", "");
            normalizedDescription = normalizedDescription.Replace("\r", "");
            normalizedDescription = Regex.Replace(normalizedDescription, @"[ ]+\n", "\n", RegexOptions.Singleline);
            normalizedDescription = Regex.Replace(normalizedDescription, @"(?:&nbsp;){2,}", "&nbsp;", RegexOptions.Singleline);
            normalizedDescription = Regex.Replace(normalizedDescription, @"^&nbsp;", "", RegexOptions.Singleline);
            normalizedDescription = Regex.Replace(normalizedDescription, @"\n&nbsp;", "\n", RegexOptions.Singleline);
            normalizedDescription = Regex.Replace(normalizedDescription, @"[\n]{3,}", "\n\n", RegexOptions.Singleline);
            return normalizedDescription;
        }
    }
}