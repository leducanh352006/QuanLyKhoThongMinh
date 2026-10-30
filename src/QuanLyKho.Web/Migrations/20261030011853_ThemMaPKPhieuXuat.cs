using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class ThemMaPKPhieuXuat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaPK",
                table: "PhieuXuat",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaPK",
                table: "PhieuXuat",
                column: "MaPK",
                unique: true,
                filter: "[LoaiPhieu] = N'Thiếu do kiểm kê'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhieuXuat_ThieuCoPhieuKiem",
                table: "PhieuXuat",
                sql: "([LoaiPhieu] = N'Thiếu do kiểm kê' AND [MaPK] IS NOT NULL) OR ([LoaiPhieu] <> N'Thiếu do kiểm kê' AND [MaPK] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhieuXuat_MaPK",
                table: "PhieuXuat");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhieuXuat_ThieuCoPhieuKiem",
                table: "PhieuXuat");

            migrationBuilder.DropColumn(
                name: "MaPK",
                table: "PhieuXuat");
        }
    }
}
