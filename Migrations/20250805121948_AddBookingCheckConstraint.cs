using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardroomBooking4.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings",
                sql: "[EndUtc] > [StartUtc]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings");
        }
    }
}
