using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.DieuChuyen.Models
{
    /// <summary>
    /// Phiếu chuyển hàng giữa hai kho (việc T5-2 đến T5-6, báo cáo mục 2.1.5, Hình 2.6, Bảng 1.5 phần Điều chuyển).
    /// Vòng đời: Nháp (sửa được) → Đã duyệt (giữ chỗ ở kho đi) → Đang vận chuyển (đã trừ tồn kho đi, không thuộc tồn của kho nào)
    /// → Nhận một phần (đã đếm hết nhưng có dòng nhận thiếu, chờ quản lý kho xử lý chênh lệch) → Đã nhận. Hủy được khi còn Nháp
    /// hoặc Đã duyệt. Kho đi, kho đến suy ra từ tuyến; người vận chuyển, phương tiện không bắt buộc.
    /// </summary>
    public class PhieuChuyen
    {
        public const string Nhap = "Nháp";
        public const string DaDuyet = "Đã duyệt";
        public const string DangVanChuyen = "Đang vận chuyển";
        public const string NhanMotPhan = "Nhận một phần";
        public const string DaNhan = "Đã nhận";
        public const string DaHuy = "Đã hủy";

        public static readonly IReadOnlyList<string> TatCaTrangThai = new[] { Nhap, DaDuyet, DangVanChuyen, NhanMotPhan, DaNhan, DaHuy };

        public int MaPC { get; set; }

        public string SoPhieu { get; set; } = "";                    // CK000001, hệ thống tự sinh

        public DateTime ThoiGian { get; set; }                       // giờ UTC

        public int MaKhoXuat { get; set; }

        public int MaKhoNhap { get; set; }

        /// <summary>Người vận chuyển (tài xế, nhân viên giao hàng...), không bắt buộc.</summary>
        public string? NguoiVanChuyen { get; set; }

        /// <summary>Phương tiện vận chuyển (ví dụ xe tải 29C-123.45), không bắt buộc.</summary>
        public string? PhuongTien { get; set; }

        public int MaNguoiLap { get; set; }

        /// <summary>Quản lý kho duyệt; null cho tới khi phiếu được duyệt.</summary>
        public int? MaNguoiDuyet { get; set; }

        public string? DienGiai { get; set; }

        public string TrangThai { get; set; } = Nhap;

        public Kho? KhoXuat { get; set; }

        public Kho? KhoNhap { get; set; }

        public List<ChiTietChuyen> ChiTiet { get; set; } = new();

        public bool DaKhoa => TrangThai != Nhap;

        /// <summary>Hủy được khi hàng chưa rời kho xuất.</summary>
        public bool HuyDuoc => TrangThai is Nhap or DaDuyet;

        /// <summary>Quy tắc không cần CSDL. Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (MaKhoXuat <= 0 || MaKhoNhap <= 0) return "Chọn tuyến vận chuyển (kho đi → kho đến).";
            if (MaKhoXuat == MaKhoNhap) return "Kho đi phải khác kho đến.";
            if (NguoiVanChuyen?.Length > 100) return "Người vận chuyển tối đa 100 ký tự.";
            if (PhuongTien?.Length > 50) return "Phương tiện tối đa 50 ký tự.";
            if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
            if (ChiTiet.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
            for (var i = 0; i < ChiTiet.Count; i++)
            {
                var d = ChiTiet[i];
                if (d.MaLo <= 0 || d.MaViTriXuat <= 0) return $"Dòng {i + 1}: chọn hàng (lô, ô) ở kho đi.";
                if (d.MaViTriNhap <= 0) return $"Dòng {i + 1}: chọn ô xếp ở kho đến.";
                if (d.SoLuong <= 0) return $"Dòng {i + 1}: số lượng phải lớn hơn 0.";
            }
            // Mỗi (lô, ô xuất) và mỗi (lô, ô nhận) chỉ một dòng: sổ cái coi hai lần ghi cùng lô, ô, số lượng trong một phiếu là gọi lặp.
            if (ChiTiet.GroupBy(d => (d.MaLo, d.MaViTriXuat)).Any(g => g.Count() > 1))
                return "Có hai dòng cùng lô và cùng ô xuất; gộp lại thành một dòng.";
            if (ChiTiet.GroupBy(d => (d.MaLo, d.MaViTriNhap)).Any(g => g.Count() > 1))
                return "Có hai dòng cùng lô và cùng ô nhận; gộp lại thành một dòng.";
            return null;
        }
    }

    /// <summary>
    /// Một dòng chuyển: lô (cho biết biến thể), ô lấy ở kho đi, ô xếp ở kho đến, số lượng gửi. SLNhan, LyDoLech điền khi thủ kho
    /// kho đến quét nhận (việc T5-5); nhận thiếu phải ghi lý do lệch. Phần thiếu do quản lý kho xử lý: nhập tại kho đến hoặc trả về
    /// kho đi, ghi nguyên nhân (XuLyLech, NguyenNhanLech); hệ thống ghi sổ cái, không sinh phiếu nhập (Bảng 1.5).
    /// </summary>
    public class ChiTietChuyen
    {
        public int MaCTC { get; set; }
        public int MaPC { get; set; }
        public int MaLo { get; set; }
        public int MaViTriXuat { get; set; }
        public int MaViTriNhap { get; set; }
        public int SoLuong { get; set; }
        public int? SLNhan { get; set; }
        public string? LyDoLech { get; set; }
        public string? XuLyLech { get; set; }
        public string? NguyenNhanLech { get; set; }

        public const string NhapTaiKhoDen = "Nhập tại kho đến";
        public const string TraVeKhoDi = "Trả về kho đi";

        public int SoThieu => SLNhan is int n ? SoLuong - n : 0;
        public bool ChoXuLyLech => SoThieu > 0 && XuLyLech is null;
    }

    // ---- Cấu hình bảng (EF tự nạp các lớp này) ----

    public class PhieuChuyenConfig : IEntityTypeConfiguration<PhieuChuyen>
    {
        public void Configure(EntityTypeBuilder<PhieuChuyen> b)
        {
            b.ToTable("PhieuChuyen", t =>
            {
                t.HasCheckConstraint("CK_PhieuChuyen_TrangThai", "[TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đang vận chuyển', N'Nhận một phần', N'Đã nhận', N'Đã hủy')");
                t.HasCheckConstraint("CK_PhieuChuyen_KhacKho", "[MaKhoXuat] <> [MaKhoNhap]");
                t.HasCheckConstraint("CK_PhieuChuyen_NguoiDuyet", "[TrangThai] IN (N'Nháp', N'Đã hủy') OR [MaNguoiDuyet] IS NOT NULL");
            });
            b.HasKey(x => x.MaPC);
            b.Property(x => x.SoPhieu).HasColumnType("varchar(20)").IsRequired();
            b.HasIndex(x => x.SoPhieu).IsUnique();
            b.Property(x => x.ThoiGian).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.DienGiai).HasMaxLength(500);
            b.Property(x => x.NguoiVanChuyen).HasMaxLength(100);
            b.Property(x => x.PhuongTien).HasMaxLength(50);
            b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(PhieuChuyen.Nhap);
            b.Ignore(x => x.DaKhoa);
            b.Ignore(x => x.HuyDuoc);
            b.HasIndex(x => new { x.MaKhoXuat, x.ThoiGian });
            b.HasOne(x => x.KhoXuat).WithMany().HasForeignKey(x => x.MaKhoXuat).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.KhoNhap).WithMany().HasForeignKey(x => x.MaKhoNhap).OnDelete(DeleteBehavior.Restrict);
            // Phiếu chỉ lập trên tuyến đã khai báo (báo cáo mục 2.1.5).
            b.HasOne<TuyenVanChuyen>().WithMany().HasForeignKey(x => new { x.MaKhoXuat, x.MaKhoNhap }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiLap).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDuyet).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(d => d.MaPC).OnDelete(DeleteBehavior.Restrict);
            // Cột GoiYAI.MaPC đã chừa sẵn cho phiếu chuyển; nay có bảng thật thì thêm khóa ngoại.
            b.HasMany<GoiYAI>().WithOne().HasForeignKey(g => g.MaPC).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ChiTietChuyenConfig : IEntityTypeConfiguration<ChiTietChuyen>
    {
        public void Configure(EntityTypeBuilder<ChiTietChuyen> b)
        {
            b.ToTable("ChiTietChuyen", t =>
            {
                t.HasCheckConstraint("CK_ChiTietChuyen_SoLuong", "[SoLuong] > 0");
                t.HasCheckConstraint("CK_ChiTietChuyen_KhacO", "[MaViTriXuat] <> [MaViTriNhap]");
                t.HasCheckConstraint("CK_ChiTietChuyen_SLNhan", "[SLNhan] >= 0 AND [SLNhan] <= [SoLuong]");
                t.HasCheckConstraint("CK_ChiTietChuyen_LyDoLech", "[SLNhan] IS NULL OR [SLNhan] = [SoLuong] OR [LyDoLech] IS NOT NULL");
                t.HasCheckConstraint("CK_ChiTietChuyen_XuLyLech",
                    "[XuLyLech] IS NULL OR ([XuLyLech] IN (N'Nhập tại kho đến', N'Trả về kho đi') AND [SLNhan] < [SoLuong] AND [NguyenNhanLech] IS NOT NULL)");
            });
            b.HasKey(x => x.MaCTC);
            b.Property(x => x.LyDoLech).HasMaxLength(255);
            b.Property(x => x.XuLyLech).HasMaxLength(20);
            b.Property(x => x.NguyenNhanLech).HasMaxLength(255);
            b.Ignore(x => x.SoThieu);
            b.Ignore(x => x.ChoXuLyLech);
            b.HasIndex(x => new { x.MaPC, x.MaLo, x.MaViTriXuat }).IsUnique();
            b.HasIndex(x => new { x.MaPC, x.MaLo, x.MaViTriNhap }).IsUnique();
            b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriXuat).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriNhap).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn hai bảng phiếu chuyển vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<PhieuChuyen> PhieuChuyen => Set<PhieuChuyen>();
        public DbSet<ChiTietChuyen> ChiTietChuyen => Set<ChiTietChuyen>();
    }
}
