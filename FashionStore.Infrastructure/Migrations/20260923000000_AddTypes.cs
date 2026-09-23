using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionStore.Infrastructure.Migrations;

public partial class AddTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Types",
            columns: table => new
            {
                Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()::text"),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Types", type => type.Id);
                table.CheckConstraint("CK_Types_Name_NotBlank", "btrim(\"Name\") <> ''");
                table.CheckConstraint("CK_Types_Slug_Format", "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Types_Name",
            table: "Types",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Types_Slug",
            table: "Types",
            column: "Slug",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Types");
    }
}
