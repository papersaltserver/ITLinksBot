﻿using System;
using Microsoft.EntityFrameworkCore;
using ItLinksBot.Data;
using ItLinksBot.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using ItLinksBot.Providers;
using Serilog;

namespace ItLinksBot
{
    public interface IParser
    {
        List<Digest> GetCurrentDigests();
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
                foreach (Provider prov in context.Providers)
                {
                    var parser = ParserFactory.Setup(prov);
                    List<Digest> digests = parser.GetCurrentDigests();
                    //saving digests to entities
                    var newDigests = digests.Except(context.Digests, new DigestComparer());
                    context.Digests.AddRange(newDigests);
                    Log.Information($"Found {newDigests.Count()} new digests for newsletter {prov.ProviderName}");

                    //getting and savint only new links to entities
                    List<Link> links = new List<Link>();
                    foreach(var dgst in newDigests)
                    {
                        var linksInCurrentDigest = parser.GetDigestLinks(dgst);
                        links.AddRange(linksInCurrentDigest);
                    }
                    
                    var newLinks = links.Except(context.Links, new LinkComparer());
                    context.Links.AddRange(newLinks);
                    Log.Information($"Found {newLinks.Count()} new links for newsletter {prov.ProviderName}");
                    //persisting entities change
                    context.SaveChanges();
                }

                bool botTimeout;
                do
                {
                    botTimeout = false;
                    foreach (TelegramChannel tgChannel in context.TelegramChannels)
                    {
                        //Finishing any unfinished previouslt digests
                        var unfinishedLinks = context.Links.Where(l => l.Digest == context.DigestPosts.Where(d => d.Channel == tgChannel).OrderBy(d => d.PostDate).Last().Digest && !context.LinkPosts.Select(lp => lp.Link).Contains(l));
                        if(unfinishedLinks.Any()) Log.Information($"Found {unfinishedLinks.Count()} unfinished links for the latest digest in {tgChannel.Provider.ProviderName}");
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
                            Log.Information("Sleeping for 1 minute for Telegram cooldown");
                            System.Threading.Thread.Sleep(1000 * 60 * 1);
                            break;
                        }
                        //Posting new digests, not posted yet
                        var digests = context.Digests.Where(d => d.Provider == tgChannel.Provider && !context.DigestPosts.Select(dp => dp.Digest).Contains(d)).OrderBy(d => d.DigestDay);
                        if (digests.Any()) Log.Information($"Found {digests.Count()} new digests to post in {tgChannel.ChannelName}");
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
                            Log.Information("Sleeping for 1 minute for Telegram cooldown - throttling not working");
                            System.Threading.Thread.Sleep(1000 * 60 * 1);
                            break; 
                        }
                    }
                    context.SaveChanges();
                } while (botTimeout);
                context.SaveChanges();
                Log.Information("Nothing to post. Sleeping for 1 hour");
                System.Threading.Thread.Sleep(1000 * 60 * 60);
            }
        }

    }
}