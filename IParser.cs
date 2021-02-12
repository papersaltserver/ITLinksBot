using ItLinksBot.Models;
using System.Collections.Generic;

namespace ItLinksBot
{
    public interface IParser
    {
        string CurrentProvider { get; }
        List<Digest> GetCurrentDigests(Provider provider);
        Digest GetDigestDetails(Digest digest);
        List<Link> GetDigestLinks(Digest digest);
        string FormatDigestPost(Digest digest);
        string FormatLinkPost(Link link);
    }
}