using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationPolicyToBookingRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ! existing bookings get the same defaults as the rate migration, 0 = NonRefundable
            migrationBuilder.AddColumn<int>(
                name: "CancellationPolicy",
                table: "BookingRoom",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FullRefundDays",
                table: "BookingRoom",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundDays",
                table: "BookingRoom",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundPercent",
                table: "BookingRoom",
                type: "integer",
                nullable: false,
                defaultValue: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "FullRefundDays",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "PartialRefundDays",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "PartialRefundPercent",
                table: "BookingRoom");
        }
    }
}
