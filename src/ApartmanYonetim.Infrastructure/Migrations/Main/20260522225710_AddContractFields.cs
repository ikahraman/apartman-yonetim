using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
