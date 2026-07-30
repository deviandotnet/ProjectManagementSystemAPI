using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl.ms_HolidayCalendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    IsRecurringAnnually = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ms_HolidayCalendar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekStartDay = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DefaultTimelineScale = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)2),
                    ProgressMode = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)1),
                    Status = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)1),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_Categories_tbl.ps_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "tbl.ps_Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_AuditLogs_tbl.ps_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "tbl.ps_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<byte>(type: "smallint", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ProjectMembers_tbl.ps_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "tbl.ps_Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ProjectMembers_tbl.ps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl.ps_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_SubCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_SubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_SubCategories_tbl.ps_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "tbl.ps_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_ActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionItemName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)1),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_ActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActionItems_tbl.ps_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "tbl.ps_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActionItems_tbl.ps_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "tbl.ps_Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActionItems_tbl.ps_SubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "tbl.ps_SubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActionItems_tbl.ps_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "tbl.ps_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_ActualExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualHours = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    CompletedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DelayReason = table.Column<string>(type: "text", nullable: true),
                    ActualRemarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_ActualExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActualExecutions_tbl.ps_ActionItems_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "tbl.ps_ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl.ps_ActualExecutions_tbl.ps_Users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "tbl.ps_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl.ps_PlannedSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedStartWeek = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PlannedEndWeek = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DurationCalendarDays = table.Column<int>(type: "integer", nullable: false),
                    DurationWorkingDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl.ps_PlannedSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl.ps_PlannedSchedules_tbl.ps_ActionItems_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "tbl.ps_ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ms_HolidayCalendar_HolidayDate_Year",
                table: "tbl.ms_HolidayCalendar",
                columns: new[] { "HolidayDate", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActionItems_CategoryId",
                table: "tbl.ps_ActionItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActionItems_OwnerId",
                table: "tbl.ps_ActionItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActionItems_ProjectId",
                table: "tbl.ps_ActionItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActionItems_SubCategoryId",
                table: "tbl.ps_ActionItems",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActualExecutions_ActionItemId",
                table: "tbl.ps_ActualExecutions",
                column: "ActionItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ActualExecutions_CompletedById",
                table: "tbl.ps_ActualExecutions",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_AuditLogs_ChangedByUserId",
                table: "tbl.ps_AuditLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_AuditLogs_EntityName_EntityId",
                table: "tbl.ps_AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_Categories_ProjectId",
                table: "tbl.ps_Categories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_PlannedSchedules_ActionItemId",
                table: "tbl.ps_PlannedSchedules",
                column: "ActionItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ProjectMembers_ProjectId_UserId",
                table: "tbl.ps_ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_ProjectMembers_UserId",
                table: "tbl.ps_ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_SubCategories_CategoryId",
                table: "tbl.ps_SubCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl.ps_Users_Email",
                table: "tbl.ps_Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl.ms_HolidayCalendar");

            migrationBuilder.DropTable(
                name: "tbl.ps_ActualExecutions");

            migrationBuilder.DropTable(
                name: "tbl.ps_AuditLogs");

            migrationBuilder.DropTable(
                name: "tbl.ps_PlannedSchedules");

            migrationBuilder.DropTable(
                name: "tbl.ps_ProjectMembers");

            migrationBuilder.DropTable(
                name: "tbl.ps_ActionItems");

            migrationBuilder.DropTable(
                name: "tbl.ps_SubCategories");

            migrationBuilder.DropTable(
                name: "tbl.ps_Users");

            migrationBuilder.DropTable(
                name: "tbl.ps_Categories");

            migrationBuilder.DropTable(
                name: "tbl.ps_Projects");
        }
    }
}
