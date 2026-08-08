using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_User_SystemRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "SystemRole",
                table: "tbl.ps_Users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "tbl.ps_ProjectMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "tbl.ps_ActualExecutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "tbl.ps_ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ProjectMembers_UserId1",
                table: "tbl.ps_ProjectMembers",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActualExecutions_UserId",
                table: "tbl.ps_ActualExecutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActionItems_UserId",
                table: "tbl.ps_ActionItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ps_ActionItems_tbl.ps_Users_UserId",
                table: "tbl.ps_ActionItems",
                column: "UserId",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ps_ActualExecutions_tbl.ps_Users_UserId",
                table: "tbl.ps_ActualExecutions",
                column: "UserId",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl.ps_ProjectMembers_tbl.ps_Users_UserId1",
                table: "tbl.ps_ProjectMembers",
                column: "UserId1",
                principalTable: "tbl.ps_Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ps_ActionItems_tbl.ps_Users_UserId",
                table: "tbl.ps_ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ps_ActualExecutions_tbl.ps_Users_UserId",
                table: "tbl.ps_ActualExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl.ps_ProjectMembers_tbl.ps_Users_UserId1",
                table: "tbl.ps_ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_tbl.ps_ProjectMembers_UserId1",
                table: "tbl.ps_ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_tbl.ps_ActualExecutions_UserId",
                table: "tbl.ps_ActualExecutions");

            migrationBuilder.DropIndex(
                name: "IX_tbl.ps_ActionItems_UserId",
                table: "tbl.ps_ActionItems");

            migrationBuilder.DropColumn(
                name: "SystemRole",
                table: "tbl.ps_Users");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "tbl.ps_ProjectMembers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "tbl.ps_ActualExecutions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "tbl.ps_ActionItems");
        }
    }
}
