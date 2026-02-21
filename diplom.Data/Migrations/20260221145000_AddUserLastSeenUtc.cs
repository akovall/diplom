using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Data.Migrations
{
    public partial class AddUserLastSeenUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenUtc",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenUtc",
                table: "Users");
        }
    }
}

