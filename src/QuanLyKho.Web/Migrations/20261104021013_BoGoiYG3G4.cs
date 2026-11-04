using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class BoGoiYG3G4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dữ liệu G3/G4 (thường chỉ là gợi ý mẫu ở Development) phải xóa trước khi siết ràng buộc, nếu không CHECK mới sẽ lỗi.
            migrationBuilder.Sql("DELETE FROM ChiTietGoiY WHERE MaGoiY IN (SELECT MaGoiY FROM GoiYAI WHERE LoaiGoiY IN ('G3', 'G4'))");
            migrationBuilder.Sql("DELETE FROM GoiYAI WHERE LoaiGoiY IN ('G3', 'G4')");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoiYAI_LoaiGoiY",
                table: "GoiYAI");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoiYAI_ThamChieu",
                table: "GoiYAI");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoiYAI_LoaiGoiY",
                table: "GoiYAI",
                sql: "[LoaiGoiY] IN ('G1', 'G2')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoiYAI_ThamChieu",
                table: "GoiYAI",
                sql: "([LoaiGoiY] = 'G1' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G2' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GoiYAI_LoaiGoiY",
                table: "GoiYAI");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GoiYAI_ThamChieu",
                table: "GoiYAI");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoiYAI_LoaiGoiY",
                table: "GoiYAI",
                sql: "[LoaiGoiY] IN ('G1', 'G2', 'G3', 'G4')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GoiYAI_ThamChieu",
                table: "GoiYAI",
                sql: "([LoaiGoiY] = 'G1' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G2' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G3' AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G4' AND [MaPX] IS NULL AND [MaPN] IS NULL)");
        }
    }
}
