﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class CSharpDigest_data : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] {
                    "ProviderID",
                    "ProviderName",
                    "DigestURL" },
                values: new object[,] {
                    {
                        10,
                        "c# digest",
                        "https://csharpdigest.net/digests"
                    }
                }
                );
            migrationBuilder.InsertData(
                table: "TelegramChannels",
                columns: new[] {
                    "ChannelID",
                    "ChannelName",
                    "ProviderID"
                },
                values: new object[,]
                {
                    {
                        10,
                        "@csharpdigest_newsletter",
                        10
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 10
                );
            migrationBuilder.DeleteData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: 10
                );
        }
    }
}
