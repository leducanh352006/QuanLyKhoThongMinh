using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;

namespace QuanLyKho.Web.Areas.XuatKho.Models
{
    /// <summary>
    /// Phiếu xuất kho (việc T2-1, Bảng 2.23). Phiếu Nháp sửa được, phiếu Đã duyệt trở đi bị khóa.
    /// Loại "Thiếu do kiểm kê" do hệ thống tự sinh qua <see cref="QuanLyKho.Web.Contracts.IXuatKhoService"/> (việc T2-8), không tạo tay được ở đây.
    /// </summary>
    public class PhieuXuat
    {
        public const string Khac = "Khác";
        public const string TraLaiHangMua = "Trả lại hàng mua";
        public const string HuyHang = "Hủy hàng";
        public const string ThieuDoKiemKe = "Thiếu do kiểm kê";

        public const string Nhap = "Nháp";
        public const string DaDuyet = "Đã duyệt";
        public const string DangLayHang = "Đang lấy hàng";
        public const string DaDoiChieu = "Đã đối chiếu";
        public const string HoanTat = "Hoàn tất";
        public const string DaHuy = "Đã hủy";

        public int MaPX { get; set; }

        public string SoPhieu { get; set; } = "";                    // XK000001, hệ thống tự sinh

        public string LoaiPhieu { get; set; } = Khac;

        public DateTime ThoiGian { get; set; }                       // giờ UTC

        public int MaKho { get; set; }

        public string? DoiTuong { get; set; }

        public string? ThamChieu { get; set; }

        /// <summary>Phiếu nhập gốc; bắt buộc khi LoaiPhieu = Trả lại hàng mua (việc T2-7 kiểm không vượt số đã nhập).</summary>
        public int? MaPNGoc { get; set; }

        public int? MaPK { get; set; }                               // phiếu kiểm sinh ra phiếu Thiếu do kiểm kê (T2-8); các loại khác để null

        public int MaNguoiLap { get; set; }

        /// <summary>Quản lý kho duyệt; null cho tới khi phiếu được duyệt (việc T2-3).</summary>
        public int? MaNguoiDuyet { get; set; }

        /// <summary>Bắt buộc khi LoaiPhieu = Hủy hàng.</summary>
        public string? LyDo { get; set; }

        public string? DienGiai { get; set; }

        public string TrangThai { get; set; } = Nhap;

        public Kho? Kho { get; set; }

        public PhieuNhap? PhieuNhapGoc { get; set; }

        public List<ChiTietPhieuXuat> ChiTiet { get; set; } = new();

        public bool DaKhoa => TrangThai != Nhap;

        /// <summary>Quy tắc không cần CSDL (Bảng 2.4 dòng 1). Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (LoaiPhieu is not (Khac or TraLaiHangMua or HuyHang)) return "Loại phiếu chỉ được chọn Khác, Trả lại hàng mua hoặc Hủy hàng.";
            if (MaKho <= 0) return "Chọn kho xuất.";
            if (LoaiPhieu == TraLaiHangMua && MaPNGoc is null) return "Phiếu trả lại hàng mua phải chọn phiếu nhập gốc.";
            if (LoaiPhieu != TraLaiHangMua && MaPNGoc is not null) return "Chỉ phiếu trả lại hàng mua mới chọn phiếu nhập gốc.";
            if (LoaiPhieu == HuyHang && string.IsNullOrWhiteSpace(LyDo)) return "Phiếu hủy hàng phải ghi lý do.";
            if (ChiTiet.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
            if (ChiTiet.Any(d => d.MaBT <= 0)) return "Chọn mặt hàng cho mỗi dòng.";
            if (ChiTiet.Any(d => d.SoLuong <= 0)) return "Số lượng mỗi dòng phải lớn hơn 0.";
            if (DoiTuong?.Length > 150) return "Đối tượng tối đa 150 ký tự.";
            if (ThamChieu?.Length > 100) return "Tham chiếu tối đa 100 ký tự.";
            if (LyDo?.Length > 255) return "Lý do tối đa 255 ký tự.";
            if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
            return null;
        }
    }

    /// <summary>Một dòng hàng của phiếu xuất (Bảng 2.23). Lô và ô lấy hàng ở bảng LayHang (việc T2-8 giữ chỗ khi duyệt; T2-2 thay FIFO
    /// bằng gợi ý G1, T2-4 điền SLThucLay/LyDoLech khi thủ kho quét mã lấy hàng thật).</summary>
    public class ChiTietPhieuXuat
    {
        public int MaCTX { get; set; }
        public int MaPX { get; set; }
        public int MaBT { get; set; }
        public int SoLuong { get; set; }
    }

    /// <summary>
    /// Lô và ô lấy hàng cho từng dòng xuất (Bảng 2.23). Một dòng ChiTietPhieuXuat có thể lấy từ nhiều (lô, ô) nên tách bảng riêng.
    /// Việc T2-8 tạo dòng này khi duyệt (giữ chỗ FIFO tối giản: lô hết hạn sớm trước, không có hạn thì lô nhỏ trước — xấp xỉ "nhập trước");
    /// SLThucLay, LyDoLech để trống tới khi việc T2-4 (lấy hàng quét mã) điền. Hủy phiếu (T2-8) xóa dòng chưa lấy và trả chỗ qua IStockService.TraCho.
    /// </summary>
    public class LayHang
    {
        public int MaLH { get; set; }
        public int MaCTX { get; set; }
        public int MaLo { get; set; }
        public int MaViTri { get; set; }
        public int SLGiu { get; set; }
        public int? SLThucLay { get; set; }
        public string? LyDoLech { get; set; }
    }

