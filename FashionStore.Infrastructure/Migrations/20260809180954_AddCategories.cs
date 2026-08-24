using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()::text"),
                    ParentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowInMenu = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Categories_Id_UuidFormat", "\"Id\" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'");
                    table.CheckConstraint("CK_Categories_Name_NotBlank", "btrim(\"Name\") <> ''");
                    table.CheckConstraint("CK_Categories_ParentId_NotSelf", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\"");
                    table.CheckConstraint("CK_Categories_Slug_Format", "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
                    table.CheckConstraint("CK_Categories_SortOrder_Nonnegative", "\"SortOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onUpdate: ReferentialAction.Cascade,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_SortOrder",
                table: "Categories",
                columns: new[] { "ParentId", "SortOrder", "Name" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_Categories_Active_ParentId_Name"
                ON "Categories" (COALESCE("ParentId", ''), lower("Name"))
                WHERE "DeletedAt" IS NULL;

                CREATE UNIQUE INDEX "UX_Categories_Active_Slug"
                ON "Categories" (lower("Slug"))
                WHERE "DeletedAt" IS NULL;

                INSERT INTO "Categories"
                    ("ParentId", "Name", "Slug", "Description", "SortOrder", "IsActive", "ShowInMenu")
                VALUES
                    (NULL, 'Fashion', 'fashion', 'Fashion product categories', 10, true, true)
                ON CONFLICT DO NOTHING;

                INSERT INTO "Categories"
                    ("ParentId", "Name", "Slug", "Description", "SortOrder", "IsActive", "ShowInMenu")
                SELECT "Id", 'Mixed Fabrics', 'mixed-fabrics', 'Products made from mixed fabrics', 10, true, true
                FROM "Categories"
                WHERE "Slug" = 'fashion' AND "DeletedAt" IS NULL
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
