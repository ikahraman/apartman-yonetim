using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Site
{
    /// <inheritdoc />
    public partial class AddDaireType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DaireTypeId",
                table: "Units",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AppliesToDaireTypeId",
                table: "FeeSchedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DaireTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaireTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_DaireTypeId",
                table: "Units",
                column: "DaireTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_DaireTypes_DaireTypeId",
                table: "Units",
                column: "DaireTypeId",
                principalTable: "DaireTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_DaireTypes_DaireTypeId",
                table: "Units");

            migrationBuilder.DropTable(
                name: "DaireTypes");

            migrationBuilder.DropIndex(
                name: "IX_Units_DaireTypeId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "DaireTypeId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "AppliesToDaireTypeId",
                table: "FeeSchedules");
        }
    }
}
