using ItLinksBot.Models;
using Microsoft.EntityFrameworkCore;

namespace ItLinksBot.Data
{
    public class ITLinksContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            //modelBuilder.Entity<Provider>().ToTable("Providers", "test");
            modelBuilder.Entity<Provider>(entity =>
            {
                entity.HasKey(e => e.ProviderID);
                entity.HasIndex(e => e.ProviderName);
                entity.Property(e => e.DigestURL);
            });
            modelBuilder.Entity<Provider>().HasData(
                new Provider
                {
                    ProviderID = 1,
                    ProviderName = "O'Reily Four Short Links",
                    DigestURL = "https://www.oreilly.com/radar/topics/four-short-links/feed/index.xml"
                }
            );
            modelBuilder.Entity<Digest>(entity =>
            {
                entity.HasKey(e => e.DigestId);
                entity.HasIndex(e => e.DigestName);
                entity.Property(e => e.DigestDay);
                entity.Property(e => e.DigestDescription);
                entity.Property(e => e.DigestURL);
                //entity.Property(e => e.Provider);
            });
            modelBuilder.Entity<TelegramChannel>().HasData(
                new TelegramChannel
                {
                    ChannelID = 1,
                    ChannelName = "@oreily4shortlinks",
                });
            base.OnModelCreating(modelBuilder);
        }
        public ITLinksContext(DbContextOptions<ITLinksContext> options) : base(options)
        {
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
