using System.Collections.Generic;

namespace ItLinksBot.ContentGetters
{
    public interface IContentGetter<out T>
    {
        T GetContent(string resourceUrl, Dictionary<string, string> requestHeaders = null);
    }
}
