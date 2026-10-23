using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Room_Name",
                table: "Room",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Rate_Name",
                table: "Rate",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceNumber",
                table: "Payment",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaidAt",
                table: "Payment",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_Name",
                table: "Guest",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_CheckOut",
                table: "Booking",
                column: "CheckOut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Room_Name",
                table: "Room");

            migrationBuilder.DropIndex(
                name: "IX_Rate_Name",
                table: "Rate");

            migrationBuilder.DropIndex(
                name: "IX_Payment_InvoiceNumber",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_PaidAt",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Guest_Name",
                table: "Guest");

            migrationBuilder.DropIndex(
                name: "IX_Booking_CheckOut",
                table: "Booking");
        }
    }
}
