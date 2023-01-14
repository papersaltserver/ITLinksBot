using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    public partial class Fintech_new_platform : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "21",
                column: "DigestURL",
                value: "https://teal.api.tryletterhead.com/api/v2/channels/techstars-fintech/letters/archive?page=1&records=5"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "3",
                column: "DigestURL",
                value: "https://www.getrevue.co/profile/fintech"
                );
        }
    }
}
