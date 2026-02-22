using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Data.Migrations
{
    public partial class AddTaskAssignmentCompletionTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "Tasks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "Tasks",
                type: "timestamp without time zone",
                nullable: true);

            // Backfill existing data with best-effort approximations.
            // - If a task is assigned, treat CreatedAt as the initial assignment moment.
            // - If a task is Done, treat CreatedAt as completion moment (unknown historically).
            migrationBuilder.Sql(@"
UPDATE ""Tasks""
SET ""AssignedAtUtc"" = ""CreatedAt""
WHERE ""AssignedAtUtc"" IS NULL AND ""AssigneeId"" IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ""Tasks""
SET ""CompletedAtUtc"" = ""CreatedAt""
WHERE ""CompletedAtUtc"" IS NULL AND ""Status"" = 3;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Tasks");
        }
    }
}

