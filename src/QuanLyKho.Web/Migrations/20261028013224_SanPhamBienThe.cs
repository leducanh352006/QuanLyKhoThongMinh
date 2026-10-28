using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class SanPhamBienThe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SanPham",
                columns: table => new
                {
                    MaSP = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSP = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MaDM = table.Column<int>(type: "int", nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CoHanDung = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPham", x => x.MaSP);
                    table.ForeignKey(
                        name: "FK_SanPham_DanhMuc_MaDM",
                        column: x => x.MaDM,
                        principalTable: "DanhMuc",
                        principalColumn: "MaDM",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BienThe",
                columns: table => new
                {
                    MaBT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSP = table.Column<int>(type: "int", nullable: false),
                    MaVach = table.Column<string>(type: "varchar(50)", nullable: false),
                    ThuocTinh = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Dai = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Rong = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Cao = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    KhoiLuong = table.Column<decimal>(type: "decimal(10,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienThe", x => x.MaBT);
                    table.CheckConstraint("CK_BienThe_KichThuoc", "[Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuong] > 0");
                    table.ForeignKey(
                        name: "FK_BienThe_SanPham_MaSP",
                        column: x => x.MaSP,
                        principalTable: "SanPham",
                        principalColumn: "MaSP",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BienThe_MaSP",
                table: "BienThe",
                column: "MaSP");

            migrationBuilder.CreateIndex(
                name: "IX_BienThe_MaVach",
                table: "BienThe",
                column: "MaVach",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_MaDM",
                table: "SanPham",
                column: "MaDM");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BienThe");

            migrationBuilder.DropTable(
                name: "SanPham");
        }
    }
}
