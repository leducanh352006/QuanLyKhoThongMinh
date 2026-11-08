using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Models
{
    public class PhieuKiem
    {
        public const string ToanBo = "Toàn bộ";
        public const string MotPhan = "Một phần";

        public const string DangKiem = "Đang kiểm";
        public const string HoanTat = "Hoàn tất";

        public int MaPK { get; set; }
        public string SoPhieu { get; set; } = "";
        public int MaKho { get; set; }
        public string LoaiKiemKe { get; set; } = ToanBo;
        public string? ThamChieu { get; set; }
        public string? DienGiai { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public int MaNguoiLap { get; set; }
        public string TrangThai { get; set; } = DangKiem;

        public Kho? Kho { get; set; }
        public List<ChiTietPhieuKiem> ChiTiet { get; set; } = new();
        public List<ThamGiaKiem> ThamGia { get; set; } = new();
        public List<int> PhamViMaBT { get; set; } = new();

        public string? KiemTra()
        {
            ThamChieu = ThamChieu?.Trim();
            DienGiai = DienGiai?.Trim();

            if (LoaiKiemKe is not (ToanBo or MotPhan)) return "Loại kiểm kê chỉ được chọn Toàn bộ hoặc Một phần.";
            if (MaKho <= 0) return "Chọn kho kiểm kê.";
            if (LoaiKiemKe == MotPhan && (PhamViMaBT == null || PhamViMaBT.Count == 0)) return "Kiểm kê một phần phải chọn phạm vi mặt hàng.";
            if (LoaiKiemKe == ToanBo && PhamViMaBT != null && PhamViMaBT.Count > 0) return "Kiểm kê toàn bộ không chọn phạm vi mặt hàng.";
            if (ThamGia == null || ThamGia.Count == 0) return "Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.";
            if (ThamGia.Any(t => t.MaND <= 0)) return "Chọn người cho mỗi dòng tham gia kiểm.";
            if (ThamGia.Select(t => t.MaND).Distinct().Count() != ThamGia.Count) return "Một người không khai báo tham gia hai lần trên cùng phiếu.";
            if (ThamChieu?.Length > 100) return "Tham chiếu tối đa 100 ký tự.";
            if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
            return null;
        }
    }

    public class ChiTietPhieuKiem
    {
        public int MaCTK { get; set; }
        public int MaPK { get; set; }
        public int MaBT { get; set; }
        public int MaLo { get; set; }
        public int MaViTri { get; set; }
        public int SoLuongHeThong { get; set; }
    }

    public class ThamGiaKiem
    {
        public int MaPK { get; set; }
        public int MaND { get; set; }
        public string? GhiChu { get; set; }
        public NguoiDung? NguoiDung { get; set; }
    }

    public class PhieuKiemConfig : IEntityTypeConfiguration<PhieuKiem>
    {
        public void Configure(EntityTypeBuilder<PhieuKiem> b)
        {
            b.ToTable("PhieuKiem", t =>
            {
                t.HasCheckConstraint("CK_PhieuKiem_LoaiKiemKe", "[LoaiKiemKe] IN (N'Toàn bộ', N'Một phần')");
                t.HasCheckConstraint("CK_PhieuKiem_TrangThai", "[TrangThai] IN (N'Đang kiểm', N'Hoàn tất')");
            });
            b.HasKey(x => x.MaPK);
            b.Property(x => x.SoPhieu).HasColumnType("varchar(20)").IsRequired();
            b.HasIndex(x => x.SoPhieu).IsUnique();
            b.Property(x => x.LoaiKiemKe).HasMaxLength(20).IsRequired();
            b.Property(x => x.ThamChieu).HasMaxLength(100);
            b.Property(x => x.DienGiai).HasMaxLength(500);
            b.Property(x => x.ThoiGianBatDau).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(PhieuKiem.DangKiem);
            b.Ignore(x => x.PhamViMaBT);
            b.HasIndex(x => new { x.MaKho, x.ThoiGianBatDau });
            b.HasOne(x => x.Kho).WithMany().HasForeignKey(x => x.MaKho).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiLap).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(d => d.MaPK).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ThamGia).WithOne().HasForeignKey(d => d.MaPK).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ChiTietPhieuKiemConfig : IEntityTypeConfiguration<ChiTietPhieuKiem>
    {
        public void Configure(EntityTypeBuilder<ChiTietPhieuKiem> b)
        {
            b.ToTable("ChiTietPhieuKiem", t => t.HasCheckConstraint("CK_ChiTietPhieuKiem_SoLuongHeThong", "[SoLuongHeThong] >= 0"));
            b.HasKey(x => x.MaCTK);
            b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ThamGiaKiemConfig : IEntityTypeConfiguration<ThamGiaKiem>
    {
        public void Configure(EntityTypeBuilder<ThamGiaKiem> b)
        {
            b.ToTable("ThamGiaKiem");
            b.HasKey(x => new { x.MaPK, x.MaND });
            b.Property(x => x.GhiChu).HasMaxLength(255);
            b.HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    public partial class KhoDbContext
    {
        public DbSet<Areas.HangHoa.Models.PhieuKiem> PhieuKiem => Set<Areas.HangHoa.Models.PhieuKiem>();
        public DbSet<Areas.HangHoa.Models.ChiTietPhieuKiem> ChiTietPhieuKiem => Set<Areas.HangHoa.Models.ChiTietPhieuKiem>();
        public DbSet<Areas.HangHoa.Models.ThamGiaKiem> ThamGiaKiem => Set<Areas.HangHoa.Models.ThamGiaKiem>();
    }
}
