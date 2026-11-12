using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>
/// Phiếu kiểm kê (việc T3-4, Bảng A.7). Tạo xong là bắt đầu kiểm ngay (ThoiGianBatDau = lúc tạo): các ô thuộc phạm vi bị khóa qua
/// ILockService rồi tồn hệ thống được chụp vào <see cref="ChiTietPhieuKiem.SoLuongHeThong"/> tại đúng thời điểm đó.
/// Vòng đời (Bảng 2.9): Đang kiểm → Chờ duyệt → Đã xử lý. Việc này chỉ tạo phiếu ở trạng thái Đang kiểm; kiểm đếm, ghi nhận và
/// duyệt phát sinh, hoàn tất phiếu là việc của các bước sau (T3-5 đến T3-8).
/// </summary>
public class PhieuKiem
{
    public const string ToanBo = "Toàn bộ";
    public const string MotPhan = "Một phần";

    public const string DangKiem = "Đang kiểm";
    public const string ChoDuyet = "Chờ duyệt";
    public const string DaXuLy = "Đã xử lý";

    public int MaPK { get; set; }

    /// <summary>Số phiếu tiền tố KK (KK000001), hệ thống tự sinh.</summary>
    public string SoPhieu { get; set; } = "";

    public int MaKho { get; set; }

    public string LoaiKiemKe { get; set; } = ToanBo;

    public string? DienGiai { get; set; }

    public string? ThamChieu { get; set; }

    /// <summary>Thời điểm chụp số hệ thống (UTC), bằng lúc tạo phiếu.</summary>
    public DateTime ThoiGianBatDau { get; set; }

    public int MaNguoiLap { get; set; }

    public string TrangThai { get; set; } = DangKiem;

    public Kho? Kho { get; set; }

    public List<ChiTietPhieuKiem> ChiTiet { get; set; } = new();

    public List<ThamGiaKiem> ThamGia { get; set; } = new();
}

/// <summary>
/// Một dòng phạm vi kiểm (một lô tại một ô) kèm tồn hệ thống chụp lúc bắt đầu kiểm.
/// Số đếm thực tế, người đếm và phân loại phát sinh là việc của bước kiểm đếm (T3-5), chưa có ở đây.
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

/// <summary>Người tham gia kiểm của một phiếu (Bảng A.7); khóa chính ghép (MaPK, MaND).</summary>
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
            t.HasCheckConstraint("CK_PhieuKiem_TrangThai", "[TrangThai] IN (N'Đang kiểm', N'Chờ duyệt', N'Đã xử lý')");
        });
        b.HasKey(x => x.MaPK);
        b.Property(x => x.SoPhieu).HasColumnType("varchar(20)").IsRequired();
        b.HasIndex(x => x.SoPhieu).IsUnique();
        b.Property(x => x.LoaiKiemKe).HasMaxLength(10).IsRequired();
        b.Property(x => x.DienGiai).HasMaxLength(500);
        b.Property(x => x.ThamChieu).HasMaxLength(100);
        b.Property(x => x.ThoiGianBatDau).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(PhieuKiem.DangKiem);
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
