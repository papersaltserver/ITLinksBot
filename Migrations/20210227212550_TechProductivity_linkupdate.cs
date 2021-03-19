using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class TechProductivity_linkupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "19",
                column: "DigestURL",
                value: "https://us20.campaign-archive.com/home/?u=88eb3ff0c5a479cf74013ef57&id=362b1686a3"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "19",
                column: "DigestURL",
                value: "https://techproductivity.co/archive/"
                );
        }
    }
}
