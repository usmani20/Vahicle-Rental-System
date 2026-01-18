using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vahicle_Rental_System.Migrations
{
    /// <inheritdoc />
    public partial class Bookinggg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropoffLocation",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffLocation",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "Bookings");
        }
    }
}
