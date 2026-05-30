using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sharepoint.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AvatarData",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarMimeType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarData",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarMimeType",
                table: "Users");
        }
    }
}
