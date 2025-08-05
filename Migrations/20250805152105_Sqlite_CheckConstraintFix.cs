using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardroomBooking4.Migrations
{
    /// <inheritdoc />
    public partial class Sqlite_CheckConstraintFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Venues_VenueId",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VenueId_StartUtc",
                table: "Bookings",
                columns: new[] { "VenueId", "StartUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_StartBeforeEnd",
                table: "Bookings",
                sql: "StartUtc < EndUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Venues_VenueId",
                table: "Bookings",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Venues_VenueId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_VenueId_StartUtc",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_StartBeforeEnd",
                table: "Bookings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings",
                sql: "[EndUtc] > [StartUtc]");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Venues_VenueId",
                table: "Bookings",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
