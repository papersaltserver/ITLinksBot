using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class CSharpDigest_LinkUpdate_2024_02_25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "10",
                column: "DigestURL",
                value: "https://newsletter.csharpdigest.net/"
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "10",
                column: "DigestURL",
                value: "https://csharpdigest.net/digests"
                );
        }
    }
}
