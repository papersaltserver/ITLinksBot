using System;
using Microsoft.EntityFrameworkCore;
using ItLinksBot.Data;
using ItLinksBot.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using ItLinksBot.Providers;
using Serilog;
using System.Net;

namespace ItLinksBot
{
    public interface IParser
    {
        List<Digest> GetCurrentDigests();
        Digest GetDigestDetails(Digest digest);
        List<Link> GetDigestLinks(Digest digest);
        string FormatDigestPost(Digest digest);
        string FormatLinkPost(Link link);
    }
    public static class ParserFactory
    {
        public static IParser Setup(Provider provider)
        {
            return provider.ProviderName switch
            {
                "O'Reily Four Short Links" => new Oreily4ShortLinksParser(provider),
                "Changelog Weekly" => new ChangelogParser(provider),
                "TLDR" => new TLDRparser(provider),
                "React Newsletter" => new ReactNewsletterParser(provider),
                "JavaScript Weekly" => new JavaScriptWeeklyParser(provider),
                "Smashing Email Newsletter" => new SmashingEmailParser(provider),
                "Dev Awesome" => new DevAwesomeParser(provider),
                "CSS Weekly" => new CssWeeklyParser(provider),
                "programming digest" => new ProgrammingDigestParser(provider),
                "c# digest" => new CSharpDigestParser(provider),
                "DB Weekly" => new DBWeeklyParser(provider),
                "StatusCode Weekly" => new StatusCodeWeeklyParser(provider),
                "Awesome SysAdmin Newsletter" => new AwesomeSysAdminParser(provider),
                "SRE Weekly" => new SREWeeklyParser(provider),
                "Inside Cryptocurrency" => new InsideCryptocurrencyParser(provider),
                "Better Dev Link" => new BetterDevLinkParser(provider),
                "Data Is Plural" => new DataIsPluralParser(provider),
                "Software Lead Weekly" => new SoftwareLeadWeeklyParser(provider),
                "Tech Productivity" => new TechProductivityParser(provider),
                "Artificial Intelligence Weekly" => new ArtificialIntelligenceParser(provider),
                _ => throw new NotImplementedException(),
            };
        }
    }
    public static class Utils
    {
        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static string UnshortenLink(string linkUrl)
        {
            HttpWebRequest req;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(linkUrl);
            }
            catch (Exception)
            {
                Log.Warning("Malformed URL {url}", linkUrl);
                return linkUrl;
            }
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75";
            req.AllowAutoRedirect = false;
            string realUrl = linkUrl;
            while (true)
            {
                try
                {
                    var resp = (HttpWebResponse)req.GetResponse();
                    if (resp.StatusCode == HttpStatusCode.Ambiguous ||
                    resp.StatusCode == HttpStatusCode.MovedPermanently ||
                    resp.StatusCode == HttpStatusCode.Found ||
                    resp.StatusCode == HttpStatusCode.RedirectMethod ||
                    resp.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        if (!resp.Headers["Location"].Contains("://"))
                        {
                            //var baseRedirUri = new Uri(req.RequestUri.Scheme + "://" + req.RequestUri.Authority);
                            realUrl = (new Uri(req.RequestUri, resp.Headers["Location"])).AbsoluteUri;
                            if (realUrl == req.RequestUri.AbsoluteUri)
                            {
                                break;
                            }
                        }
                        else
                        {
                            realUrl = resp.Headers["Location"];
                            if(realUrl == req.RequestUri.AbsoluteUri)
                            {
                                break;
                            }
                        }
                        req = (HttpWebRequest)WebRequest.Create(realUrl);
                        req.AllowAutoRedirect = false;
                        req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75";
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Problem {exception} with link {original} which leads to {realUrl} ", e.Message, linkUrl, realUrl);
                    break;
                }
                
            }
            return realUrl;
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
#if DEBUG
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.debug.json",
                             optional: true,
                             reloadOnChange: true)
                .Build();
#else
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json",
                             optional: true,
                             reloadOnChange: true)
                .Build();
#endif
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            Log.Information("Started bot");
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();

            var connectionString = config
                        .GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlite(connectionString);
            var context = new ITLinksContext();
            context.Database.Migrate();
            TelegramAPI bot = new TelegramAPI(config["BotApiKey"]);
            while (true)
            {
                var activeProviders = context.Providers.Where(pr => pr.ProviderEnabled);
                foreach (Provider prov in activeProviders)
                {
                    var parser = ParserFactory.Setup(prov);
                    List<Digest> digests = parser.GetCurrentDigests();
                    //saving digests to entities
                    var newDigests = digests.Except(context.Digests, new DigestComparer());
                    Log.Information($"Found {newDigests.Count()} new digests for newsletter {prov.ProviderName}");
                    //parse digests which do not have info in digest itself
                    if (newDigests.Any() && newDigests.First().DigestDay == new DateTime(1900, 1, 1))
                    {
                        var tempDigests = new List<Digest>();
                        foreach(var digest in newDigests)
                        {
                            tempDigests.Add(parser.GetDigestDetails(digest));
                        }
                        newDigests = tempDigests;
                    }
                    //context.Digests.AddRange(newDigests);
                    

                    //getting and saving only new links to entities
                    if (newDigests.Any())
                    {
                        int totalLinks = 0;
                        foreach (var dgst in newDigests)
                        {
                            //List<Link> links = new List<Link>();
                            List<Link> linksInCurrentDigest = parser.GetDigestLinks(dgst);
                            //links.AddRange(linksInCurrentDigest);
                            var newLinks = linksInCurrentDigest.Except(context.Links, new LinkComparer());
                            context.Digests.Add(dgst);
                            context.Links.AddRange(newLinks);
                            Log.Information($"Found {newLinks.Count()} new links for newsletter {prov.ProviderName} in digest {dgst.DigestName}");
                            //persisting entities change
                            context.SaveChanges();
                            totalLinks += 1 + linksInCurrentDigest.Count;
                        }
                        Log.Information($"Total number of objects to post: {totalLinks}");
                    }
                }

                bool botTimeout;
                do
                {
                    botTimeout = false;
                    foreach (TelegramChannel tgChannel in context.TelegramChannels)
                    {
                        //Posting new digests, not posted yet
                        var digests = context.Digests.Where(d => d.Provider == tgChannel.Provider && !context.DigestPosts.Select(dp => dp.Digest).Contains(d)).OrderBy(d => d.DigestDay);
                        if (digests.Any()) Log.Information($"Found {digests.Count()} new digests to post in {tgChannel.ChannelName}");
                        foreach (Digest digest in digests)
                        {
                            List<DigestPost> digestPost = QueueProcessor.AddDigestPost(tgChannel, digest, bot);
                            context.DigestPosts.AddRange(digestPost);

                            var links = context.Links.Where(l => l.Digest == digest);
                            foreach (var link in links)
                            {
                                List<LinkPost> linkPost = QueueProcessor.AddLinkPost(tgChannel, link, bot);
                                context.LinkPosts.AddRange(linkPost);
                            }
                            //save after each successfull digest post session
                            context.SaveChanges();
                        }
                    }
                } while (botTimeout);
                context.SaveChanges();
                Log.Information("Nothing to post. Sleeping for 1 hour");
                System.Threading.Thread.Sleep(1000 * 60 * 60);
            }
        }

    }
}