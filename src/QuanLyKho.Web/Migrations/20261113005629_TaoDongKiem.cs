using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TaoDongKiem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DongKiem (Bảng A.7) thay cho ChiTietPhieuKiem của T3-4: tạo bảng mới, chép số hệ thống đã chụp sang, rồi mới bỏ bảng cũ.
            migrationBuilder.CreateTable(
                name: "DongKiem",
                columns: table => new
                {
                    MaDK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPK = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    SLHeThong = table.Column<int>(type: "int", nullable: false),
                    SLThucTe = table.Column<int>(type: "int", nullable: true),
                    LoaiPhatSinh = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    NguyenNhan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaViTriDung = table.Column<int>(type: "int", nullable: true),
                    MaBTDung = table.Column<int>(type: "int", nullable: true),
                    MaNguoiDem = table.Column<int>(type: "int", nullable: true),
                    TrangThaiXuLy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chưa kiểm"),
                    MaNguoiDuyet = table.Column<int>(type: "int", nullable: true),
                    ThoiGianDuyet = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongKiem", x => x.MaDK);
                    table.CheckConstraint("CK_DongKiem_ChuaKiem", "([TrangThaiXuLy] = N'Chưa kiểm' AND [SLThucTe] IS NULL) OR ([TrangThaiXuLy] <> N'Chưa kiểm' AND [SLThucTe] IS NOT NULL)");
                    table.CheckConstraint("CK_DongKiem_LoaiPhatSinh", "[LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] IN (N'Thiếu hoặc thừa', N'Hư hỏng hoặc hết hạn', N'Sai vị trí', N'Sai mã hàng')");
                    table.CheckConstraint("CK_DongKiem_MaBTDung", "([LoaiPhatSinh] = N'Sai mã hàng' AND [MaBTDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai mã hàng') AND [MaBTDung] IS NULL)");
                    table.CheckConstraint("CK_DongKiem_MaViTriDung", "([LoaiPhatSinh] = N'Sai vị trí' AND [MaViTriDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai vị trí') AND [MaViTriDung] IS NULL)");
                    table.CheckConstraint("CK_DongKiem_PhatSinh", "[TrangThaiXuLy] NOT IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối') OR [LoaiPhatSinh] IS NOT NULL");
                    table.CheckConstraint("CK_DongKiem_SLHeThong", "[SLHeThong] >= 0");
                    table.CheckConstraint("CK_DongKiem_SLThucTe", "[SLThucTe] IS NULL OR [SLThucTe] >= 0");
                    table.CheckConstraint("CK_DongKiem_TrangThaiXuLy", "[TrangThaiXuLy] IN (N'Chưa kiểm', N'Khớp', N'Chờ duyệt', N'Đã duyệt', N'Từ chối')");
                    table.ForeignKey(
                        name: "FK_DongKiem_BienThe_MaBTDung",
                        column: x => x.MaBTDung,
                        principalTable: "BienThe",
                        principalColumn: "MaBT",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_NguoiDung_MaNguoiDem",
                        column: x => x.MaNguoiDem,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_NguoiDung_MaNguoiDuyet",
                        column: x => x.MaNguoiDuyet,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_PhieuKiem_MaPK",
                        column: x => x.MaPK,
                        principalTable: "PhieuKiem",
                        principalColumn: "MaPK",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongKiem_ViTri_MaViTriDung",
                        column: x => x.MaViTriDung,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaBTDung",
                table: "DongKiem",
                column: "MaBTDung");

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaLo",
                table: "DongKiem",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaNguoiDem",
                table: "DongKiem",
                column: "MaNguoiDem");

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaNguoiDuyet",
                table: "DongKiem",
                column: "MaNguoiDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaPK_MaViTri_MaLo",
                table: "DongKiem",
                columns: new[] { "MaPK", "MaViTri", "MaLo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaViTri",
                table: "DongKiem",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_DongKiem_MaViTriDung",
                table: "DongKiem",
                column: "MaViTriDung");

            migrationBuilder.Sql(
                "INSERT INTO [DongKiem] ([MaPK], [MaViTri], [MaLo], [SLHeThong]) " +
                "SELECT [MaPK], [MaViTri], [MaLo], SUM([SoLuongHeThong]) FROM [ChiTietPhieuKiem] GROUP BY [MaPK], [MaViTri], [MaLo];");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuKiem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChiTietPhieuKiem",
                columns: table => new
                {
                    MaCTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBT = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaPK = table.Column<int>(type: "int", nullable: false),
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

            // Chép ngược số hệ thống (bỏ các dòng hàng ngoài sổ, vốn không có ở bảng cũ); số đếm thực tế mất khi hoàn tác.
            migrationBuilder.Sql(
                "INSERT INTO [ChiTietPhieuKiem] ([MaPK], [MaBT], [MaLo], [MaViTri], [SoLuongHeThong]) " +
                "SELECT d.[MaPK], l.[MaBT], d.[MaLo], d.[MaViTri], d.[SLHeThong] FROM [DongKiem] d " +
                "JOIN [LoHang] l ON l.[MaLo] = d.[MaLo] WHERE d.[SLHeThong] > 0;");

            migrationBuilder.DropTable(
                name: "DongKiem");
        }
    }
}
