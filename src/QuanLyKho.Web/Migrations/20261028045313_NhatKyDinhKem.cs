using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class NhatKyDinhKem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhatKyThaoTac",
                columns: table => new
                {
                    MaNK = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaND = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanhDong = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    LoaiCT = table.Column<string>(type: "varchar(2)", nullable: true),
                    MaCT = table.Column<int>(type: "int", nullable: true),
                    ChiTiet = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyThaoTac", x => x.MaNK);
                    table.CheckConstraint("CK_NhatKyThaoTac_ChungTu", "([LoaiCT] IS NULL AND [MaCT] IS NULL) OR ([LoaiCT] IS NOT NULL AND [MaCT] IS NOT NULL)");
                    table.CheckConstraint("CK_NhatKyThaoTac_LoaiCT", "[LoaiCT] IS NULL OR [LoaiCT] IN ('NK', 'XK', 'CK', 'KK')");
                    table.ForeignKey(
                        name: "FK_NhatKyThaoTac_NguoiDung_MaND",
                        column: x => x.MaND,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TepDinhKem",
                columns: table => new
                {
                    MaTep = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiCT = table.Column<string>(type: "varchar(2)", nullable: false),
                    MaCT = table.Column<int>(type: "int", nullable: false),
                    TenTep = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    MaND = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDinhKem", x => x.MaTep);
                    table.CheckConstraint("CK_TepDinhKem_LoaiCT", "[LoaiCT] IN ('NK', 'XK', 'CK', 'KK')");
                    table.ForeignKey(
                        name: "FK_TepDinhKem_NguoiDung_MaND",
                        column: x => x.MaND,
                        principalTable: "NguoiDung",
                        principalColumn: "MaND",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyThaoTac_LoaiCT_MaCT",
                table: "NhatKyThaoTac",
                columns: new[] { "LoaiCT", "MaCT" });

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyThaoTac_MaND",
                table: "NhatKyThaoTac",
                column: "MaND");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyThaoTac_ThoiGian",
                table: "NhatKyThaoTac",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKem_LoaiCT_MaCT",
                table: "TepDinhKem",
                columns: new[] { "LoaiCT", "MaCT" });

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKem_MaND",
                table: "TepDinhKem",
                column: "MaND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhatKyThaoTac");

            migrationBuilder.DropTable(
                name: "TepDinhKem");
        }
    }
}
