using ItLinksBot;
using System;
using Xunit;

namespace ItLinksBot_Tests
{
    public class UtilsTest
    {
        [Theory]
        [InlineData(471999600)]
        public void UnixTimeStampToDateTimeTest(int timestamp)
        {
            Assert.Equal(new DateTime(1984, 12, 16, 0, 0, 0, DateTimeKind.Utc), Utils.UnixTimeStampToDateTime(timestamp));
        }
    }
}
