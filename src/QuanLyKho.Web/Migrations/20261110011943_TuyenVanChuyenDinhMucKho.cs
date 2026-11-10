using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class TuyenVanChuyenDinhMucKho : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_DinhMucKho_MaBT",
                table: "DinhMucKho",
                column: "MaBT");

            migrationBuilder.CreateIndex(
                name: "IX_TuyenVanChuyen_MaKhoNhap",
                table: "TuyenVanChuyen",
                column: "MaKhoNhap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DinhMucKho");

            migrationBuilder.DropTable(
                name: "TuyenVanChuyen");
        }
    }
}
