using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class LoHangBangThat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoHang",
                columns: table => new
                {
                    MaLo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaVachLo = table.Column<string>(type: "varchar(50)", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    HanDung = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoHang", x => x.MaLo);
                    table.ForeignKey(
                        name: "FK_LoHang_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoHang_MaBT",
                table: "LoHang",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_LoHang_MaLo_MaBT",
                table: "LoHang",
                columns: new[] { "MaLo", "MaBT" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoHang_MaVachLo",
                table: "LoHang",
                column: "MaVachLo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoHang");
        }
    }
}
