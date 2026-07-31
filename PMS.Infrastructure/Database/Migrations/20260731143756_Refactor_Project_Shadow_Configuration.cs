using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_Project_Shadow_Configuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_Projects_CreatedByUserId",
                table: "tbl.ps_Projects",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ps_Projects_tbl.ps_Users_CreatedByUserId",
                table: "tbl.ps_Projects",
                column: "CreatedByUserId",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ps_Projects_tbl.ps_Users_CreatedByUserId",
                table: "tbl.ps_Projects");

            migrationBuilder.DropIndex(
                name: "IX_tbl.ps_Projects_CreatedByUserId",
                table: "tbl.ps_Projects");
        }
    }
}
