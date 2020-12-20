using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class InitializeDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    ProviderID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(type: "TEXT", nullable: true),
                    DigestURL = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.ProviderID);
                });

            migrationBuilder.CreateTable(
                name: "Digests",
                columns: table => new
                {
                    DigestId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DigestDay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DigestName = table.Column<string>(type: "TEXT", nullable: true),
                    DigestURL = table.Column<string>(type: "TEXT", nullable: true),
                    DigestDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Digests", x => x.DigestId);
                    table.ForeignKey(
                        name: "FK_Digests_Providers_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Providers",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelegramChannels",
                columns: table => new
                {
                    ChannelID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramChannels", x => x.ChannelID);
                    table.ForeignKey(
                        name: "FK_TelegramChannels_Providers_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Providers",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    LinkID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    URL = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DigestId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.LinkID);
                    table.ForeignKey(
                        name: "FK_Links_Digests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "Digests",
                        principalColumn: "DigestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DigestPosts",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<int>(type: "INTEGER", nullable: true),
                    TelegramMessageID = table.Column<int>(type: "INTEGER", nullable: false),
                    PostDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostLink = table.Column<string>(type: "TEXT", nullable: true),
                    PostText = table.Column<string>(type: "TEXT", nullable: true),
                    DigestId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestPosts", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_DigestPosts_Digests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "Digests",
                        principalColumn: "DigestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DigestPosts_TelegramChannels_ChannelID",
                        column: x => x.ChannelID,
                        principalTable: "TelegramChannels",
                        principalColumn: "ChannelID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinkPost",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<int>(type: "INTEGER", nullable: true),
                    TelegramMessageID = table.Column<int>(type: "INTEGER", nullable: false),
                    PostDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostLink = table.Column<string>(type: "TEXT", nullable: true),
                    PostText = table.Column<string>(type: "TEXT", nullable: true),
                    LinkID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkPost", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_LinkPost_Links_LinkID",
                        column: x => x.LinkID,
                        principalTable: "Links",
                        principalColumn: "LinkID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LinkPost_TelegramChannels_ChannelID",
                        column: x => x.ChannelID,
                        principalTable: "TelegramChannels",
                        principalColumn: "ChannelID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigestPosts_ChannelID",
                table: "DigestPosts",
                column: "ChannelID");

            migrationBuilder.CreateIndex(
                name: "IX_DigestPosts_DigestId",
                table: "DigestPosts",
                column: "DigestId");

            migrationBuilder.CreateIndex(
                name: "IX_Digests_ProviderID",
                table: "Digests",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_LinkPost_ChannelID",
                table: "LinkPost",
                column: "ChannelID");

            migrationBuilder.CreateIndex(
                name: "IX_LinkPost_LinkID",
                table: "LinkPost",
                column: "LinkID");

            migrationBuilder.CreateIndex(
                name: "IX_Links_DigestId",
                table: "Links",
                column: "DigestId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramChannels_ProviderID",
                table: "TelegramChannels",
                column: "ProviderID");

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] {
                    "ProviderID",
                    "ProviderName",
                    "DigestURL" },
                values: new object[,] {
                    {
                        1,
                        "O'Reily Four Short Links",
                        "https://www.oreilly.com/radar/topics/four-short-links/feed/index.xml"
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
                        1,
                        "@oreily4shortlinks",
                        1
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigestPosts");

            migrationBuilder.DropTable(
                name: "LinkPost");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "TelegramChannels");

            migrationBuilder.DropTable(
                name: "Digests");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
