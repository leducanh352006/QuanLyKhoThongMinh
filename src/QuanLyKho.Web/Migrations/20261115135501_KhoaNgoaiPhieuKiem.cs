using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class KhoaNgoaiPhieuKiem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_PhieuNhap_PhieuKiem_MaPK",
                table: "PhieuNhap",
                column: "MaPK",
                principalTable: "PhieuKiem",
                principalColumn: "MaPK",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuXuat_PhieuKiem_MaPK",
                table: "PhieuXuat",
                column: "MaPK",
                principalTable: "PhieuKiem",
                principalColumn: "MaPK",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhieuNhap_PhieuKiem_MaPK",
                table: "PhieuNhap");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuXuat_PhieuKiem_MaPK",
                table: "PhieuXuat");
        }
    }
}
