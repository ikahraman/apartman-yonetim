using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmanYonetim.Infrastructure.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddEgitimModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Egitimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: true),
                    Hedefler = table.Column<string>(type: "TEXT", nullable: true),
                    Gereksinimler = table.Column<string>(type: "TEXT", nullable: true),
                    SertifikaAdi = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Egitimler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EgitimDonemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EgitimId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BaslangicTarihi = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BitisTarihi = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kontenjan = table.Column<int>(type: "INTEGER", nullable: false),
                    Fiyat = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tur = table.Column<int>(type: "INTEGER", nullable: false),
                    Konum = table.Column<string>(type: "TEXT", nullable: true),
                    OnlinePlatform = table.Column<string>(type: "TEXT", nullable: true),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitimDonemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EgitimDonemleri_Egitimler_EgitimId",
                        column: x => x.EgitimId,
                        principalTable: "Egitimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DersProgramlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SiraNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Baslik = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: true),
                    DersTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SureDakika = table.Column<int>(type: "INTEGER", nullable: false),
                    IcerikMetin = table.Column<string>(type: "TEXT", nullable: true),
                    IcerikLink = table.Column<string>(type: "TEXT", nullable: true),
                    VideoLink = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersProgramlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DersProgramlari_EgitimDonemleri_DonemId",
                        column: x => x.DonemId,
                        principalTable: "EgitimDonemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kursiyerler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Sehir = table.Column<string>(type: "TEXT", nullable: true),
                    Meslek = table.Column<string>(type: "TEXT", nullable: true),
                    OdemeDurumu = table.Column<int>(type: "INTEGER", nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    KayitTarihi = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SertifikaVerildi = table.Column<bool>(type: "INTEGER", nullable: false),
                    SertifikaNo = table.Column<string>(type: "TEXT", nullable: true),
                    SertifikaTarihi = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notlar = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kursiyerler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kursiyerler_EgitimDonemleri_DonemId",
                        column: x => x.DonemId,
                        principalTable: "EgitimDonemleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DersTakipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KursiyerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DersProgramiId = table.Column<int>(type: "INTEGER", nullable: false),
                    Katildi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Not = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersTakipleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DersTakipleri_DersProgramlari_DersProgramiId",
                        column: x => x.DersProgramiId,
                        principalTable: "DersProgramlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DersTakipleri_Kursiyerler_KursiyerId",
                        column: x => x.KursiyerId,
                        principalTable: "Kursiyerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DersProgramlari_DonemId",
                table: "DersProgramlari",
                column: "DonemId");

            migrationBuilder.CreateIndex(
                name: "IX_DersTakipleri_DersProgramiId",
                table: "DersTakipleri",
                column: "DersProgramiId");

            migrationBuilder.CreateIndex(
                name: "IX_DersTakipleri_KursiyerId_DersProgramiId",
                table: "DersTakipleri",
                columns: new[] { "KursiyerId", "DersProgramiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EgitimDonemleri_EgitimId",
                table: "EgitimDonemleri",
                column: "EgitimId");

            migrationBuilder.CreateIndex(
                name: "IX_Kursiyerler_DonemId",
                table: "Kursiyerler",
                column: "DonemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DersTakipleri");

            migrationBuilder.DropTable(
                name: "DersProgramlari");

            migrationBuilder.DropTable(
                name: "Kursiyerler");

            migrationBuilder.DropTable(
                name: "EgitimDonemleri");

            migrationBuilder.DropTable(
                name: "Egitimler");
        }
    }
}
