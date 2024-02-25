using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class TechProductivity_LinkUpdate_2024_02_25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "19",
                column: "DigestURL",
                value: "https://us5.campaign-archive.com/home/?u=ea228d7061e8bbfa8639666ad&id=aa93122b21"
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "19",
                column: "DigestURL",
                value: "https://us20.campaign-archive.com/home/?u=88eb3ff0c5a479cf74013ef57&id=362b1686a3"
                );
        }
    }
}
