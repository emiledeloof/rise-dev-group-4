using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAdultsAndRateNameSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RateName",
                table: "BookingRoom",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            // existing bookings get the name their rate has now, the closest we know to the name at booking time
            migrationBuilder.Sql(
                """
                UPDATE "BookingRoom" AS br
                SET "RateName" = r."Name"
                FROM "Rate" AS r
                WHERE br."RateId" = r."Id";
                """);

            // the nb of guests of existing bookings is unknown, the minimum of 1 guest keeps them valid
            migrationBuilder.AddColumn<int>(
                name: "Adults",
                table: "Booking",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateName",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "Adults",
                table: "Booking");
        }
    }
}
