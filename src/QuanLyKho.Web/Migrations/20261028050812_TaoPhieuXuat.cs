using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TaoPhieuXuat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuXuat",
                columns: table => new
                {
                    MaPX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MaKho = table.Column<int>(type: "int", nullable: false),
                    DoiTuong = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaPNGoc = table.Column<int>(type: "int", nullable: true),
                    MaNguoiLap = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDuyet = table.Column<int>(type: "int", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DienGiai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Nháp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuXuat", x => x.MaPX);
                    table.CheckConstraint("CK_PhieuXuat_HuyHang", "[LoaiPhieu] <> N'Hủy hàng' OR [LyDo] IS NOT NULL");
                    table.CheckConstraint("CK_PhieuXuat_LoaiPhieu", "[LoaiPhieu] IN (N'Khác', N'Trả lại hàng mua', N'Hủy hàng', N'Thiếu do kiểm kê')");
                    table.CheckConstraint("CK_PhieuXuat_NguoiDuyet", "[TrangThai] = N'Nháp' OR [TrangThai] = N'Đã hủy' OR [MaNguoiDuyet] IS NOT NULL");
                    table.CheckConstraint("CK_PhieuXuat_TraHangMua", "([LoaiPhieu] <> N'Trả lại hàng mua' OR [MaPNGoc] IS NOT NULL) AND ([LoaiPhieu] = N'Trả lại hàng mua' OR [MaPNGoc] IS NULL)");
                    table.CheckConstraint("CK_PhieuXuat_TrangThai", "[TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đang lấy hàng', N'Đã đối chiếu', N'Hoàn tất', N'Đã hủy')");
                    table.ForeignKey(
                        name: "FK_PhieuXuat_Kho_MaKho",
                        column: x => x.MaKho,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NguoiDung_MaNguoiDuyet",
                        column: x => x.MaNguoiDuyet,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NguoiDung_MaNguoiLap",
                        column: x => x.MaNguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_PhieuNhap_MaPNGoc",
                        column: x => x.MaPNGoc,
                        principalTable: "PhieuNhap",
                        principalColumn: "MaPN",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuXuat",
                columns: table => new
                {
                    MaCTX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPX = table.Column<int>(type: "int", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuXuat", x => x.MaCTX);
                    table.CheckConstraint("CK_ChiTietPhieuXuat_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_PhieuXuat_MaPX",
                        column: x => x.MaPX,
                        principalTable: "PhieuXuat",
                        principalColumn: "MaPX",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_MaBT",
                table: "ChiTietPhieuXuat",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_MaPX",
                table: "ChiTietPhieuXuat",
                column: "MaPX");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaKho_ThoiGian",
                table: "PhieuXuat",
                columns: new[] { "MaKho", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaNguoiDuyet",
                table: "PhieuXuat",
                column: "MaNguoiDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaNguoiLap",
                table: "PhieuXuat",
                column: "MaNguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaPNGoc",
                table: "PhieuXuat",
                column: "MaPNGoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_SoPhieu",
                table: "PhieuXuat",
                column: "SoPhieu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuXuat");

            migrationBuilder.DropTable(
                name: "PhieuXuat");
        }
    }
}
