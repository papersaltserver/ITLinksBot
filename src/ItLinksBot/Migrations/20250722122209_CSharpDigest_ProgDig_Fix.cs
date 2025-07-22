using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItLinksBot.Migrations
{
    /// <inheritdoc />
    public partial class CSharpDigest_ProgDig_Fix : Migration
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
            migrationBuilder.UpdateData(
                            table: "Providers",
                            keyColumn: "ProviderID",
                            keyValue: "10",
                            column: "DigestURL",
                            value: "https://csharpdigest.net/newsletters"
                            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
