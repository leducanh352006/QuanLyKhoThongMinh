namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>Giá trị cột BienDongTon.LoaiBienDong (Bảng 2.16), khớp Contracts.LoaiBienDong qua GiaTriCsdl.ToCsdl/ParseLoaiBienDong.</summary>
public static class LoaiBienDongGiaTri
{
    public const string NhanHang = "Nhận hàng";
    public const string SapXep = "Sắp xếp";
    public const string XuatKho = "Xuất kho";
    public const string ChuyenDi = "Chuyển đi";
    public const string NhanChuyen = "Nhận chuyển";
    public const string DoiO = "Đổi ô";
    public const string HoanTac = "Hoàn tác";

    public static readonly IReadOnlyList<string> TatCa = new[] { NhanHang, SapXep, XuatKho, ChuyenDi, NhanChuyen, DoiO, HoanTac };
}

/// <summary>
/// Bảng BienDongTon (Bảng 2.21, việc T4-4): sổ cái mọi biến động tồn, chỉ thêm, không sửa, không xóa.
/// SoLuong dương là tăng, âm là giảm; không bao giờ bằng 0. LoaiCT, MaCT là chứng từ gây biến động, không có khóa ngoại
/// (chứng từ do nhóm khác quản lý, giống quy ước ThamChieuChungTu ở Contracts).
/// </summary>
public class BienDongTon
{
    public long MaBD { get; set; }

    public int MaLo { get; set; }

    public int MaViTri { get; set; }

    public int SoLuong { get; set; }

    public string LoaiBienDong { get; set; } = "";

    /// <summary>NK, XK, CK hoặc KK (Bảng 2.16).</summary>
    public string LoaiCT { get; set; } = "";

    public int MaCT { get; set; }

    public int MaND { get; set; }

    public DateTime ThoiGianUtc { get; set; }
}
