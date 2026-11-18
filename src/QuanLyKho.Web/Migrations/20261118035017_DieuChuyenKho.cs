using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class DieuChuyenKho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DinhMucKho",
                columns: table => new
                {
                    MaKho = table.Column<int>(type: "int", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    TonToiThieu = table.Column<int>(type: "int", nullable: false),
                    NgayAnToan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DinhMucKho", x => new { x.MaKho, x.MaBT });
                    table.CheckConstraint("CK_DinhMucKho_KhongAm", "[TonToiThieu] >= 0 AND [NgayAnToan] >= 0");
                    table.ForeignKey(
                        name: "FK_DinhMucKho_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DinhMucKho_Kho_MaKho",
                        column: x => x.MaKho,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TuyenVanChuyen",
                columns: table => new
                {
                    MaKhoXuat = table.Column<int>(type: "int", nullable: false),
                    MaKhoNhap = table.Column<int>(type: "int", nullable: false),
                    DonGiaVC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhiChuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuyenVanChuyen", x => new { x.MaKhoXuat, x.MaKhoNhap });
                    table.CheckConstraint("CK_TuyenVanChuyen_KhacKho", "[MaKhoXuat] <> [MaKhoNhap]");
                    table.CheckConstraint("CK_TuyenVanChuyen_KhongAm", "[DonGiaVC] >= 0 AND [PhiChuyen] >= 0");
                    table.ForeignKey(
                        name: "FK_TuyenVanChuyen_Kho_MaKhoNhap",
                        column: x => x.MaKhoNhap,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TuyenVanChuyen_Kho_MaKhoXuat",
                        column: x => x.MaKhoXuat,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuChuyen",
                columns: table => new
                {
                    MaPC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    MaKhoXuat = table.Column<int>(type: "int", nullable: false),
                    MaKhoNhap = table.Column<int>(type: "int", nullable: false),
                    NguoiVanChuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhuongTien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaNguoiLap = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDuyet = table.Column<int>(type: "int", nullable: true),
                    DienGiai = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Nháp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuChuyen", x => x.MaPC);
                    table.CheckConstraint("CK_PhieuChuyen_KhacKho", "[MaKhoXuat] <> [MaKhoNhap]");
                    table.CheckConstraint("CK_PhieuChuyen_NguoiDuyet", "[TrangThai] IN (N'Nháp', N'Đã hủy') OR [MaNguoiDuyet] IS NOT NULL");
                    table.CheckConstraint("CK_PhieuChuyen_TrangThai", "[TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đang vận chuyển', N'Nhận một phần', N'Đã nhận', N'Đã hủy')");
                    table.ForeignKey(
                        name: "FK_PhieuChuyen_Kho_MaKhoNhap",
                        column: x => x.MaKhoNhap,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChuyen_Kho_MaKhoXuat",
                        column: x => x.MaKhoXuat,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChuyen_NguoiDung_MaNguoiDuyet",
                        column: x => x.MaNguoiDuyet,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChuyen_NguoiDung_MaNguoiLap",
                        column: x => x.MaNguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChuyen_TuyenVanChuyen_MaKhoXuat_MaKhoNhap",
                        columns: x => new { x.MaKhoXuat, x.MaKhoNhap },
                        principalTable: "TuyenVanChuyen",
                        principalColumns: new[] { "MaKhoXuat", "MaKhoNhap" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietChuyen",
                columns: table => new
                {
                    MaCTC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPC = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaViTriXuat = table.Column<int>(type: "int", nullable: false),
                    MaViTriNhap = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    SLNhan = table.Column<int>(type: "int", nullable: true),
                    LyDoLech = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    XuLyLech = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NguyenNhanLech = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietChuyen", x => x.MaCTC);
                    table.CheckConstraint("CK_ChiTietChuyen_KhacO", "[MaViTriXuat] <> [MaViTriNhap]");
                    table.CheckConstraint("CK_ChiTietChuyen_LyDoLech", "[SLNhan] IS NULL OR [SLNhan] = [SoLuong] OR [LyDoLech] IS NOT NULL");
                    table.CheckConstraint("CK_ChiTietChuyen_SLNhan", "[SLNhan] >= 0 AND [SLNhan] <= [SoLuong]");
                    table.CheckConstraint("CK_ChiTietChuyen_SoLuong", "[SoLuong] > 0");
                    table.CheckConstraint("CK_ChiTietChuyen_XuLyLech", "[XuLyLech] IS NULL OR ([XuLyLech] IN (N'Nhập tại kho đến', N'Trả về kho đi') AND [SLNhan] < [SoLuong] AND [NguyenNhanLech] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ChiTietChuyen_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietChuyen_PhieuChuyen_MaPC",
                        column: x => x.MaPC,
                        principalTable: "PhieuChuyen",
                        principalColumn: "MaPC",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietChuyen_ViTri_MaViTriNhap",
                        column: x => x.MaViTriNhap,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietChuyen_ViTri_MaViTriXuat",
                        column: x => x.MaViTriXuat,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoiYAI_MaPC",
                table: "GoiYAI",
                column: "MaPC");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuyen_MaLo",
                table: "ChiTietChuyen",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuyen_MaPC_MaLo_MaViTriNhap",
                table: "ChiTietChuyen",
                columns: new[] { "MaPC", "MaLo", "MaViTriNhap" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuyen_MaPC_MaLo_MaViTriXuat",
                table: "ChiTietChuyen",
                columns: new[] { "MaPC", "MaLo", "MaViTriXuat" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuyen_MaViTriNhap",
                table: "ChiTietChuyen",
                column: "MaViTriNhap");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuyen_MaViTriXuat",
                table: "ChiTietChuyen",
                column: "MaViTriXuat");

            migrationBuilder.CreateIndex(
                name: "IX_DinhMucKho_MaBT",
                table: "DinhMucKho",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_MaKhoNhap",
                table: "PhieuChuyen",
                column: "MaKhoNhap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_MaKhoXuat_MaKhoNhap",
                table: "PhieuChuyen",
                columns: new[] { "MaKhoXuat", "MaKhoNhap" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_MaKhoXuat_ThoiGian",
                table: "PhieuChuyen",
                columns: new[] { "MaKhoXuat", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_MaNguoiDuyet",
                table: "PhieuChuyen",
                column: "MaNguoiDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_MaNguoiLap",
                table: "PhieuChuyen",
                column: "MaNguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChuyen_SoPhieu",
                table: "PhieuChuyen",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TuyenVanChuyen_MaKhoNhap",
                table: "TuyenVanChuyen",
                column: "MaKhoNhap");

            migrationBuilder.AddForeignKey(
                name: "FK_GoiYAI_PhieuChuyen_MaPC",
                table: "GoiYAI",
                column: "MaPC",
                principalTable: "PhieuChuyen",
                principalColumn: "MaPC",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoiYAI_PhieuChuyen_MaPC",
                table: "GoiYAI");

            migrationBuilder.DropTable(
                name: "ChiTietChuyen");

            migrationBuilder.DropTable(
                name: "DinhMucKho");

            migrationBuilder.DropTable(
                name: "PhieuChuyen");

            migrationBuilder.DropTable(
                name: "TuyenVanChuyen");

            migrationBuilder.DropIndex(
                name: "IX_GoiYAI_MaPC",
                table: "GoiYAI");
        }
    }
}
