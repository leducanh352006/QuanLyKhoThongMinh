using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;

namespace QuanLyKho.Web.Areas.NhapKho.Models
{
    /// <summary>
    /// Lệnh sắp xếp hàng đạt từ khu tiếp nhận vào một ô lưu trữ (việc T1-4, Bảng 2.22). Một dòng hàng có thể có nhiều lệnh (chia vào nhiều ô).
    /// Lệnh Chờ xác nhận đã chiếm chỗ trong ô (tính vào Vđ, Kđ của sức chứa) nhưng hàng chưa vào TonKho; thủ kho quét mã ô và mã lô thì mới Đã xác nhận.
    /// </summary>
    public class SapXep
    {
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DaHuy = "Đã hủy";

        public int MaSX { get; set; }

        public int MaCTN { get; set; }                  // dòng phiếu nhập được xếp

        public int MaViTri { get; set; }                // ô nhận hàng (ô Lưu trữ cùng kho với phiếu)

        public int SoLuong { get; set; }

        public string TrangThai { get; set; } = ChoXacNhan;

        public int? MaNguoiXN { get; set; }             // thủ kho quét xác nhận; null đến khi xác nhận

        public DateTime? ThoiGianXN { get; set; }       // giờ UTC
    }

    public class SapXepConfig : IEntityTypeConfiguration<SapXep>
    {
        public void Configure(EntityTypeBuilder<SapXep> b)
        {
            b.ToTable("SapXep", t =>
            {
                t.HasCheckConstraint("CK_SapXep_SoLuong", "[SoLuong] > 0");
                t.HasCheckConstraint("CK_SapXep_TrangThai", "[TrangThai] IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã hủy')");
                t.HasCheckConstraint("CK_SapXep_XacNhan", "([TrangThai] = N'Đã xác nhận' AND [MaNguoiXN] IS NOT NULL AND [ThoiGianXN] IS NOT NULL) OR ([TrangThai] <> N'Đã xác nhận' AND [MaNguoiXN] IS NULL AND [ThoiGianXN] IS NULL)");
            });
            b.HasKey(x => x.MaSX);
            b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(SapXep.ChoXacNhan);
            b.Property(x => x.ThoiGianXN).HasColumnType("datetime2(0)");
            b.HasIndex(x => new { x.MaViTri, x.TrangThai });   // tính hàng chờ xếp của một ô khi kiểm tra sức chứa
            b.HasOne<ChiTietPhieuNhap>().WithMany().HasForeignKey(x => x.MaCTN).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiXN).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn bảng SapXep vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<SapXep> SapXep => Set<SapXep>();
    }
}
