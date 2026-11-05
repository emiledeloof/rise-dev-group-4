using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <summary>
    /// Moves the cancellation from the booking to its rooms, the booking now derives its state from the rooms.
    /// </summary>
    public partial class MoveCancellationToBookingRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "BookingRoom",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "BookingRoom",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "BookingRoom",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // every room of a cancelled booking becomes cancelled, the old refund is split by the room price
            // the rounding difference goes to the 1st room, so the refunds still add up to the old amount exactly
            migrationBuilder.Sql("""
                WITH shares AS (
                    SELECT br."Id",
                           br."BookingId",
                           b."CancelledAt",
                           b."CancellationReason",
                           COALESCE(b."RefundAmount", 0) AS refund,
                           ROUND(COALESCE(b."RefundAmount", 0) * br."PricePerNight"
                               / SUM(br."PricePerNight") OVER (PARTITION BY br."BookingId"), 2) AS share,
                           ROW_NUMBER() OVER (PARTITION BY br."BookingId" ORDER BY br."Id") AS position
                    FROM "BookingRoom" br
                    JOIN "Booking" b ON b."Id" = br."BookingId"
                    WHERE b."CancelledAt" IS NOT NULL AND NOT br."IsDeleted"
                ), split AS (
                    SELECT "Id",
                           "CancelledAt",
                           "CancellationReason",
                           share + CASE WHEN position = 1
                               THEN refund - SUM(share) OVER (PARTITION BY "BookingId")
                               ELSE 0 END AS room_refund
                    FROM shares
                )
                UPDATE "BookingRoom" room
                SET "CancelledAt" = split."CancelledAt",
                    "CancellationReason" = split."CancellationReason",
                    "RefundAmount" = split.room_refund
                FROM split
                WHERE room."Id" = split."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Booking");
        }

        /// <summary>
        /// ! only fully cancelled bookings can be restored, the cancelled rooms of a partly cancelled booking become active again
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Booking",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Booking",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Booking" b
                SET "CancelledAt" = cancelled.cancelled_at,
                    "CancellationReason" = cancelled.reason,
                    "RefundAmount" = cancelled.refund
                FROM (
                    SELECT "BookingId",
                           MAX("CancelledAt") AS cancelled_at,
                           (ARRAY_AGG("CancellationReason" ORDER BY "CancelledAt" DESC))[1] AS reason,
                           SUM(COALESCE("RefundAmount", 0)) AS refund
                    FROM "BookingRoom"
                    WHERE NOT "IsDeleted"
                    GROUP BY "BookingId"
                    HAVING COUNT(*) = COUNT("CancelledAt")
                ) cancelled
                WHERE b."Id" = cancelled."BookingId";
                """);

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "BookingRoom");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "BookingRoom");
        }
    }
}
