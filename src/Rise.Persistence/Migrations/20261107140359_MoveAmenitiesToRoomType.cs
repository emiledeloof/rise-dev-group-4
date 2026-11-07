using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <summary>
    /// moves the amenities and the bed type from the room to its room type and gives the amenity names a language
    /// the data is copied before the old columns and the RoomAmenity table are dropped
    /// </summary>
    public partial class MoveAmenitiesToRoomType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- new tables and columns ----------
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Amenity",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "AmenityTranslation",
                columns: table => new
                {
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    AmenityId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmenityTranslation", x => new { x.AmenityId, x.Language });
                    table.ForeignKey(
                        name: "FK_AmenityTranslation_Amenity_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeAmenity",
                columns: table => new
                {
                    AmenitiesId = table.Column<int>(type: "integer", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeAmenity", x => new { x.AmenitiesId, x.RoomTypeId });
                    table.ForeignKey(
                        name: "FK_RoomTypeAmenity_Amenity_AmenitiesId",
                        column: x => x.AmenitiesId,
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomTypeAmenity_RoomType_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeTranslation",
                columns: table => new
                {
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false),
                    BedConfiguration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeTranslation", x => new { x.RoomTypeId, x.Language });
                    table.ForeignKey(
                        name: "FK_RoomTypeTranslation_RoomType_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- copy the data ----------
            // the old single name becomes the english name
            migrationBuilder.Sql("""
                INSERT INTO "AmenityTranslation" ("AmenityId", "Language", "Name")
                SELECT "Id", 'en', "Name" FROM "Amenity";
                """);

            // the icon is now required, an amenity without one gets a check mark
            migrationBuilder.Sql("""UPDATE "Amenity" SET "Icon" = 'fa-check' WHERE "Icon" IS NULL OR btrim("Icon") = '';""");

            // a type offers what its rooms offered, the union when several rooms share a type
            migrationBuilder.Sql("""
                INSERT INTO "RoomTypeAmenity" ("RoomTypeId", "AmenitiesId")
                SELECT DISTINCT r."RoomTypeId", ra."AmenitiesId"
                FROM "RoomAmenity" ra
                JOIN "Room" r ON r."Id" = ra."RoomId"
                WHERE NOT r."IsDeleted";
                """);

            // the bed type of the first room of the type becomes the english bed configuration
            migrationBuilder.Sql("""
                INSERT INTO "RoomTypeTranslation" ("RoomTypeId", "Language", "BedConfiguration")
                SELECT DISTINCT ON (r."RoomTypeId") r."RoomTypeId", 'en', r."BedType"
                FROM "Room" r
                WHERE r."BedType" IS NOT NULL AND btrim(r."BedType") <> '' AND NOT r."IsDeleted"
                ORDER BY r."RoomTypeId", r."Id";
                """);

            // ---------- remove the old structure ----------
            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Amenity",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "RoomAmenity");

            migrationBuilder.DropIndex(
                name: "IX_Amenity_Name",
                table: "Amenity");

            migrationBuilder.DropColumn(
                name: "BedType",
                table: "Room");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Amenity");

            migrationBuilder.CreateIndex(
                name: "IX_AmenityTranslation_Language_Name",
                table: "AmenityTranslation",
                columns: new[] { "Language", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeAmenity_RoomTypeId",
                table: "RoomTypeAmenity",
                column: "RoomTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ---------- old structure back ----------
            migrationBuilder.AddColumn<string>(
                name: "BedType",
                table: "Room",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Amenity",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Amenity",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "RoomAmenity",
                columns: table => new
                {
                    AmenitiesId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAmenity", x => new { x.AmenitiesId, x.RoomId });
                    table.ForeignKey(
                        name: "FK_RoomAmenity_Amenity_AmenitiesId",
                        column: x => x.AmenitiesId,
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomAmenity_Room_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Room",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------- copy the data back, only the english texts survive ----------
            migrationBuilder.Sql("""
                UPDATE "Amenity" a SET "Name" = t."Name"
                FROM "AmenityTranslation" t
                WHERE t."AmenityId" = a."Id" AND t."Language" = 'en';

                INSERT INTO "RoomAmenity" ("RoomId", "AmenitiesId")
                SELECT r."Id", rta."AmenitiesId"
                FROM "RoomTypeAmenity" rta
                JOIN "Room" r ON r."RoomTypeId" = rta."RoomTypeId";

                UPDATE "Room" r SET "BedType" = t."BedConfiguration"
                FROM "RoomTypeTranslation" t
                WHERE t."RoomTypeId" = r."RoomTypeId" AND t."Language" = 'en';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Amenity",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amenity_Name",
                table: "Amenity",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenity_RoomId",
                table: "RoomAmenity",
                column: "RoomId");

            migrationBuilder.DropTable(
                name: "AmenityTranslation");

            migrationBuilder.DropTable(
                name: "RoomTypeAmenity");

            migrationBuilder.DropTable(
                name: "RoomTypeTranslation");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Amenity");
        }
    }
}
