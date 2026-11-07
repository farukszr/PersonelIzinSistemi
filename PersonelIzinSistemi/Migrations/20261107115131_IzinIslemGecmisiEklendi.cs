using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonelIzinSistemi.Migrations
{
    /// <inheritdoc />
    public partial class IzinIslemGecmisiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IzinIslemGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IzinTalebiId = table.Column<int>(type: "int", nullable: false),
                    IslemTuru = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OncekiDurum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YeniDurum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IslemiYapanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IzinIslemGecmisleri", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IzinIslemGecmisleri");
        }
    }
}
