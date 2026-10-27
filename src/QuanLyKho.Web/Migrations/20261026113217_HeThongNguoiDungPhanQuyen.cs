using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class HeThongNguoiDungPhanQuyen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    MaVaiTro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenVaiTro = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.MaVaiTro);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    MaND = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", nullable: true),
                    SDT = table.Column<string>(type: "varchar(15)", nullable: true),
                    MaVaiTro = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Hoạt động")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.MaND);
                    table.CheckConstraint("CK_NguoiDung_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
                    table.ForeignKey(
                        name: "FK_NguoiDung_VaiTro_MaVaiTro",
                        column: x => x.MaVaiTro,
                        principalTable: "VaiTro",
                        principalColumn: "MaVaiTro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhanQuyen",
                columns: table => new
                {
                    MaVaiTro = table.Column<int>(type: "int", nullable: false),
                    LoaiChungTu = table.Column<string>(type: "char(2)", nullable: false),
                    CoXem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CoThem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CoSua = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CoXoa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CoDuyet = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanQuyen", x => new { x.MaVaiTro, x.LoaiChungTu });
                    table.CheckConstraint("CK_PhanQuyen_LoaiChungTu", "[LoaiChungTu] IN ('NK', 'XK', 'CK', 'KK')");
                    table.ForeignKey(
                        name: "FK_PhanQuyen_VaiTro_MaVaiTro",
                        column: x => x.MaVaiTro,
                        principalTable: "VaiTro",
                        principalColumn: "MaVaiTro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaVaiTro",
                table: "NguoiDung",
                column: "MaVaiTro");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_TenDangNhap",
                table: "NguoiDung",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_TenVaiTro",
                table: "VaiTro",
                column: "TenVaiTro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "PhanQuyen");

            migrationBuilder.DropTable(
                name: "VaiTro");
        }
    }
}
