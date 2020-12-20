using System;
using Microsoft.EntityFrameworkCore;
using ItLinksBot.Data;
using ItLinksBot.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ItLinksBot
{
    public interface IParser
    {
        void GetCurrentDigests(out List<Digest> digests, out List<Link> links);
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
            var optionsBuilder = new DbContextOptionsBuilder<ITLinksContext>();
            string connectionString = string.Format("Data Source={0}", config["DBName"]);
            optionsBuilder.UseSqlite(connectionString);
            var context = new ITLinksContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
            TelegramAPI bot = new TelegramAPI(config["BotApiKey"]);
            while (true)
            {
                foreach (Provider prov in context.Providers)
                {
                    List<Digest> digests = new List<Digest>();
                    List<Link> links = new List<Link>();
                    var parser = ParserFactory.Setup(prov);
                    parser.GetCurrentDigests(out digests, out links);
                    //saving links to entities
                    context.Digests.AddRange(digests.Except(context.Digests, new DigestComparer()));
                    context.Links.AddRange(links.Except(context.Links, new LinkComparer()));
                }
                //persisting entities change
                context.SaveChanges();

                bool botTimeout;
                do
                {
                    botTimeout = false;
                    foreach (TelegramChannel tgChannel in context.TelegramChannels)
                    {
                        //Finishing any unfinished previouslt digests
                        var unfinishedLinks = context.Links.Where(l => l.Digest == context.DigestPosts.OrderBy(d => d.PostDate).Last().Digest && !context.LinkPosts.Select(lp => lp.Link).Contains(l));
                        foreach (var unfinishedLink in unfinishedLinks)
                        {
                            LinkPost linkPost = QueueProcessor.AddLinkPost(tgChannel, unfinishedLink, bot);
                            if (linkPost != null)
                            {
                                context.LinkPosts.Add(linkPost);
                            }
                            else
                            {
                                botTimeout = true;
                                break;
                            }
                        }
                        if (botTimeout)
                        {
                            Console.WriteLine("Waiting 2 minutes");
                            System.Threading.Thread.Sleep(1000 * 60 * 2);
                            break;
                        }
                        //Posting new digests, not posted yet
                        var digests = context.Digests.Where(d => d.Provider == tgChannel.Provider && !context.DigestPosts.Select(dp => dp.Digest).Contains(d)).OrderBy(d => d.DigestDay);
                        foreach (Digest digest in digests)
                        {
                            DigestPost digestPost = QueueProcessor.AddDigestPost(tgChannel, digest, bot);
                            if(digestPost != null)
                            {
                                context.DigestPosts.Add(digestPost);
                            }
                            else
                            {
                                botTimeout = true;
                                break;
                            }

                            var links = context.Links.Where(l => l.Digest == digest);
                            foreach (var link in links)
                            {
                                LinkPost linkPost = QueueProcessor.AddLinkPost(tgChannel, link, bot);
                                if (linkPost != null)
                                {
                                    context.LinkPosts.Add(linkPost);
                                }
                                else
                                {
                                    botTimeout = true;
                                    break;
                                }
                            }
                            if (botTimeout)
                            {
                                break;
                            }
                        }
                        if (botTimeout) 
                        {
                            context.SaveChanges();
                            Console.WriteLine("Waiting 2 minutes");
                            System.Threading.Thread.Sleep(1000 * 60 * 2);
                            break; 
                        }
                    }
                    
                } while (botTimeout);
                context.SaveChanges();
                Console.WriteLine("Waiting 60 minutes");
                System.Threading.Thread.Sleep(1000 * 60 * 60);
            }
        }

    }
}