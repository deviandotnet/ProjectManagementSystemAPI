using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectMemberShadowUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ps_ProjectMembers_tbl.ps_Users_UserId1",
                table: "tbl.ps_ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_tbl.ps_ProjectMembers_UserId1",
                table: "tbl.ps_ProjectMembers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "tbl.ps_ProjectMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "tbl.ps_ProjectMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ProjectMembers_UserId1",
                table: "tbl.ps_ProjectMembers",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ps_ProjectMembers_tbl.ps_Users_UserId1",
                table: "tbl.ps_ProjectMembers",
                column: "UserId1",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id");
        }
    }
}
