using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationPolicyToRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancellationPolicy",
                table: "Rate",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FullRefundDays",
                table: "Rate",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundDays",
                table: "Rate",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundPercent",
                table: "Rate",
                type: "integer",
                nullable: false,
                defaultValue: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "Rate");

            migrationBuilder.DropColumn(
                name: "FullRefundDays",
                table: "Rate");

            migrationBuilder.DropColumn(
                name: "PartialRefundDays",
                table: "Rate");

            migrationBuilder.DropColumn(
                name: "PartialRefundPercent",
                table: "Rate");
        }
    }
}
