using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sharepoint.Migrations
{
    /// <inheritdoc />
    public partial class AddIconSvgToWorkCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconSvg",
                table: "WorkCategories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconSvg",
                table: "WorkCategories");
        }
    }
}
