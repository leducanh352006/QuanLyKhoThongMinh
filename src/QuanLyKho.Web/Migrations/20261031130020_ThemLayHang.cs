using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class ThemLayHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LayHang",
                columns: table => new
                {
                    MaLH = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCTX = table.Column<int>(type: "int", nullable: false),
                    MaLo = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<int>(type: "int", nullable: false),
                    SLGiu = table.Column<int>(type: "int", nullable: false),
                    SLThucLay = table.Column<int>(type: "int", nullable: true),
                    LyDoLech = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayHang", x => x.MaLH);
                    table.CheckConstraint("CK_LayHang_LyDoLech", "[SLThucLay] IS NULL OR [SLThucLay] = [SLGiu] OR [LyDoLech] IS NOT NULL");
                    table.CheckConstraint("CK_LayHang_SLGiu", "[SLGiu] > 0");
                    table.CheckConstraint("CK_LayHang_SLThucLay", "[SLThucLay] >= 0 AND [SLThucLay] <= [SLGiu]");
                    table.ForeignKey(
                        name: "FK_LayHang_ChiTietPhieuXuat_MaCTX",
                        column: x => x.MaCTX,
                        principalTable: "ChiTietPhieuXuat",
                        principalColumn: "MaCTX",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LayHang_LoHang_MaLo",
                        column: x => x.MaLo,
                        principalTable: "LoHang",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LayHang_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LayHang_MaCTX_MaLo_MaViTri",
                table: "LayHang",
                columns: new[] { "MaCTX", "MaLo", "MaViTri" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LayHang_MaLo",
                table: "LayHang",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_LayHang_MaViTri",
                table: "LayHang",
                column: "MaViTri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LayHang");
        }
    }
}
