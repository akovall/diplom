using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diplom.Data.Migrations
{
    public partial class AddUserCurrentSessionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSessionId",
                table: "Users",
                type: "uuid",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSessionId",
                table: "Users");
        }
    }
}

