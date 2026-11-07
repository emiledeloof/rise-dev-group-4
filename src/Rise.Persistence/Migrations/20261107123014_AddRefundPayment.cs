using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundNote",
                table: "BookingRoom",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundPaidAmount",
                table: "BookingRoom",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RefundPaidOn",
                table: "BookingRoom",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundNote",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "RefundPaidAmount",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "RefundPaidOn",
                table: "BookingRoom");
        }
    }
}
