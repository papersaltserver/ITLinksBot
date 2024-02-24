using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class ProgrammingDigest_LinkUpdate_2024_02_19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "9",
                column: "DigestURL",
                value: "https://newsletter.programmingdigest.net/"
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "9",
                column: "DigestURL",
                value: "https://programmingdigest.net/digests"
                );
        }
    }
}
