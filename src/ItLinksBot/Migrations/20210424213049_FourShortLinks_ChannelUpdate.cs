using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class FourShortLinks_ChannelUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: "1",
                column: "ChannelName",
                value: "-1001167330994"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TelegramChannels",
                keyColumn: "ChannelID",
                keyValue: "1",
                column: "ChannelName",
                value: "@oreily4shortlinks"
                );
        }
    }
}
