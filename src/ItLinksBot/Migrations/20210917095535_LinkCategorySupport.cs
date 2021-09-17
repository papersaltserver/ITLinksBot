using Microsoft.EntityFrameworkCore.Migrations;

namespace ItLinksBot.Migrations
{
    public partial class LinkCategorySupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Links",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Links");
        }
    }
}
