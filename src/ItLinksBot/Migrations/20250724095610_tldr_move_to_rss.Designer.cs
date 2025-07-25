﻿// <auto-generated />
using System;
using ItLinksBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ItLinksBot.Migrations
{
    [DbContext(typeof(ITLinksContext))]
    [Migration("20250724095610_tldr_move_to_rss")]
    partial class tldr_move_to_rss
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("ItLinksBot.Models.Digest", b =>
                {
                    b.Property<int>("DigestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DigestDay")
                        .HasColumnType("TEXT");

                    b.Property<string>("DigestDescription")
                        .HasColumnType("TEXT");

                    b.Property<string>("DigestName")
                        .HasColumnType("TEXT");

                    b.Property<string>("DigestURL")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ProviderID")
                        .HasColumnType("INTEGER");

                    b.HasKey("DigestId");

                    b.HasIndex("ProviderID");

                    b.ToTable("Digests");
                });

            modelBuilder.Entity("ItLinksBot.Models.DigestPost", b =>
                {
                    b.Property<int>("PostID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ChannelID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DigestId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("PostDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("PostLink")
                        .HasColumnType("TEXT");

                    b.Property<string>("PostText")
                        .HasColumnType("TEXT");

                    b.Property<int>("TelegramMessageID")
                        .HasColumnType("INTEGER");

                    b.HasKey("PostID");

                    b.HasIndex("ChannelID");

                    b.HasIndex("DigestId");

                    b.ToTable("DigestPosts");
                });

            modelBuilder.Entity("ItLinksBot.Models.Link", b =>
                {
                    b.Property<int>("LinkID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int?>("DigestId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LinkOrder")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<string>("URL")
                        .HasColumnType("TEXT");

                    b.HasKey("LinkID");

                    b.HasIndex("DigestId");

                    b.ToTable("Links");
                });

            modelBuilder.Entity("ItLinksBot.Models.LinkPost", b =>
                {
                    b.Property<int>("PostID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ChannelID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LinkID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("PostDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("PostLink")
                        .HasColumnType("TEXT");

                    b.Property<string>("PostText")
                        .HasColumnType("TEXT");

                    b.Property<int>("TelegramMessageID")
                        .HasColumnType("INTEGER");

                    b.HasKey("PostID");

                    b.HasIndex("ChannelID");

                    b.HasIndex("LinkID");

                    b.ToTable("LinkPost");
                });

            modelBuilder.Entity("ItLinksBot.Models.Media", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("ContentBytes")
                        .HasColumnType("BLOB");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("LinkID")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("LinkID");

                    b.ToTable("Medias");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Media");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("ItLinksBot.Models.Provider", b =>
                {
                    b.Property<int>("ProviderID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DigestURL")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LatestIssue")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LatestSync")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ProviderEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProviderName")
                        .HasColumnType("TEXT");

                    b.Property<int>("SubsequentErrors")
                        .HasColumnType("INTEGER");

                    b.HasKey("ProviderID");

                    b.ToTable("Providers");
                });

            modelBuilder.Entity("ItLinksBot.Models.TelegramChannel", b =>
                {
                    b.Property<int>("ChannelID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChannelName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ProviderID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ChannelID");

                    b.HasIndex("ProviderID");

                    b.ToTable("TelegramChannels");
                });

            modelBuilder.Entity("ItLinksBot.Models.Photo", b =>
                {
                    b.HasBaseType("ItLinksBot.Models.Media");

                    b.HasDiscriminator().HasValue("Photo");
                });

            modelBuilder.Entity("ItLinksBot.Models.Digest", b =>
                {
                    b.HasOne("ItLinksBot.Models.Provider", "Provider")
                        .WithMany()
                        .HasForeignKey("ProviderID");

                    b.Navigation("Provider");
                });

            modelBuilder.Entity("ItLinksBot.Models.DigestPost", b =>
                {
                    b.HasOne("ItLinksBot.Models.TelegramChannel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelID");

                    b.HasOne("ItLinksBot.Models.Digest", "Digest")
                        .WithMany()
                        .HasForeignKey("DigestId");

                    b.Navigation("Channel");

                    b.Navigation("Digest");
                });

            modelBuilder.Entity("ItLinksBot.Models.Link", b =>
                {
                    b.HasOne("ItLinksBot.Models.Digest", "Digest")
                        .WithMany("Links")
                        .HasForeignKey("DigestId");

                    b.Navigation("Digest");
                });

            modelBuilder.Entity("ItLinksBot.Models.LinkPost", b =>
                {
                    b.HasOne("ItLinksBot.Models.TelegramChannel", "Channel")
                        .WithMany("Posts")
                        .HasForeignKey("ChannelID");

                    b.HasOne("ItLinksBot.Models.Link", "Link")
                        .WithMany()
                        .HasForeignKey("LinkID");

                    b.Navigation("Channel");

                    b.Navigation("Link");
                });

            modelBuilder.Entity("ItLinksBot.Models.Media", b =>
                {
                    b.HasOne("ItLinksBot.Models.Link", "Link")
                        .WithMany("Medias")
                        .HasForeignKey("LinkID");

                    b.Navigation("Link");
                });

            modelBuilder.Entity("ItLinksBot.Models.TelegramChannel", b =>
                {
                    b.HasOne("ItLinksBot.Models.Provider", "Provider")
                        .WithMany()
                        .HasForeignKey("ProviderID");

                    b.Navigation("Provider");
                });

            modelBuilder.Entity("ItLinksBot.Models.Digest", b =>
                {
                    b.Navigation("Links");
                });

            modelBuilder.Entity("ItLinksBot.Models.Link", b =>
                {
                    b.Navigation("Medias");
                });

            modelBuilder.Entity("ItLinksBot.Models.TelegramChannel", b =>
                {
                    b.Navigation("Posts");
                });
#pragma warning restore 612, 618
        }
    }
}
