using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Site
{
    /// <inheritdoc />
    public partial class AddBlockAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManagerUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ManagerDisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockAssignments_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockAssignments_BlockId",
                table: "BlockAssignments",
                column: "BlockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockAssignments");
        }
    }
}
