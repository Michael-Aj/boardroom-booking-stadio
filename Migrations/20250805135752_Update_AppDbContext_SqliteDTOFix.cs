using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardroomBooking4.Migrations
{
    /// <inheritdoc />
    public partial class Update_AppDbContext_SqliteDTOFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_VenueId_StartUtc_EndUtc",
                table: "Bookings");

            migrationBuilder.AlterColumn<long>(
                name: "StartUtc",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "EndUtc",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VenueId_StartUtc_EndUtc",
                table: "Bookings",
                columns: new[] { "VenueId", "StartUtc", "EndUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_VenueId_StartUtc_EndUtc",
                table: "Bookings");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartUtc",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndUtc",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VenueId_StartUtc_EndUtc",
                table: "Bookings",
                columns: new[] { "VenueId", "StartUtc", "EndUtc" });
        }
    }
}
