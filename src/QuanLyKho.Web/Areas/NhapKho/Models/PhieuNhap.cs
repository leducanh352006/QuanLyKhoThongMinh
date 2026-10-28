using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;

namespace QuanLyKho.Web.Areas.NhapKho.Models
{
    /// <summary>Phiếu nhập kho (việc T1-1, Bảng 2.22). Phiếu Nháp sửa được, phiếu Đã xác nhận bị khóa.</summary>
    public class PhieuNhap
    {
        public const string Nhap = "Nháp";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DaKiemTra = "Đã kiểm tra";

        public int MaPN { get; set; }

        public string SoPhieu { get; set; } = "";                    // NK000001, hệ thống tự sinh

        public string LoaiPhieu { get; set; } = "Mua hàng";         // Mua hàng hoặc Khác

        public DateTime ThoiGian { get; set; }                       // giờ UTC

        public int MaKho { get; set; }

        public int? MaNCC { get; set; }                              // null chỉ với phiếu nháp từ gợi ý G3 (làm sau)

        public int MaNguoiLap { get; set; }

        public string? DienGiai { get; set; }

        public string TrangThai { get; set; } = Nhap;

        public Kho? Kho { get; set; }

        public NhaCungCap? NhaCungCap { get; set; }

        public List<ChiTietPhieuNhap> ChiTiet { get; set; } = new();

        public bool DaKhoa => TrangThai != Nhap;

        /// <summary>
        /// Mã lô in trên tem của dòng thứ <paramref name="stt"/> (đếm từ 1) của phiếu: LO-NK000001-01. Số phiếu duy nhất nên mã lô duy nhất toàn hệ thống,
        /// khác kiểu mã tự sinh của HangHoa (LO-yyMM-nnn) nên không trùng. Việc T1-2 tạo lô nên truyền đúng mã này vào IHangHoaService.TaoLo.
        /// </summary>
        public static string MaLo(string soPhieu, int stt) => $"LO-{soPhieu}-{stt:D2}";

        /// <summary>Quy tắc không cần CSDL (Bảng 2.3 dòng 1). Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (LoaiPhieu is not ("Mua hàng" or "Khác")) return "Loại phiếu chỉ được chọn Mua hàng hoặc Khác.";
            if (MaKho <= 0) return "Chọn kho nhập.";
            if (MaNCC is null) return "Chọn nhà cung cấp.";
            if (ChiTiet.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
            if (ChiTiet.Any(d => d.MaBT <= 0)) return "Chọn mặt hàng cho mỗi dòng.";
            if (ChiTiet.Any(d => d.SLDuKien <= 0)) return "Số lượng mỗi dòng phải lớn hơn 0.";
            if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
            return null;
        }
    }

    /// <summary>Một dòng hàng của phiếu nhập (Bảng 2.22). Bốn cột sau cùng do việc T1-2 (kiểm tra hàng nhập) điền.</summary>
    public class ChiTietPhieuNhap
    {
        public int MaCTN { get; set; }
        public int MaPN { get; set; }
        public int MaBT { get; set; }
        public int SLDuKien { get; set; }

        public int? MaLo { get; set; }          // null đến khi tiếp nhận

        public int? SLThucNhan { get; set; }    // null đến khi kiểm; SLThucNhan trừ SLDat là số không đạt

        public int? SLDat { get; set; }

        public string? LyDoLech { get; set; }

        /// <summary>Quy tắc kiểm tra một dòng khi tiếp nhận (Bảng 2.3 dòng 2), không cần CSDL. Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTraKetQua()
        {
            if (SLThucNhan is null || SLThucNhan < 0) return "Nhập số thực nhận (từ 0 trở lên).";
            if (SLDat is null || SLDat < 0 || SLDat > SLThucNhan) return "Số đạt phải từ 0 đến số thực nhận.";
            if (SLThucNhan != SLDuKien && string.IsNullOrWhiteSpace(LyDoLech)) return "Số thực nhận khác phiếu phải ghi lý do.";
            if (LyDoLech?.Length > 255) return "Lý do tối đa 255 ký tự.";
            return null;
        }
    }

    // ---- Cấu hình bảng (EF tự nạp các lớp này) ----

    public class PhieuNhapConfig : IEntityTypeConfiguration<PhieuNhap>
    {
        public void Configure(EntityTypeBuilder<PhieuNhap> b)
        {
            b.ToTable("PhieuNhap", t =>
            {
                t.HasCheckConstraint("CK_PhieuNhap_LoaiPhieu", "[LoaiPhieu] IN (N'Mua hàng', N'Khác', N'Thừa do kiểm kê')");
                t.HasCheckConstraint("CK_PhieuNhap_TrangThai", "[TrangThai] IN (N'Nháp', N'Đã xác nhận', N'Đã kiểm tra', N'Hoàn tất', N'Đã hủy')");
            });
            b.HasKey(x => x.MaPN);
            b.Property(x => x.SoPhieu).HasColumnType("varchar(20)").IsRequired();
            b.HasIndex(x => x.SoPhieu).IsUnique();
            b.Property(x => x.LoaiPhieu).HasMaxLength(20).IsRequired();
            b.Property(x => x.ThoiGian).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.DienGiai).HasMaxLength(500);
            b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(PhieuNhap.Nhap);
            b.Ignore(x => x.DaKhoa);
            b.HasIndex(x => new { x.MaKho, x.ThoiGian });
            b.HasOne(x => x.Kho).WithMany().HasForeignKey(x => x.MaKho).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.NhaCungCap).WithMany().HasForeignKey(x => x.MaNCC).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiLap).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(d => d.MaPN).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ChiTietPhieuNhapConfig : IEntityTypeConfiguration<ChiTietPhieuNhap>
    {
        public void Configure(EntityTypeBuilder<ChiTietPhieuNhap> b)
        {
            b.ToTable("ChiTietPhieuNhap", t =>
            {
                t.HasCheckConstraint("CK_ChiTietPhieuNhap_SLDuKien", "[SLDuKien] > 0");
                t.HasCheckConstraint("CK_ChiTietPhieuNhap_SLThucNhan", "[SLThucNhan] >= 0");
                t.HasCheckConstraint("CK_ChiTietPhieuNhap_SLDat", "[SLDat] >= 0 AND [SLDat] <= [SLThucNhan]");
                t.HasCheckConstraint("CK_ChiTietPhieuNhap_LyDoLech", "[SLThucNhan] IS NULL OR [SLThucNhan] = [SLDuKien] OR [LyDoLech] IS NOT NULL");
            });
            b.HasKey(x => x.MaCTN);
            b.Property(x => x.LyDoLech).HasMaxLength(255);
            // Khóa ngoại kép (MaLo, MaBT) → LoHang (Bảng 2.22) tạo bằng SQL trong migration KiemTraHangNhap, vì EF cần khóa thay thế ở LoHang (Area của nhóm khác).
            b.HasIndex(x => x.MaLo);
            b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn hai bảng của Nhập kho vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
        public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap => Set<ChiTietPhieuNhap>();
    }
}
