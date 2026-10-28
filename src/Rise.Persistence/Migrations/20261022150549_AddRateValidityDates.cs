using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateValidityDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Rate",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Rate",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Rate");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Rate");
        }
    }
}
