using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConferenceRoomBookingAPIv3.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingServiceSnapshotsAndRangeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "BookingServiceSnapshot",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServiceSnapshot", x => new { x.BookingId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_BookingServiceSnapshot_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [BookingServiceSnapshot] ([BookingId], [ServiceId], [Name], [Price])
                SELECT [bs].[BookingId], [bs].[ServiceId], [rs].[Name], [rs].[Price]
                FROM [BookingServices] AS [bs]
                INNER JOIN [RoomServices] AS [rs] ON [rs].[Id] = [bs].[ServiceId];
                """);

            migrationBuilder.DropTable(name: "BookingServices");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_StartsAt_EndsAt",
                table: "Bookings",
                columns: new[] { "RoomId", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServiceSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId_StartsAt_EndsAt",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "BookingServices",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServices", x => new { x.BookingId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_BookingServices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingServices_RoomServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "RoomServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [BookingServices] ([BookingId], [ServiceId])
                SELECT [BookingId], [ServiceId] FROM [BookingServiceSnapshot];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId",
                table: "Bookings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_ServiceId",
                table: "BookingServices",
                column: "ServiceId");
        }
    }
}
