using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class TLDR_LinkUpdate_Nov_14_2021 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "3",
                column: "DigestURL",
                value: "https://messaged.com/tldr/newsletter"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "3",
                column: "DigestURL",
                value: "https://tldr.tech/newsletter"
                );
        }
    }
}
