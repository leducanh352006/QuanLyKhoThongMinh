using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.DieuChuyen.Models
{
    /// <summary>
    /// Bảng TuyenVanChuyen (việc T5-1, Bảng 1.2, Bảng 2.20): tham số chi phí vận chuyển giữa một cặp kho, dùng làm đầu vào cho
    /// gợi ý G4 (điều chuyển tối ưu chi phí) — c(i, j) = DonGiaVC, F(i, j) = PhiChuyen. Khóa chính (MaKhoXuat, MaKhoNhap); một
    /// cặp kho chỉ có một tuyến (chiều ngược lại là tuyến khác, có thể có giá khác).
    /// </summary>
    public class TuyenVanChuyen
    {
        public int MaKhoXuat { get; set; }

        public int MaKhoNhap { get; set; }

        /// <summary>Đơn giá vận chuyển một đơn vị hàng từ kho xuất sang kho nhập — c(i, j).</summary>
        public decimal DonGiaVC { get; set; }

        /// <summary>Phí cố định mỗi chuyến từ kho xuất sang kho nhập — F(i, j).</summary>
        public decimal PhiChuyen { get; set; }

        public Kho? KhoXuat { get; set; }

        public Kho? KhoNhap { get; set; }

        /// <summary>Quy tắc không cần CSDL. Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (MaKhoXuat <= 0) return "Chọn kho xuất.";
            if (MaKhoNhap <= 0) return "Chọn kho nhập.";
            if (MaKhoXuat == MaKhoNhap) return "Kho xuất phải khác kho nhập.";
            if (DonGiaVC < 0) return "Đơn giá vận chuyển không được âm.";
            if (PhiChuyen < 0) return "Phí mỗi chuyến không được âm.";
            return null;
        }
    }

    public sealed class TuyenVanChuyenConfig : IEntityTypeConfiguration<TuyenVanChuyen>
    {
        public void Configure(EntityTypeBuilder<TuyenVanChuyen> b)
        {
            b.ToTable("TuyenVanChuyen", t =>
            {
                t.HasCheckConstraint("CK_TuyenVanChuyen_KhacKho", "[MaKhoXuat] <> [MaKhoNhap]");
                t.HasCheckConstraint("CK_TuyenVanChuyen_KhongAm", "[DonGiaVC] >= 0 AND [PhiChuyen] >= 0");
            });
            b.HasKey(x => new { x.MaKhoXuat, x.MaKhoNhap });
            b.Property(x => x.DonGiaVC).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.PhiChuyen).HasColumnType("decimal(18,2)").IsRequired();
            b.HasOne(x => x.KhoXuat).WithMany().HasForeignKey(x => x.MaKhoXuat).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.KhoNhap).WithMany().HasForeignKey(x => x.MaKhoNhap).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn bảng TuyenVanChuyen của Điều chuyển kho vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<QuanLyKho.Web.Areas.DieuChuyen.Models.TuyenVanChuyen> TuyenVanChuyen => Set<QuanLyKho.Web.Areas.DieuChuyen.Models.TuyenVanChuyen>();
    }
}
