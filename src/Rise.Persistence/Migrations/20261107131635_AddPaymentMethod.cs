using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "Payment",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // every payment so far went through Stripe
                defaultValue: "Online");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Payment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Method",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Payment");
        }
    }
}
