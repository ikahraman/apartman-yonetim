using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddSubscriptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirmPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MinSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxSiteCount = table.Column<int>(type: "INTEGER", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirmPaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirmSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PeriodYear = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmPaymentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirmSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirmSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FirmPackageId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ContractEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CustomMonthlyPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirmSubscriptions_FirmPackages_FirmPackageId",
                        column: x => x.FirmPackageId,
                        principalTable: "FirmPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirmPaymentRecords_FirmSlug_PeriodYear_PeriodMonth",
                table: "FirmPaymentRecords",
                columns: new[] { "FirmSlug", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirmSubscriptions_FirmPackageId",
                table: "FirmSubscriptions",
                column: "FirmPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmSubscriptions_FirmSlug",
                table: "FirmSubscriptions",
                column: "FirmSlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirmPaymentRecords");

            migrationBuilder.DropTable(
                name: "FirmSubscriptions");

            migrationBuilder.DropTable(
                name: "FirmPackages");
        }
    }
}
