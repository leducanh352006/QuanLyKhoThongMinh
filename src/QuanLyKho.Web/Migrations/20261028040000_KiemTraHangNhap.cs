using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class KiemTraHangNhap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyDoLech",
                table: "ChiTietPhieuNhap",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaLo",
                table: "ChiTietPhieuNhap",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SLDat",
                table: "ChiTietPhieuNhap",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SLThucNhan",
                table: "ChiTietPhieuNhap",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap",
                column: "MaLo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuNhap_LyDoLech",
                table: "ChiTietPhieuNhap",
                sql: "[SLThucNhan] IS NULL OR [SLThucNhan] = [SLDuKien] OR [LyDoLech] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuNhap_SLDat",
                table: "ChiTietPhieuNhap",
                sql: "[SLDat] >= 0 AND [SLDat] <= [SLThucNhan]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuNhap_SLThucNhan",
                table: "ChiTietPhieuNhap",
                sql: "[SLThucNhan] >= 0");

            // Khóa ngoại kép (MaLo, MaBT) -> LoHang (Bảng 2.22). Viết bằng SQL vì EF cần khóa thay thế ở LoHang (Area của nhóm khác);
            // LoHang đã có chỉ mục duy nhất (MaLo, MaBT). Khi MaLo còn NULL (chưa tiếp nhận) SQL Server không kiểm tra khóa này.
            migrationBuilder.Sql("ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [FK_ChiTietPhieuNhap_LoHang_MaLo_MaBT] FOREIGN KEY ([MaLo], [MaBT]) REFERENCES [LoHang] ([MaLo], [MaBT])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT [FK_ChiTietPhieuNhap_LoHang_MaLo_MaBT]");

            migrationBuilder.DropCheckConstraint(name: "CK_ChiTietPhieuNhap_LyDoLech", table: "ChiTietPhieuNhap");
            migrationBuilder.DropCheckConstraint(name: "CK_ChiTietPhieuNhap_SLDat", table: "ChiTietPhieuNhap");
            migrationBuilder.DropCheckConstraint(name: "CK_ChiTietPhieuNhap_SLThucNhan", table: "ChiTietPhieuNhap");

            migrationBuilder.DropIndex(name: "IX_ChiTietPhieuNhap_MaLo", table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(name: "LyDoLech", table: "ChiTietPhieuNhap");
            migrationBuilder.DropColumn(name: "MaLo", table: "ChiTietPhieuNhap");
            migrationBuilder.DropColumn(name: "SLDat", table: "ChiTietPhieuNhap");
            migrationBuilder.DropColumn(name: "SLThucNhan", table: "ChiTietPhieuNhap");
        }
    }
}
