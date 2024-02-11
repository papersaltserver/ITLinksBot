using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLog_LinkUpdate_2024_02_09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "2",
                column: "DigestURL",
                value: "https://changelog.com/news/feed"
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "ProviderID",
                keyValue: "2",
                column: "DigestURL",
                value: "https://changelog.com/weekly/archive"
                );
        }
    }
}
