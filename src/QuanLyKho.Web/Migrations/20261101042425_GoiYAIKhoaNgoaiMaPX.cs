using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class GoiYAIKhoaNgoaiMaPX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_GoiYAI_PhieuXuat_MaPX",
                table: "GoiYAI",
                column: "MaPX",
                principalTable: "PhieuXuat",
                principalColumn: "MaPX",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoiYAI_PhieuXuat_MaPX",
                table: "GoiYAI");
        }
    }
}
