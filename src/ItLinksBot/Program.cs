using AutoMapper;
using ItLinksBot.ContentGetters;
using ItLinksBot.Data;
using ItLinksBot.DTO;
using ItLinksBot.Models;
using ItLinksBot.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItLinksBot
{
    class Program
    {
        private static IServiceProvider serviceProvider;

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddScoped<IContentGetter<string>, HtmlContentGetter>();
            services.AddScoped<IContentGetter<byte[]>, BinContentGetter>();
            services.AddScoped<IContentNormalizer, DomNormlizer>();
            services.AddScoped<ITextSanitizer, TextSanitizer>();
            services.AddTransient<IParser, Oreily4ShortLinksParser>();
            services.AddTransient<IParser, ChangelogParser>();
            services.AddTransient<IParser, TLDRparser>();
            services.AddTransient<IParser, ReactNewsletterParser>();
            services.AddTransient<IParser, JavaScriptWeeklyParser>();
            services.AddTransient<IParser, SmashingEmailParser>();
            services.AddTransient<IParser, DevAwesomeParser>();
            services.AddTransient<IParser, CssWeeklyParser>();
            services.AddTransient<IParser, ProgrammingDigestParser>();
            services.AddTransient<IParser, CSharpDigestParser>();
            services.AddTransient<IParser, DBWeeklyParser>();
            services.AddTransient<IParser, StatusCodeWeeklyParser>();
            services.AddTransient<IParser, AwesomeSysAdminParser>();
            services.AddTransient<IParser, SREWeeklyParser>();
            services.AddTransient<IParser, InsideCryptocurrencyParser>();
            services.AddTransient<IParser, BetterDevLinkParser>();
            services.AddTransient<IParser, DataIsPluralParser>();
            services.AddTransient<IParser, SoftwareLeadWeeklyParser>();
            services.AddTransient<IParser, TechProductivityParser>();
            services.AddTransient<IParser, ArtificialIntelligenceParser>();
            services.AddTransient<IParser, FintechParser>();
            services.AddTransient<IParser, ProductiveGrowthParser>();
            services.AddTransient<IParser, ProductParser>();
            services.AddTransient<IParser, SpaceParser>();
            services.AddTransient<IParser, TechManagerWeeklyParser>();
            services.AddTransient<IParser, TimelessAndTimely>();
            services.AddTransient<IParser, TechmemeParser>();
            services.AddTransient<IParser, DenseDiscoveryParser>();
            services.AddTransient<IParser, BizzaroDevsParser>();
            services.AddTransient<IParser, TheProtocolParser>();
            services.AddTransient<IParser, DevopsWeeklyParser>();
            services.AddTransient<IParser, CoreyTechParser>();
            services.AddTransient<IParser, DevOpsishParser>();
            services.AddTransient<IParser, KubeWeeklyParser>();
            services.AddTransient<IParser, HowCuriousParser>();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<Photo, PhotoDTO>();
            });
            serviceProvider = services.BuildServiceProvider();
        }
        static void Main()
        {
            ConfigureServices();
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("./config/appsettings.json",
                             optional: true,
                             reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "LINKSBOT_")
                .Build();

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
            TelegramAPI bot = new(config["BotApiKey"]);
            IEnumerable<IParser> serviceCollection = serviceProvider.GetServices<IParser>();

            foreach (TelegramChannel tgChannel in context.TelegramChannels)
            {
                context.Entry(tgChannel).Reference(c => c.Provider).Load();
                var inviteLink = QueueProcessor.GetChannelInviteLink(tgChannel, bot);
                Log.Information("Digest name: {providerName}", tgChannel.Provider.ProviderName);
                Log.Information("Original digest link: {digestLink}", tgChannel.Provider.DigestURL);
                Log.Information("Invite link: {inviteLink}\n", inviteLink);
            }

            Log.Information("Checking if there are not updated digests posted");
            foreach (var prov in context.Providers)
            {
                Digest lastIssue = context.Digests.OrderBy(p => p.DigestDay).LastOrDefault(p => p.Provider == prov);
                if (lastIssue != null && prov.LatestIssue < lastIssue.DigestDay)
                {
                    Log.Information("Provider {providerName}, was updated to latest issue date {latestIssueDate}", prov.ProviderName, lastIssue.DigestDay);
                    prov.LatestIssue = lastIssue.DigestDay;
                }
            }
            context.SaveChanges();

            while (true)
            {
                var activeProviders = context.Providers.Where(pr => pr.ProviderEnabled);
                foreach (Provider prov in activeProviders)
                {
                    var parser = serviceCollection.FirstOrDefault(p => p.CurrentProvider == prov.ProviderName);
                    try
                    {
                        List<Digest> digests = parser.GetCurrentDigests(prov);
                        //saving digests to entities
                        var newDigests = digests.Except(context.Digests, new DigestComparer());
                        Log.Information($"Found {newDigests.Count()} new digests for newsletter {prov.ProviderName}");

                        //getting and saving only new links to entities
                        if (newDigests.Any())
                        {
                            int totalLinks = 0;
                            foreach (var dgst in newDigests)
                            {
                                var fullDigest = dgst;
                                //parse digests which do not have info in digest itself
                                if (fullDigest.DigestDay == new DateTime(1900, 1, 1))
                                {
                                    fullDigest = parser.GetDigestDetails(dgst);
                                }
                                List<Link> linksInCurrentDigest = parser.GetDigestLinks(fullDigest);
                                var newLinks = linksInCurrentDigest.Except(context.Links, new LinkComparer());
                                context.Digests.Add(fullDigest);
                                context.Links.AddRange(newLinks);
                                Log.Information($"Found {newLinks.Count()} new links for newsletter {prov.ProviderName} in digest {fullDigest.DigestName}");
                                prov.LatestIssue = fullDigest.DigestDay;
                                prov.SubsequentErrors = 0;
                                //persisting entities change
                                context.SaveChanges();
                                totalLinks += 1 + linksInCurrentDigest.Count;
                            }
                            prov.LatestSync = DateTime.Now;
                            Log.Information($"Total number of objects to post: {totalLinks}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error procesing {digestName}, error:\n{exception}", prov.ProviderName, e);
                        prov.SubsequentErrors++;
                        continue;
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
                        if (digests.Any()) Log.Information($"Found {digests.Count()} new digests to post in {tgChannel.Provider.ProviderName}");
                        foreach (Digest digest in digests)
                        {
                            List<DigestPost> digestPost = QueueProcessor.AddDigestPost(tgChannel, digest, bot, serviceProvider);
                            context.DigestPosts.AddRange(digestPost);

                            var links = context.Links.Where(l => l.Digest == digest).OrderBy(l => l.LinkOrder).Include(l => l.Medias);
                            foreach (var link in links)
                            {
                                List<LinkPost> linkPost = QueueProcessor.AddLinkPost(tgChannel, link, bot, serviceProvider);
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