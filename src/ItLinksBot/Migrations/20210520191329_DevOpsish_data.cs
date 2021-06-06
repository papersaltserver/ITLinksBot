using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class DevOpsish_data : Migration
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
                        33,
                        "DevOps'ish",
                        "https://devopsish.com/categories/weekly/"
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
                        33,
                        "-1001158197527",
                        33
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 33
                );
            migrationBuilder.DeleteData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: 33
                );
        }
    }
}
