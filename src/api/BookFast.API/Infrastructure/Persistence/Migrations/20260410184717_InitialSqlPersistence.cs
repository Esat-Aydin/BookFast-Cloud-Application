using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookFast.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    AmenitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.CheckConstraint("CK_Rooms_Capacity_Positive", "[Capacity] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.CheckConstraint("CK_Reservations_TimeRange", "[StartUtc] < [EndUtc]");
                    table.ForeignKey(
                        name: "FK_Reservations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "AmenitiesJson", "Capacity", "Code", "Location", "Name" },
                values: new object[,]
                {
                    { new Guid("8c2d3cfd-2f3a-4c72-9f5b-7397c1d4b901"), "[\"Teams Room\",\"Whiteboard\",\"4K Display\"]", 12, "AMS-BOARD-01", "Amsterdam HQ - Floor 5", "Amsterdam Boardroom" },
                    { new Guid("a8b70b66-676c-4a1d-9ea6-865a0b918a72"), "[\"Video Conferencing\",\"Whiteboard\"]", 8, "UTR-COLLAB-02", "Utrecht Office - Floor 2", "Utrecht Collaboration Hub" },
                    { new Guid("c93b8e11-6d12-4f7c-8bf0-08fa0a1d2c54"), "[\"Quiet Zone\",\"Docking Station\"]", 4, "RTM-FOCUS-03", "Rotterdam Office - Floor 3", "Rotterdam Focus Room" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedUtc",
                table: "Reservations",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_Status_StartUtc_EndUtc",
                table: "Reservations",
                columns: new[] { "RoomId", "Status", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Code",
                table: "Rooms",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
