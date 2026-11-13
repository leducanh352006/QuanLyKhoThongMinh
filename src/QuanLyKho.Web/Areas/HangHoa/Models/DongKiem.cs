using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>
/// Dòng kiểm đếm theo ô và lô (việc T3-5, Bảng A.7). Dòng được sinh lúc tạo phiếu kiểm với <see cref="SLHeThong"/> là số hệ thống
/// chụp sau khi khóa ô; thủ kho quét mã rồi nhập <see cref="SLThucTe"/>. Hàng đếm thấy mà sổ không có (ngoài sổ) được thêm thành
/// dòng mới với SLHeThong = 0. Chênh lệch = SLThucTe - SLHeThong, không lưu thành cột.
/// Phân loại phát sinh chi tiết, nguyên nhân, ô đúng, mã đúng (T3-6) và duyệt (T3-7) dùng các cột còn lại, chưa xử lý ở việc này.
/// </summary>
public class DongKiem
{
    // Miền giá trị TrangThaiXuLy (Bảng 2.9, Bảng A.7).
    public const string ChuaKiem = "Chưa kiểm";
    public const string Khop = "Khớp";
    public const string ChoDuyet = "Chờ duyệt";
    public const string DaDuyet = "Đã duyệt";
    public const string TuChoi = "Từ chối";

    // Miền giá trị LoaiPhatSinh (Bảng A.7).
    public const string ThieuHoacThua = "Thiếu hoặc thừa";
    public const string HuHongHoacHetHan = "Hư hỏng hoặc hết hạn";
    public const string SaiViTri = "Sai vị trí";
    public const string SaiMaHang = "Sai mã hàng";

    public int MaDK { get; set; }

    public int MaPK { get; set; }

    /// <summary>Ô được đếm.</summary>
    public int MaViTri { get; set; }

    /// <summary>Lô được đếm.</summary>
    public int MaLo { get; set; }

    /// <summary>Số hệ thống lúc bắt đầu kiểm.</summary>
    public int SLHeThong { get; set; }

    /// <summary>Số đếm thực tế; null khi chưa kiểm.</summary>
    public int? SLThucTe { get; set; }

    public string? LoaiPhatSinh { get; set; }

    public string? NguyenNhan { get; set; }

    /// <summary>Ô đúng, chỉ khi LoaiPhatSinh = Sai vị trí.</summary>
    public int? MaViTriDung { get; set; }

    /// <summary>Biến thể đúng, chỉ khi LoaiPhatSinh = Sai mã hàng.</summary>
    public int? MaBTDung { get; set; }

    /// <summary>Người đếm; null khi chưa đếm.</summary>
    public int? MaNguoiDem { get; set; }

    public string TrangThaiXuLy { get; set; } = ChuaKiem;

    public int? MaNguoiDuyet { get; set; }

    public DateTime? ThoiGianDuyet { get; set; }

    /// <summary>Chênh lệch = tồn thực tế trừ tồn kho (Bảng 2.4 dòng 4); null khi chưa kiểm.</summary>
    public int? ChenhLech => SLThucTe - SLHeThong;

    /// <summary>Dòng đã được quản lý kho duyệt hoặc từ chối thì không đếm lại được.</summary>
    public bool DaChot => TrangThaiXuLy is DaDuyet or TuChoi;

    /// <summary>
    /// Ghi số đếm và tự xếp trạng thái: khớp thì Khớp; lệch thì Chờ duyệt với loại phát sinh mặc định "Thiếu hoặc thừa"
    /// (bước ghi nhận phát sinh T3-6 phân loại lại nếu là hư hỏng, sai vị trí, sai mã hàng). Đếm lại mà vẫn lệch thì giữ phân loại đã ghi.
    /// </summary>
    public void GhiSoDem(int slThucTe, int maNguoiDem)
    {
        SLThucTe = slThucTe;
        MaNguoiDem = maNguoiDem;
        if (slThucTe == SLHeThong)
        {
            TrangThaiXuLy = Khop;
            LoaiPhatSinh = null; NguyenNhan = null; MaViTriDung = null; MaBTDung = null;
        }
        else
        {
            TrangThaiXuLy = ChoDuyet;
            LoaiPhatSinh ??= ThieuHoacThua;
        }
    }

    /// <summary>Bỏ số đếm: dòng về Chưa kiểm.</summary>
    public void BoSoDem()
    {
        SLThucTe = null; MaNguoiDem = null; TrangThaiXuLy = ChuaKiem;
        LoaiPhatSinh = null; NguyenNhan = null; MaViTriDung = null; MaBTDung = null;
    }
}

public class DongKiemConfig : IEntityTypeConfiguration<DongKiem>
{
    public void Configure(EntityTypeBuilder<DongKiem> b)
    {
        b.ToTable("DongKiem", t =>
        {
            t.HasCheckConstraint("CK_DongKiem_SLHeThong", "[SLHeThong] >= 0");
            t.HasCheckConstraint("CK_DongKiem_SLThucTe", "[SLThucTe] IS NULL OR [SLThucTe] >= 0");
            t.HasCheckConstraint("CK_DongKiem_LoaiPhatSinh",
                "[LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] IN (N'Thiếu hoặc thừa', N'Hư hỏng hoặc hết hạn', N'Sai vị trí', N'Sai mã hàng')");
            t.HasCheckConstraint("CK_DongKiem_TrangThaiXuLy",
                "[TrangThaiXuLy] IN (N'Chưa kiểm', N'Khớp', N'Chờ duyệt', N'Đã duyệt', N'Từ chối')");
            // Chưa kiểm khi và chỉ khi chưa có số đếm.
            t.HasCheckConstraint("CK_DongKiem_ChuaKiem",
                "([TrangThaiXuLy] = N'Chưa kiểm' AND [SLThucTe] IS NULL) OR ([TrangThaiXuLy] <> N'Chưa kiểm' AND [SLThucTe] IS NOT NULL)");
            // Chờ duyệt, Đã duyệt, Từ chối bắt buộc có loại phát sinh.
            t.HasCheckConstraint("CK_DongKiem_PhatSinh",
                "[TrangThaiXuLy] NOT IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối') OR [LoaiPhatSinh] IS NOT NULL");
            t.HasCheckConstraint("CK_DongKiem_MaViTriDung",
                "([LoaiPhatSinh] = N'Sai vị trí' AND [MaViTriDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai vị trí') AND [MaViTriDung] IS NULL)");
            t.HasCheckConstraint("CK_DongKiem_MaBTDung",
                "([LoaiPhatSinh] = N'Sai mã hàng' AND [MaBTDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai mã hàng') AND [MaBTDung] IS NULL)");
        });
        b.HasKey(x => x.MaDK);
        b.HasIndex(x => new { x.MaPK, x.MaViTri, x.MaLo }).IsUnique();
        b.Property(x => x.LoaiPhatSinh).HasMaxLength(25);
        b.Property(x => x.NguyenNhan).HasMaxLength(255);
        b.Property(x => x.TrangThaiXuLy).HasMaxLength(20).IsRequired().HasDefaultValue(DongKiem.ChuaKiem);
        b.Property(x => x.ThoiGianDuyet).HasColumnType("datetime2(0)");
        b.Ignore(x => x.ChenhLech);
        b.Ignore(x => x.DaChot);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriDung).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBTDung).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDem).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDuyet).OnDelete(DeleteBehavior.Restrict);
    }
}
