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

    /// <summary>Bốn loại phát sinh (Bảng A.7), theo thứ tự hiện trên màn hình ghi nhận.</summary>
    public static readonly IReadOnlyList<string> TatCaLoaiPhatSinh = new[] { ThieuHoacThua, HuHongHoacHetHan, SaiViTri, SaiMaHang };

    /// <summary>Độ dài tối đa của <see cref="NguyenNhan"/> (cột nvarchar(255)).</summary>
    public const int NguyenNhanToiDa = 255;

    /// <summary>Dòng đã đếm mà số đếm khác số hệ thống.</summary>
    public bool LaDongLech => SLThucTe is int s && s != SLHeThong;

    /// <summary>
    /// Dòng lệch đã được phân loại phát sinh đầy đủ (việc T3-6): có loại hợp lệ, có nguyên nhân, và đủ ô đúng (Sai vị trí) hoặc mã hàng đúng
    /// (Sai mã hàng). Loại mặc định "Thiếu hoặc thừa" do bước đếm tự gán chưa có nguyên nhân nên chưa tính là đã phân loại.
    /// Dòng khớp và dòng chưa đếm không cần phân loại nên trả false.
    /// </summary>
    public bool DaPhanLoai => LaDongLech && DuThongTinPhatSinh(LoaiPhatSinh, NguyenNhan, MaViTriDung, MaBTDung);

    /// <summary>Loại hợp lệ + nguyên nhân không rỗng + thông tin đi kèm của loại (ô đúng cho Sai vị trí, mã hàng đúng cho Sai mã hàng).</summary>
    public static bool DuThongTinPhatSinh(string? loai, string? nguyenNhan, int? maViTriDung, int? maBTDung)
        => loai is not null && TatCaLoaiPhatSinh.Contains(loai)
           && !string.IsNullOrWhiteSpace(nguyenNhan)
           && (loai != SaiViTri || maViTriDung is > 0)
           && (loai != SaiMaHang || maBTDung is > 0);

    /// <summary>
    /// Ghi số đếm và tự xếp trạng thái: khớp thì Khớp; lệch thì Chờ duyệt với loại phát sinh mặc định "Thiếu hoặc thừa" nhưng chưa có
    /// nguyên nhân, nên còn phải phân loại ở bước ghi nhận phát sinh (T3-6). Đếm lại ra cùng số thì giữ phân loại đã ghi; ra số khác thì
    /// xóa phân loại, vì chiều lệch và số lượng cần chuyển/kiểm sức chứa đã đổi.
    /// </summary>
    public void GhiSoDem(int slThucTe, int maNguoiDem)
    {
        var doiSo = SLThucTe != slThucTe;
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
            if (doiSo || LoaiPhatSinh is null)
            {
                LoaiPhatSinh = ThieuHoacThua;
                NguyenNhan = null; MaViTriDung = null; MaBTDung = null;
            }
        }
    }

    /// <summary>Ghi phân loại phát sinh của dòng lệch; chỉ giữ ô đúng cho Sai vị trí và mã hàng đúng cho Sai mã hàng (ràng buộc CHECK của A.7).</summary>
    public void PhanLoai(string loaiPhatSinh, string nguyenNhan, int? maViTriDung, int? maBTDung)
    {
        LoaiPhatSinh = loaiPhatSinh;
        NguyenNhan = nguyenNhan;
        MaViTriDung = loaiPhatSinh == SaiViTri ? maViTriDung : null;
        MaBTDung = loaiPhatSinh == SaiMaHang ? maBTDung : null;
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
        b.Ignore(x => x.LaDongLech);
        b.Ignore(x => x.DaPhanLoai);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTriDung).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBTDung).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDem).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDuyet).OnDelete(DeleteBehavior.Restrict);
    }
}
