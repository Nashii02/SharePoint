using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sharepoint.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    WorkCategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavorites_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_WorkCategories_WorkCategoryId",
                        column: x => x.WorkCategoryId,
                        principalTable: "WorkCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_AppUserId_WorkCategoryId",
                table: "UserFavorites",
                columns: new[] { "AppUserId", "WorkCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_WorkCategoryId",
                table: "UserFavorites",
                column: "WorkCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFavorites");
        }
    }
}
