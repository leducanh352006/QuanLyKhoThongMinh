using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKho.Web.Migrations
{
    /// <inheritdoc />
    public partial class NhaCungCapDanhMuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    MaDM = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDM = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.MaDM);
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    MaNCC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNCC = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    SDT = table.Column<string>(type: "varchar(15)", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", nullable: false, defaultValue: "Hoạt động")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCap", x => x.MaNCC);
                    table.CheckConstraint("CK_NhaCungCap_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhMuc_TenDM",
                table: "DanhMuc",
                column: "TenDM",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhaCungCap_TenNCC",
                table: "NhaCungCap",
                column: "TenNCC",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "NhaCungCap");
        }
    }
}
