using ItLinksBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace ItLinksBot.Data
{
    public class ITLinksContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.debug.json")
                .Build();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
#else
            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
#endif
        }

        public DbSet<Digest> Digests { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<LinkPost> Posts { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<TelegramChannel> TelegramChannels { get; set; }
        public DbSet<DigestPost> DigestPosts { get; set; }
        public DbSet<LinkPost> LinkPosts { get; set; }
    }
}
