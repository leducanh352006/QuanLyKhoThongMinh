using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TaoSapXep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SapXep",
                columns: table => new
                {
                    MaSX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCTN = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chờ xác nhận"),
                    MaNguoiXN = table.Column<int>(type: "int", nullable: true),
                    ThoiGianXN = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SapXep", x => x.MaSX);
                    table.CheckConstraint("CK_SapXep_SoLuong", "[SoLuong] > 0");
                    table.CheckConstraint("CK_SapXep_TrangThai", "[TrangThai] IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã hủy')");
                    table.CheckConstraint("CK_SapXep_XacNhan", "([TrangThai] = N'Đã xác nhận' AND [MaNguoiXN] IS NOT NULL AND [ThoiGianXN] IS NOT NULL) OR ([TrangThai] <> N'Đã xác nhận' AND [MaNguoiXN] IS NULL AND [ThoiGianXN] IS NULL)");
                    table.ForeignKey(
                        name: "FK_SapXep_ChiTietPhieuNhap_MaCTN",
                        column: x => x.MaCTN,
                        principalTable: "ChiTietPhieuNhap",
                        principalColumn: "MaCTN",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SapXep_NguoiDung_MaNguoiXN",
                        column: x => x.MaNguoiXN,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SapXep_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SapXep_MaCTN",
                table: "SapXep",
                column: "MaCTN");

            migrationBuilder.CreateIndex(
                name: "IX_SapXep_MaNguoiXN",
                table: "SapXep",
                column: "MaNguoiXN");

            migrationBuilder.CreateIndex(
                name: "IX_SapXep_MaViTri_TrangThai",
                table: "SapXep",
                columns: new[] { "MaViTri", "TrangThai" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SapXep");
        }
    }
}
