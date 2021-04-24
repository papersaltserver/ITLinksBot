using ItLinksBot;
using System;
using System.Collections.Generic;
using Xunit;

namespace ItLinksBot_Tests
{
    public class UtilsTest
    {
        public static IEnumerable<object[]> GetUnixTimeStampToDateTimeTestData()
        {
            var allData = new List<object[]>
        {
            new object[] { 471999600, new DateTime(1984, 12, 15, 23, 0, 0, DateTimeKind.Utc).ToLocalTime() },
            new object[] { int.MinValue, new DateTime(1901, 12, 13, 20, 45, 52, DateTimeKind.Utc).ToLocalTime() },
            new object[] { int.MaxValue, new DateTime(2038, 01, 19, 3, 14, 7, DateTimeKind.Utc).ToLocalTime() },
            new object[] { 0, new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToLocalTime() },
        };

            return allData;
        }

        [Theory]
        [MemberData(nameof(GetUnixTimeStampToDateTimeTestData))]
        public void UnixTimeStampToDateTimeTest(int timestamp, DateTime expectedDate)
        {
            Assert.Equal(expectedDate, Utils.UnixTimeStampToDateTime(timestamp));
        }

        [Theory()]
        [InlineData("https://www.google.com/", "https://www.google.com/")]
        [InlineData("https://techcrunch.com/2021/03/23/y-combinators-new-batch-features-its-largest-group-of-indian-startups/", "https://techcrunch.com/2021/03/23/y-combinators-new-batch-features-its-largest-group-of-indian-startups/")]
        [InlineData("https://google.com", "https://www.google.com/")]
        [InlineData("https://javascriptweekly.com/link/105046/web", "https://www.jackfranklin.co.uk/blog/comparing-svelte-and-react-javascript/")]
        [InlineData("", "")]
        [InlineData("xyz", "xyz")]
        public void UnshortenLinkTest(string url, string realUrl)
        {
            Assert.Equal(realUrl, Utils.UnshortenLink(url));
        }
    }
}
