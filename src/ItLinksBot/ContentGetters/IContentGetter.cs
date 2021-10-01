namespace ItLinksBot.ContentGetters
{
    public interface IContentGetter<out T>
    {
        T GetContent(string resourceUrl);
    }
}
