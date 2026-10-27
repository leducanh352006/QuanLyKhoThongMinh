using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class KhoViTri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kho",
                columns: table => new
                {
                    MaKho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKho = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    LoaiKho = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    MaQuanLy = table.Column<int>(type: "int", nullable: false),
                    ThoiGianCho = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Hoạt động")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kho", x => x.MaKho);
                    table.CheckConstraint("CK_Kho_LoaiKho", "[LoaiKho] IN (N'Kho bán hàng', N'Kho lưu trữ')");
                    table.CheckConstraint("CK_Kho_ThoiGianCho", "[ThoiGianCho] >= 0");
                    table.CheckConstraint("CK_Kho_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
                    table.ForeignKey(
                        name: "FK_Kho_NguoiDung_MaQuanLy",
                        column: x => x.MaQuanLy,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViTri",
                columns: table => new
                {
                    MaViTri = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenViTri = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    MaKho = table.Column<int>(type: "int", nullable: false),
                    MaViTriCha = table.Column<int>(type: "int", nullable: true),
                    CapViTri = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    LoaiViTri = table.Column<string>(type: "nvarchar(15)", nullable: true),
                    MaVachO = table.Column<string>(type: "varchar(50)", nullable: true),
                    Dai = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Rong = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Cao = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    KhoiLuongToiDa = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    DangKhoa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Hoạt động")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViTri", x => x.MaViTri);
                    table.UniqueConstraint("AK_ViTri_MaViTri_MaKho", x => new { x.MaViTri, x.MaKho });
                    table.CheckConstraint("CK_ViTri_CapViTri", "[CapViTri] IN (N'Tòa', N'Tầng', N'Phòng', N'Dãy', N'Hàng', N'Kệ', N'Ô')");
                    table.CheckConstraint("CK_ViTri_ChiOCoLoaiVaMaVach", "([CapViTri] = N'Ô' AND [LoaiViTri] IS NOT NULL AND [MaVachO] IS NOT NULL) OR ([CapViTri] <> N'Ô' AND [LoaiViTri] IS NULL AND [MaVachO] IS NULL)");
                    table.CheckConstraint("CK_ViTri_KichThuoc", "([CapViTri] = N'Ô' AND [LoaiViTri] = N'Lưu trữ' AND [Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuongToiDa] > 0) OR ([CapViTri] = N'Ô' AND [LoaiViTri] = N'Tiếp nhận' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL) OR ([CapViTri] <> N'Ô' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL)");
                    table.CheckConstraint("CK_ViTri_LoaiViTri", "[LoaiViTri] IS NULL OR [LoaiViTri] IN (N'Lưu trữ', N'Tiếp nhận')");
                    table.CheckConstraint("CK_ViTri_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
                    table.ForeignKey(
                        name: "FK_ViTri_Kho_MaKho",
                        column: x => x.MaKho,
                        principalTable: "Kho",
                        principalColumn: "MaKho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViTri_ViTri_MaViTriCha_MaKho",
                        columns: x => new { x.MaViTriCha, x.MaKho },
                        principalTable: "ViTri",
                        principalColumns: new[] { "MaViTri", "MaKho" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kho_MaQuanLy",
                table: "Kho",
                column: "MaQuanLy");

            migrationBuilder.CreateIndex(
                name: "IX_Kho_TenKho",
                table: "Kho",
                column: "TenKho",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_MaKho_MaViTriCha_TenViTri",
                table: "ViTri",
                columns: new[] { "MaKho", "MaViTriCha", "TenViTri" },
                unique: true,
                filter: "[MaViTriCha] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_MaVachO",
                table: "ViTri",
                column: "MaVachO",
                unique: true,
                filter: "[MaVachO] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_MaViTriCha_MaKho",
                table: "ViTri",
                columns: new[] { "MaViTriCha", "MaKho" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ViTri");

            migrationBuilder.DropTable(
                name: "Kho");
        }
    }
}
