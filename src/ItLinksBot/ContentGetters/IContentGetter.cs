namespace ItLinksBot.ContentGetters
{
    public interface IContentGetter<out T>
    {
        //string ContentType { get; }
        T GetContent(string resourceUrl);
    }
}
