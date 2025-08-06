using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardroomBooking4.Migrations
{
    /// <inheritdoc />
    public partial class RecurringBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeriesId",
                table: "Bookings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VenueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    StartUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    EndUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: true),
                    UntilUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ByWeekdays = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSeries", x => x.Id);
                    table.CheckConstraint("CK_Series_StartBeforeEnd", "StartUtc < EndUtc");
                    table.ForeignKey(
                        name: "FK_BookingSeries_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SeriesId",
                table: "Bookings",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeries_VenueId",
                table: "BookingSeries",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingSeries_SeriesId",
                table: "Bookings",
                column: "SeriesId",
                principalTable: "BookingSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingSeries_SeriesId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingSeries");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_SeriesId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Bookings");
        }
    }
}
