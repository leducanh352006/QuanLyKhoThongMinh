using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class PhieuNhapThua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaPK",
                table: "PhieuNhap",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaPK",
                table: "PhieuNhap",
                column: "MaPK");

            // Chưa có khóa ngoại MaPK -> PhieuKiem: bảng PhieuKiem do nhóm Kiểm tra hàng hóa tạo sau, nhóm đó thêm khóa ngoại khi có bảng.
            migrationBuilder.AddCheckConstraint(
                name: "CK_PhieuNhap_ThuaCoPhieuKiem",
                table: "PhieuNhap",
                sql: "([LoaiPhieu] = N'Thừa do kiểm kê' AND [MaPK] IS NOT NULL) OR ([LoaiPhieu] <> N'Thừa do kiểm kê' AND [MaPK] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_PhieuNhap_ThuaCoPhieuKiem", table: "PhieuNhap");
            migrationBuilder.DropIndex(name: "IX_PhieuNhap_MaPK", table: "PhieuNhap");
            migrationBuilder.DropColumn(name: "MaPK", table: "PhieuNhap");
        }
    }
}
