using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations
{
    /// <inheritdoc />
    public partial class JWT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TodoSet",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoSet_UserId",
                table: "TodoSet",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoSet_AspNetUsers_UserId",
                table: "TodoSet",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoSet_AspNetUsers_UserId",
                table: "TodoSet");

            migrationBuilder.DropIndex(
                name: "IX_TodoSet_UserId",
                table: "TodoSet");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TodoSet");
        }
    }
}
