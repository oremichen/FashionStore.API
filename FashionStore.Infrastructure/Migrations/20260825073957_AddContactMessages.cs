using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionStore.Infrastructure.Migrations;

public partial class AddContactMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "ContactUs",
            newName: "ContactUsConfiguration");

        migrationBuilder.CreateTable(
            name: "ContactUs",
            columns: table => new
            {
                Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()::text"),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Subject = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Message = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ContactUs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ContactUs_CreatedAt",
            table: "ContactUs",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ContactUs");

        migrationBuilder.RenameTable(
            name: "ContactUsConfiguration",
            newName: "ContactUs");
    }
}
