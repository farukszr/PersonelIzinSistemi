
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonelIzinSistemi.Migrations
{
    public partial class IzinTalebiArsivleme : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ArsivlendiMi",
                table: "IzinTalepleri",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArsivlendiMi",
                table: "IzinTalepleri");
        }
    }
}
