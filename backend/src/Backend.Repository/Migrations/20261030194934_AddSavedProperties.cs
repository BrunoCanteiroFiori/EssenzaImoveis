using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saved_properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visitor_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    saved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_properties_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_property_views_visitor_hash_viewed_at",
                table: "property_views",
                columns: new[] { "visitor_hash", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_properties_property_id",
                table: "saved_properties",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_properties_visitor_hash_property_id",
                table: "saved_properties",
                columns: new[] { "visitor_hash", "property_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_properties");

            migrationBuilder.DropIndex(
                name: "ix_property_views_visitor_hash_viewed_at",
                table: "property_views");
        }
    }
}
