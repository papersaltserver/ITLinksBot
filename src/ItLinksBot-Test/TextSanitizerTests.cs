using Xunit;
using ItLinksBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot_Tests
{
    public class TextSanitizerTests
    {
        [Fact()]
        public void SanitizeTrimTest()
        {
            string text = @"

";
            string expectedText = @"";
            TextSanitizer sanitizer = new();
            Assert.Equal(sanitizer.Sanitize(text), expectedText);
        }
    }
}