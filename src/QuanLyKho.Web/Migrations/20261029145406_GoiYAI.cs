using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class GoiYAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoiYAI",
                columns: table => new
                {
                    MaGoiY = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiGoiY = table.Column<string>(type: "char(2)", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    MaNguoiDuyet = table.Column<int>(type: "int", nullable: true),
                    ThoiGianDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaPX = table.Column<int>(type: "int", nullable: true),
                    MaPN = table.Column<int>(type: "int", nullable: true),
                    MaPC = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiYAI", x => x.MaGoiY);
                    table.CheckConstraint("CK_GoiYAI_LoaiGoiY", "[LoaiGoiY] IN ('G1', 'G2', 'G3', 'G4')");
                    table.CheckConstraint("CK_GoiYAI_NguoiDuyet", "([TrangThai] = N'Chờ duyệt' AND [MaNguoiDuyet] IS NULL AND [ThoiGianDuyet] IS NULL) OR ([TrangThai] <> N'Chờ duyệt' AND [MaNguoiDuyet] IS NOT NULL AND [ThoiGianDuyet] IS NOT NULL)");
                    table.CheckConstraint("CK_GoiYAI_ThamChieu", "([LoaiGoiY] = 'G1' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G2' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G3' AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G4' AND [MaPX] IS NULL AND [MaPN] IS NULL)");
                    table.CheckConstraint("CK_GoiYAI_TrangThai", "[TrangThai] IN (N'Chờ duyệt', N'Đã duyệt', N'Đã sửa và duyệt', N'Từ chối')");
                    table.ForeignKey(
                        name: "FK_GoiYAI_NguoiDung_MaNguoiDuyet",
                        column: x => x.MaNguoiDuyet,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoiYAI_PhieuNhap_MaPN",
                        column: x => x.MaPN,
                        principalTable: "PhieuNhap",
                        principalColumn: "MaPN",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietGoiY",
                columns: table => new
                {
                    MaCTGY = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGoiY = table.Column<int>(type: "int", nullable: false),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: true),
                    MaViTri = table.Column<int>(type: "int", nullable: true),
                    MaKhoNguon = table.Column<int>(type: "int", nullable: true),
                    MaKhoDich = table.Column<int>(type: "int", nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    LaBanSua = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietGoiY", x => x.MaCTGY);
                    table.CheckConstraint("CK_ChiTietGoiY_KhoKhac", "[MaKhoNguon] IS NULL OR [MaKhoDich] IS NULL OR [MaKhoNguon] <> [MaKhoDich]");
                    table.CheckConstraint("CK_ChiTietGoiY_LyDo", "LEN(LTRIM(RTRIM([LyDo]))) > 0");
                    table.CheckConstraint("CK_ChiTietGoiY_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_BienThe_MaBT",
                        column: x => x.MaBT,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_GoiYAI_MaGoiY",
                        column: x => x.MaGoiY,
                        principalTable: "GoiYAI",
                        principalColumn: "MaGoiY",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_Kho_MaKhoDich",
                        column: x => x.MaKhoDich,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_Kho_MaKhoNguon",
                        column: x => x.MaKhoNguon,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiY_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaBT",
                table: "ChiTietGoiY",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaGoiY",
                table: "ChiTietGoiY",
                column: "MaGoiY");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaKhoDich",
                table: "ChiTietGoiY",
                column: "MaKhoDich");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaKhoNguon",
                table: "ChiTietGoiY",
                column: "MaKhoNguon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaLo",
                table: "ChiTietGoiY",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiY_MaViTri",
                table: "ChiTietGoiY",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_GoiYAI_MaNguoiDuyet",
                table: "GoiYAI",
                column: "MaNguoiDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_GoiYAI_MaPN",
                table: "GoiYAI",
                column: "MaPN");

            migrationBuilder.CreateIndex(
                name: "IX_GoiYAI_MaPX",
                table: "GoiYAI",
                column: "MaPX");

            migrationBuilder.CreateIndex(
                name: "IX_GoiYAI_TrangThai_LoaiGoiY",
                table: "GoiYAI",
                columns: new[] { "TrangThai", "LoaiGoiY" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietGoiY");

            migrationBuilder.DropTable(
                name: "GoiYAI");
        }
    }
}
