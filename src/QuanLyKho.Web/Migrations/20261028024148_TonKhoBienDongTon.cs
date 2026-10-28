using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TonKhoBienDongTon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BienDongTon",
                columns: table => new
                {
                    MaBD = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    LoaiBienDong = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    LoaiCT = table.Column<string>(type: "varchar(2)", nullable: false),
                    MaCT = table.Column<int>(type: "int", nullable: false),
                    MaND = table.Column<int>(type: "int", nullable: false),
                    ThoiGianUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienDongTon", x => x.MaBD);
                    table.CheckConstraint("CK_BienDongTon_LoaiBienDong", "[LoaiBienDong] IN (N'Nhận hàng', N'Sắp xếp', N'Xuất kho', N'Chuyển đi', N'Nhận chuyển', N'Đổi ô', N'Hoàn tác')");
                    table.CheckConstraint("CK_BienDongTon_LoaiCT", "[LoaiCT] IN ('NK', 'XK', 'CK', 'KK')");
                    table.CheckConstraint("CK_BienDongTon_SoLuongKhac0", "[SoLuong] <> 0");
                    table.ForeignKey(
                        name: "FK_BienDongTon_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienDongTon_NguoiDung_MaND",
                        column: x => x.MaND,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienDongTon_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TonKho",
                columns: table => new
                {
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SoLuongGiu = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PhienBan = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKho", x => new { x.MaLo, x.MaViTri });
                    table.CheckConstraint("CK_TonKho_GiuKhongVuotTon", "[SoLuongGiu] <= [SoLuongTon]");
                    table.CheckConstraint("CK_TonKho_KhongAm", "[SoLuongTon] >= 0 AND [SoLuongGiu] >= 0");
                    table.ForeignKey(
                        name: "FK_TonKho_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TonKho_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BienDongTon_LoaiCT_MaCT",
                table: "BienDongTon",
                columns: new[] { "LoaiCT", "MaCT" });

            migrationBuilder.CreateIndex(
                name: "IX_BienDongTon_MaLo_MaViTri",
                table: "BienDongTon",
                columns: new[] { "MaLo", "MaViTri" });

            migrationBuilder.CreateIndex(
                name: "IX_BienDongTon_MaND",
                table: "BienDongTon",
                column: "MaND");

            migrationBuilder.CreateIndex(
                name: "IX_BienDongTon_MaViTri",
                table: "BienDongTon",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_TonKho_MaViTri",
                table: "TonKho",
                column: "MaViTri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BienDongTon");

            migrationBuilder.DropTable(
                name: "TonKho");
        }
    }
}
