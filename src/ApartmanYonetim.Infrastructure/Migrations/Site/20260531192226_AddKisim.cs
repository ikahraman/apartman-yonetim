using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Site
{
    /// <inheritdoc />
    public partial class AddKisim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KisimId",
                table: "Blocks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kisimlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kisimlar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_KisimId",
                table: "Blocks",
                column: "KisimId");

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Kisimlar_KisimId",
                table: "Blocks",
                column: "KisimId",
                principalTable: "Kisimlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Kisimlar_KisimId",
                table: "Blocks");

            migrationBuilder.DropTable(
                name: "Kisimlar");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_KisimId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "KisimId",
                table: "Blocks");
        }
    }
}
