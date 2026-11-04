using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Models
{
    /// <summary>
    /// Phiếu kiểm kê (việc T3-4, Bảng 2.25). Tạo xong là bắt đầu kiểm ngay (ThoiGianBatDau = lúc tạo): ô liên quan bị khóa qua
    /// ILockService và tồn hệ thống được chụp vào ChiTietPhieuKiem.SoLuongHeThong tại đúng thời điểm đó.
    /// Nhập kết quả đếm, đối chiếu lệch và hoàn tất phiếu (sinh phiếu Thừa/Thiếu do kiểm kê qua INhapKhoService/IXuatKhoService)
    /// là việc của bước sau, KHÔNG làm ở đây — phiếu chỉ ở trạng thái Đang kiểm khi việc này xong.
    /// </summary>
    public class PhieuKiem
    {
        public const string ToanBo = "Toàn bộ";
        public const string MotPhan = "Một phần";

        public const string DangKiem = "Đang kiểm";
        public const string HoanTat = "Hoàn tất";

        public int MaPK { get; set; }

        public string SoPhieu { get; set; } = "";                    // KK000001, hệ thống tự sinh

        public int MaKho { get; set; }

        public string LoaiKiemKe { get; set; } = ToanBo;

        public string? ThamChieu { get; set; }

        public string? DienGiai { get; set; }

        public DateTime ThoiGianBatDau { get; set; }                  // giờ UTC, = lúc tạo phiếu (xem ghi chú lớp)

        public int MaNguoiLap { get; set; }

        public string TrangThai { get; set; } = DangKiem;

        public Kho? Kho { get; set; }

        public List<ChiTietPhieuKiem> ChiTiet { get; set; } = new();

        public List<ThamGiaKiem> ThamGia { get; set; } = new();

        /// <summary>Phạm vi mặt hàng khi LoaiKiemKe = Một phần (chọn trên form); không lưu cột riêng, chỉ dùng để tính ChiTiet lúc tạo.</summary>
        public List<int> PhamViMaBT { get; set; } = new();

        /// <summary>Quy tắc không cần CSDL (Bảng 2.17). Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (LoaiKiemKe is not (ToanBo or MotPhan)) return "Loại kiểm kê chỉ được chọn Toàn bộ hoặc Một phần.";
            if (MaKho <= 0) return "Chọn kho kiểm kê.";
            if (LoaiKiemKe == MotPhan && PhamViMaBT.Count == 0) return "Kiểm kê một phần phải chọn phạm vi mặt hàng.";
            if (LoaiKiemKe == ToanBo && PhamViMaBT.Count > 0) return "Kiểm kê toàn bộ không chọn phạm vi mặt hàng.";
            if (ThamGia.Count == 0) return "Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.";
            if (ThamGia.Any(t => t.MaND <= 0)) return "Chọn người cho mỗi dòng tham gia kiểm.";
            if (ThamGia.Select(t => t.MaND).Distinct().Count() != ThamGia.Count) return "Một người không khai báo tham gia hai lần trên cùng phiếu.";
            if (ThamChieu?.Length > 100) return "Tham chiếu tối đa 100 ký tự.";
            if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
            return null;
        }
    }

    /// <summary>
    /// Một dòng phạm vi kiểm (lô tại một ô cụ thể) kèm tồn hệ thống chụp lúc bắt đầu (Bảng 2.25).
    /// SoLuongThucTe, người đếm từng dòng và lý do lệch là việc của bước nhập kết quả kiểm (bước sau), chưa có ở đây.
    /// </summary>
    public class ChiTietPhieuKiem
    {
        public int MaCTK { get; set; }
        public int MaPK { get; set; }
        public int MaBT { get; set; }
        public int MaLo { get; set; }
        public int MaViTri { get; set; }
        public int SoLuongHeThong { get; set; }
    }

    /// <summary>Người tham gia kiểm của một phiếu (Bảng 2.25); khóa chính kép (MaPK, MaND).</summary>
    public class ThamGiaKiem
    {
        public int MaPK { get; set; }
        public int MaND { get; set; }
        public string? GhiChu { get; set; }

        public NguoiDung? NguoiDung { get; set; }
    }

    // ---- Cấu hình bảng (EF tự nạp các lớp này) ----

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
    /// <summary>Gắn ba bảng kiểm kê vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<Areas.HangHoa.Models.PhieuKiem> PhieuKiem => Set<Areas.HangHoa.Models.PhieuKiem>();
        public DbSet<Areas.HangHoa.Models.ChiTietPhieuKiem> ChiTietPhieuKiem => Set<Areas.HangHoa.Models.ChiTietPhieuKiem>();
        public DbSet<Areas.HangHoa.Models.ThamGiaKiem> ThamGiaKiem => Set<Areas.HangHoa.Models.ThamGiaKiem>();
    }
}
