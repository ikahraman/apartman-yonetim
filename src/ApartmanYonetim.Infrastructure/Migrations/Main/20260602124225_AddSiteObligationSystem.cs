using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddSiteObligationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteBillingConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PricePerDaire = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PricePerBlok = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PricePerKisim = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MinimumMonthly = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DefaultPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteBillingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteObligations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirmSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SiteType = table.Column<int>(type: "INTEGER", nullable: false),
                    DaireCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BlokCount = table.Column<int>(type: "INTEGER", nullable: false),
                    KisimCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BillingPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePerDaire = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PricePerBlok = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PricePerKisim = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteObligations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteObligationPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObligationId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodLabel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteObligationPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteObligationPayments_SiteObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "SiteObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteObligationPayments_ObligationId",
                table: "SiteObligationPayments",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteObligations_SiteId",
                table: "SiteObligations",
                column: "SiteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteBillingConfigs");

            migrationBuilder.DropTable(
                name: "SiteObligationPayments");

            migrationBuilder.DropTable(
                name: "SiteObligations");
        }
    }
}
