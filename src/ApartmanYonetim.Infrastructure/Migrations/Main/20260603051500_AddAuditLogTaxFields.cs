using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddAuditLogTaxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "FirmRegistrations",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "FirmRegistrations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Changes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EntityType_EntityId",
                table: "SystemAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_Timestamp",
                table: "SystemAuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "FirmRegistrations");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "FirmRegistrations");
        }
    }
}
