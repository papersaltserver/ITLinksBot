using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class KubeWeekly_data : Migration
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
                        34,
                        "KubeWeekly",
                        "https://www.cncf.io/kubeweekly/"
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
                        34,
                        "-1001447046352",
                        34
                    }
                }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 34
                );
            migrationBuilder.DeleteData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: 34
                );
        }
    }
}
