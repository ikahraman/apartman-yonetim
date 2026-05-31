using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Site
{
    /// <inheritdoc />
    public partial class AddBlocksUnitTypeArsaPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ArsaPay",
                table: "Units",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BlockId",
                table: "Units",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitType",
                table: "Units",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToUnitType",
                table: "FeeSchedules",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistributionType",
                table: "FeeSchedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    FloorCount = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitCount = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_BlockId",
                table: "Units",
                column: "BlockId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Blocks_BlockId",
                table: "Units",
                column: "BlockId",
                principalTable: "Blocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Blocks_BlockId",
                table: "Units");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Units_BlockId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ArsaPay",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "BlockId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "AppliesToUnitType",
                table: "FeeSchedules");

            migrationBuilder.DropColumn(
                name: "DistributionType",
                table: "FeeSchedules");
        }
    }
}
