using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class CssWeekly_data : Migration
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
                        8,
                        "CSS Weekly",
                        "https://css-weekly.com/archives/"
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
                        8,
                        "@cssweekly_newsletter",
                        8
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 8
                );
            migrationBuilder.DeleteData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: 8
                );
        }
    }
}
