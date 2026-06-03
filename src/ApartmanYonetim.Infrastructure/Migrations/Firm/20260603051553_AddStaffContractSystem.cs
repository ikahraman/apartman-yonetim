using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Firm
{
    /// <inheritdoc />
    public partial class AddStaffContractSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSiteAccess");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ContractNotes",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MonthlyManagementFee",
                table: "Sites");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Companies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Companies",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyStaff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyStaff_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    FeeType = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyFee = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TerminationReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteContracts_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteStaffAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StaffId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteStaffAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteStaffAssignments_CompanyStaff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "CompanyStaff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteStaffAssignments_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStaff_CompanyId_UserId",
                table: "CompanyStaff",
                columns: new[] { "CompanyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteContracts_SiteId",
                table: "SiteContracts",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteStaffAssignments_SiteId",
                table: "SiteStaffAssignments",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteStaffAssignments_StaffId_SiteId",
                table: "SiteStaffAssignments",
                columns: new[] { "StaffId", "SiteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteContracts");

            migrationBuilder.DropTable(
                name: "SiteStaffAssignments");

            migrationBuilder.DropTable(
                name: "CompanyStaff");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEndDate",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNotes",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractStartDate",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyManagementFee",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSiteAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSiteAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSiteAccess_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSiteAccess_SiteId",
                table: "UserSiteAccess",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSiteAccess_UserId_SiteId",
                table: "UserSiteAccess",
                columns: new[] { "UserId", "SiteId" },
                unique: true);
        }
    }
}
