using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TaoPhieuNhap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuNhap",
                columns: table => new
                {
                    MaPN = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MaKho = table.Column<int>(type: "int", nullable: false),
                    MaNCC = table.Column<int>(type: "int", nullable: true),
                    MaNguoiLap = table.Column<int>(type: "int", nullable: false),
                    DienGiai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Nháp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhap", x => x.MaPN);
                    table.CheckConstraint("CK_PhieuNhap_LoaiPhieu", "[LoaiPhieu] IN (N'Mua hàng', N'Khác', N'Thừa do kiểm kê')");
                    table.CheckConstraint("CK_PhieuNhap_TrangThai", "[TrangThai] IN (N'Nháp', N'Đã xác nhận', N'Đã kiểm tra', N'Hoàn tất', N'Đã hủy')");
                    table.ForeignKey(
                        name: "FK_PhieuNhap_Kho_MaKho",
                        column: x => x.MaKho,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NguoiDung_MaNguoiLap",
                        column: x => x.MaNguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NhaCungCap_MaNCC",
                        column: x => x.MaNCC,
                        principalTable: "NhaCungCap",
                        principalColumn: "MaNCC",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuNhap",
                columns: table => new
                {
                    MaCTN = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPN = table.Column<int>(type: "int", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    SLDuKien = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuNhap", x => x.MaCTN);
                    table.CheckConstraint("CK_ChiTietPhieuNhap_SLDuKien", "[SLDuKien] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_PhieuNhap_MaPN",
                        column: x => x.MaPN,
                        principalTable: "PhieuNhap",
                        principalColumn: "MaPN",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaBT",
                table: "ChiTietPhieuNhap",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaPN",
                table: "ChiTietPhieuNhap",
                column: "MaPN");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaKho_ThoiGian",
                table: "PhieuNhap",
                columns: new[] { "MaKho", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaNCC",
                table: "PhieuNhap",
                column: "MaNCC");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaNguoiLap",
                table: "PhieuNhap",
                column: "MaNguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_SoPhieu",
                table: "PhieuNhap",
                column: "SoPhieu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuNhap");

            migrationBuilder.DropTable(
                name: "PhieuNhap");
        }
    }
}
