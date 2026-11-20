using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server_ProductsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    BookingId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Bookings" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Bookings" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "Room" TEXT NOT NULL,
                    "Date" TEXT NOT NULL,
                    "Time" TEXT NOT NULL,
                    "Hours" INTEGER NOT NULL,
                    "PricePerHour" INTEGER NOT NULL,
                    "Total" INTEGER NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_CreatedAtUtc",
                table: "AuditEntries",
                column: "CreatedAtUtc");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Bookings_CreatedAtUtc\" ON \"Bookings\" (\"CreatedAtUtc\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}
