using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class ProgrammingDigest_LinkUpdate_2025_07_22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                            table: "Providers",
                            keyColumn: "ProviderID",
                            keyValue: "9",
                            column: "DigestURL",
                            value: "https://programmingdigest.net/newsletters"
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
                            value: "https://newsletter.programmingdigest.net/"
                            );
        }
    }
}
