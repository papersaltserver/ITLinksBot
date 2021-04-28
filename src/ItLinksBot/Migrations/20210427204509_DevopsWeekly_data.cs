using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class DevopsWeekly_data : Migration
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
                        31,
                        "Devops Weekly",
                        "https://us2.campaign-archive.com/home/?u=b6635e37e35fa5eff0c2a947a&id=a63f24d068"
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
                        31,
                        "-1001206101998",
                        31
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 31
                );
            migrationBuilder.DeleteData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: 31
                );
        }
    }
}
