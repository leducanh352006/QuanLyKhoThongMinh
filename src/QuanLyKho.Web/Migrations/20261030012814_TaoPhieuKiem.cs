using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TaoPhieuKiem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuKiem",
                columns: table => new
                {
                    MaPK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaKho = table.Column<int>(type: "int", nullable: false),
                    LoaiKiemKe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DienGiai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MaNguoiLap = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Đang kiểm")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKiem", x => x.MaPK);
                    table.CheckConstraint("CK_PhieuKiem_LoaiKiemKe", "[LoaiKiemKe] IN (N'Toàn bộ', N'Một phần')");
                    table.CheckConstraint("CK_PhieuKiem_TrangThai", "[TrangThai] IN (N'Đang kiểm', N'Hoàn tất')");
                    table.ForeignKey(
                        name: "FK_PhieuKiem_Kho_MaKho",
                        column: x => x.MaKho,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKiem_NguoiDung_MaNguoiLap",
                        column: x => x.MaNguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuKiem",
                columns: table => new
                {
                    MaCTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPK = table.Column<int>(type: "int", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    SoLuongHeThong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuKiem", x => x.MaCTK);
                    table.CheckConstraint("CK_ChiTietPhieuKiem_SoLuongHeThong", "[SoLuongHeThong] >= 0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKiem_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKiem_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKiem_PhieuKiem_MaPK",
                        column: x => x.MaPK,
                        principalTable: "PhieuKiem",
                        principalColumn: "MaPK",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKiem_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThamGiaKiem",
                columns: table => new
                {
                    MaPK = table.Column<int>(type: "int", nullable: false),
                    MaND = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThamGiaKiem", x => new { x.MaPK, x.MaND });
                    table.ForeignKey(
                        name: "FK_ThamGiaKiem_NguoiDung_MaND",
                        column: x => x.MaND,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThamGiaKiem_PhieuKiem_MaPK",
                        column: x => x.MaPK,
                        principalTable: "PhieuKiem",
                        principalColumn: "MaPK",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKiem_MaBT",
                table: "ChiTietPhieuKiem",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKiem_MaLo",
                table: "ChiTietPhieuKiem",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKiem_MaPK",
                table: "ChiTietPhieuKiem",
                column: "MaPK");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKiem_MaViTri",
                table: "ChiTietPhieuKiem",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKiem_MaKho_ThoiGianBatDau",
                table: "PhieuKiem",
                columns: new[] { "MaKho", "ThoiGianBatDau" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKiem_MaNguoiLap",
                table: "PhieuKiem",
                column: "MaNguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKiem_SoPhieu",
                table: "PhieuKiem",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThamGiaKiem_MaND",
                table: "ThamGiaKiem",
                column: "MaND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuKiem");

            migrationBuilder.DropTable(
                name: "ThamGiaKiem");

            migrationBuilder.DropTable(
                name: "PhieuKiem");
        }
    }
}
