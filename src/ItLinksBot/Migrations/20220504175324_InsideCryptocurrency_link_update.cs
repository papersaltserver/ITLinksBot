using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class InsideCryptocurrency_link_update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 15,
                column: "DigestURL",
                value: "https://community-api.inside.com/v1/sections/?list=cryptocurrency&page=1"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: 15,
                column: "DigestURL",
                value: "https://inside.com/lists/cryptocurrency/recent_issues"
                );
        }
    }
}