    // ---- Cấu hình bảng (EF tự nạp các lớp này) ----

    public class PhieuXuatConfig : IEntityTypeConfiguration<PhieuXuat>
    {
        public void Configure(EntityTypeBuilder<PhieuXuat> b)
        {
            b.ToTable("PhieuXuat", t =>
            {
                t.HasCheckConstraint("CK_PhieuXuat_LoaiPhieu", "[LoaiPhieu] IN (N'Khác', N'Trả lại hàng mua', N'Hủy hàng', N'Thiếu do kiểm kê')");
                t.HasCheckConstraint("CK_PhieuXuat_TrangThai",
                    "[TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đang lấy hàng', N'Đã đối chiếu', N'Hoàn tất', N'Đã hủy')");
                t.HasCheckConstraint("CK_PhieuXuat_TraHangMua",
                    "([LoaiPhieu] <> N'Trả lại hàng mua' OR [MaPNGoc] IS NOT NULL) AND ([LoaiPhieu] = N'Trả lại hàng mua' OR [MaPNGoc] IS NULL)");
                t.HasCheckConstraint("CK_PhieuXuat_HuyHang", "[LoaiPhieu] <> N'Hủy hàng' OR [LyDo] IS NOT NULL");
                t.HasCheckConstraint("CK_PhieuXuat_NguoiDuyet",
                    "[TrangThai] = N'Nháp' OR [TrangThai] = N'Đã hủy' OR [MaNguoiDuyet] IS NOT NULL");
                t.HasCheckConstraint("CK_PhieuXuat_ThieuCoPhieuKiem",
                    "([LoaiPhieu] = N'Thiếu do kiểm kê' AND [MaPK] IS NOT NULL) OR ([LoaiPhieu] <> N'Thiếu do kiểm kê' AND [MaPK] IS NULL)");
            });
            b.HasKey(x => x.MaPX);
            b.Property(x => x.SoPhieu).HasColumnType("varchar(20)").IsRequired();
            b.HasIndex(x => x.SoPhieu).IsUnique();
            b.Property(x => x.LoaiPhieu).HasMaxLength(20).IsRequired();
            b.Property(x => x.ThoiGian).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.DoiTuong).HasMaxLength(150);
            b.Property(x => x.ThamChieu).HasMaxLength(100);
            b.Property(x => x.LyDo).HasMaxLength(255);
            b.Property(x => x.DienGiai).HasMaxLength(500);
            b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue(PhieuXuat.Nhap);
            b.Ignore(x => x.DaKhoa);
            b.HasIndex(x => new { x.MaKho, x.ThoiGian });
            // Duy nhất trong phạm vi loại Thiếu do kiểm kê: chặn ở CSDL việc hai yêu cầu đồng thời cùng tạo phiếu Thiếu cho một phiếu kiểm
            // (kiểm "đã có chưa" ở XuatKhoService chỉ đọc trước khi ghi, không tự chống được tranh chấp). Chưa có khóa ngoại tới PhieuKiem
            // vì bảng đó do nhóm Kiểm tra hàng hóa tạo sau.
            b.HasIndex(x => x.MaPK).IsUnique().HasFilter("[LoaiPhieu] = N'Thiếu do kiểm kê'");
            b.HasOne(x => x.Kho).WithMany().HasForeignKey(x => x.MaKho).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PhieuNhapGoc).WithMany().HasForeignKey(x => x.MaPNGoc).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiLap).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDuyet).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(d => d.MaPX).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ChiTietPhieuXuatConfig : IEntityTypeConfiguration<ChiTietPhieuXuat>
    {
        public void Configure(EntityTypeBuilder<ChiTietPhieuXuat> b)
        {
            b.ToTable("ChiTietPhieuXuat", t => t.HasCheckConstraint("CK_ChiTietPhieuXuat_SoLuong", "[SoLuong] > 0"));
            b.HasKey(x => x.MaCTX);
            b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LayHangConfig : IEntityTypeConfiguration<LayHang>
    {
        public void Configure(EntityTypeBuilder<LayHang> b)
        {
            b.ToTable("LayHang", t =>
            {
                t.HasCheckConstraint("CK_LayHang_SLGiu", "[SLGiu] > 0");
                t.HasCheckConstraint("CK_LayHang_SLThucLay", "[SLThucLay] >= 0 AND [SLThucLay] <= [SLGiu]");
                t.HasCheckConstraint("CK_LayHang_LyDoLech", "[SLThucLay] IS NULL OR [SLThucLay] = [SLGiu] OR [LyDoLech] IS NOT NULL");
            });
            b.HasKey(x => x.MaLH);
            b.Property(x => x.LyDoLech).HasMaxLength(255);
            b.HasIndex(x => new { x.MaCTX, x.MaLo, x.MaViTri }).IsUnique();
            b.HasOne<ChiTietPhieuXuat>().WithMany().HasForeignKey(x => x.MaCTX).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn hai bảng của Xuất kho vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<PhieuXuat> PhieuXuat => Set<PhieuXuat>();
        public DbSet<ChiTietPhieuXuat> ChiTietPhieuXuat => Set<ChiTietPhieuXuat>();
        public DbSet<LayHang> LayHang => Set<LayHang>();
    }
}
