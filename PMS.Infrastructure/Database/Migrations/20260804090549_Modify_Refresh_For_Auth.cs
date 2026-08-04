using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Modify_Refresh_For_Auth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshToken_tbl.ps_Users_UserId",
                table: "RefreshToken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshToken",
                table: "RefreshToken");

            migrationBuilder.RenameTable(
                name: "RefreshToken",
                newName: "tbl.ms_RefreshTokens");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshToken_UserId",
                table: "tbl.ms_RefreshTokens",
                newName: "IX_tbl.ms_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshToken_Token",
                table: "tbl.ms_RefreshTokens",
                newName: "IX_tbl.ms_RefreshTokens_Token");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tbl.ms_RefreshTokens",
                table: "tbl.ms_RefreshTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ms_RefreshTokens_tbl.ps_Users_UserId",
                table: "tbl.ms_RefreshTokens",
                column: "UserId",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ms_RefreshTokens_tbl.ps_Users_UserId",
                table: "tbl.ms_RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tbl.ms_RefreshTokens",
                table: "tbl.ms_RefreshTokens");

            migrationBuilder.RenameTable(
                name: "tbl.ms_RefreshTokens",
                newName: "RefreshToken");

            migrationBuilder.RenameIndex(
                name: "IX_tbl.ms_RefreshTokens_UserId",
                table: "RefreshToken",
                newName: "IX_RefreshToken_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tbl.ms_RefreshTokens_Token",
                table: "RefreshToken",
                newName: "IX_RefreshToken_Token");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshToken",
                table: "RefreshToken",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshToken_tbl.ps_Users_UserId",
                table: "RefreshToken",
                column: "UserId",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
