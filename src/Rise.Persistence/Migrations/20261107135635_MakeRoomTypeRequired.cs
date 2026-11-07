using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeRoomTypeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // a room without type gets a type named after the room, an existing type with that name is reused
            migrationBuilder.Sql("""
                INSERT INTO "RoomType" ("Name")
                SELECT DISTINCT left(r."Name", 100)
                FROM "Room" r
                WHERE r."RoomTypeId" IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "RoomType" t
                      WHERE NOT t."IsDeleted" AND lower(t."Name") = lower(left(r."Name", 100)));

                UPDATE "Room" r
                SET "RoomTypeId" = (
                    SELECT t."Id" FROM "RoomType" t
                    WHERE NOT t."IsDeleted" AND lower(t."Name") = lower(left(r."Name", 100))
                    ORDER BY t."Id"
                    LIMIT 1)
                WHERE r."RoomTypeId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RoomTypeId",
                table: "Room",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RoomTypeId",
                table: "Room",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
