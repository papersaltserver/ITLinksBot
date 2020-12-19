using System;
using Microsoft.EntityFrameworkCore;
using ItLinksBot.Data;
using ItLinksBot.Models;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
    public class Utils
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
                .AddJsonFile("appsettings.debug.json", true, true)
                .Build();
#else
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
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
                //re-create with do until
                bool childError = false;
                do
                {
                    foreach (TelegramChannel tg in context.TelegramChannels)
                    {

                        var parser = ParserFactory.Setup(tg.Provider);
                        //Finishing any unfinished previouslt digests
                        var unfinishedLinks = context.Links.Where(l => l.Digest == context.DigestPosts.OrderBy(d => d.PostDate).Last().Digest && !context.LinkPosts.Select(lp => lp.Link).Contains(l));
                        foreach (var unfinishedLink in unfinishedLinks)
                        {
                            var linkResult = bot.SendMessage(tg.ChannelName, parser.FormatLinkPost(unfinishedLink));
                            var linkStatus = JObject.Parse(linkResult);
                            if ((bool)linkStatus["ok"])
                            {
                                context.LinkPosts.Add(
                                    new LinkPost
                                    {
                                        Channel = tg,
                                        Link = unfinishedLink,
                                        TelegramMessageID = (int)linkStatus["result"]["message_id"],
                                        PostDate = Utils.UnixTimeStampToDateTime((int)linkStatus["result"]["date"]),
                                        PostLink = string.Format("https://t.me/{0}/{1}", (string)linkStatus["result"]["chat"]["username"], (string)linkStatus["result"]["message_id"]),
                                        PostText = (string)linkStatus["result"]["text"]
                                    }
                                    );
                                childError = false;
                            }
                            else if ((int)linkStatus["error_code"] == 429)
                            {
                                //"Too Many Requests"
                                childError = true;
                                break;
                            }
                            else
                            {
                                childError = true;
                                Console.WriteLine(linkResult);
                            }
                        }
                        if (childError)
                        {
                            System.Threading.Thread.Sleep(1000 * 60 * 2);
                            Console.WriteLine("Waiting 2 minutes");
                            break;
                        }
                        //Posting new digests
                        var digests = context.Digests.Where(d => d.Provider == tg.Provider && !context.DigestPosts.Select(dp => dp.Digest).Contains(d)).OrderBy(d => d.DigestDay);
                        foreach (Digest digest in digests)
                        {
                            var result = bot.SendMessage(tg.ChannelName, parser.FormatDigestPost(digest));
                            var status = JObject.Parse(result);

                            if ((bool)status["ok"])
                            {
                                context.DigestPosts.Add(
                                    new DigestPost
                                    {
                                        Channel = tg,
                                        Digest = digest,
                                        TelegramMessageID = (int)status["result"]["message_id"],
                                        PostDate = Utils.UnixTimeStampToDateTime((int)status["result"]["date"]),
                                        PostLink = string.Format("https://t.me/{0}/{1}", (string)status["result"]["chat"]["username"], (string)status["result"]["message_id"]),
                                        PostText = (string)status["result"]["text"]
                                    }
                                    );
                                var links = context.Links.Where(l => l.Digest == digest);
                                foreach (var link in links)
                                {
                                    var linkResult = bot.SendMessage(tg.ChannelName, parser.FormatLinkPost(link));
                                    var linkStatus = JObject.Parse(linkResult);
                                    if ((bool)linkStatus["ok"])
                                    {
                                        context.LinkPosts.Add(
                                            new LinkPost
                                            {
                                                Channel = tg,
                                                Link = link,
                                                TelegramMessageID = (int)linkStatus["result"]["message_id"],
                                                PostDate = Utils.UnixTimeStampToDateTime((int)linkStatus["result"]["date"]),
                                                PostLink = string.Format("https://t.me/{0}/{1}", (string)linkStatus["result"]["chat"]["username"], (string)linkStatus["result"]["message_id"]),
                                                PostText = (string)linkStatus["result"]["text"]
                                            }
                                            );
                                    }
                                    else if ((int)linkStatus["error_code"] == 429)
                                    {
                                        //"Too Many Requests"
                                        childError = true;
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine(linkResult);
                                    }
                                }
                            }
                            else if ((int)status["error_code"] == 429)
                            {
                                //"Too Many Requests"
                                break;
                            }
                            else
                            {
                                Console.WriteLine(result);
                            }

                            if (childError)
                            {
                                break;
                            }
                        }
                        //Console.WriteLine(result);
                        if (childError) 
                        {
                            System.Threading.Thread.Sleep(1000 * 60 * 2);
                            Console.WriteLine("Waiting 2 minutes");
                            break; 
                        }
                    }
                    
                } while (childError);
                context.SaveChanges();
                Console.WriteLine("Waiting 60 minutes");
                System.Threading.Thread.Sleep(1000 * 60 * 60);
            }
        }

    }
}