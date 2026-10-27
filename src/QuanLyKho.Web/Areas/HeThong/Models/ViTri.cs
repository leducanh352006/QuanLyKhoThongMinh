namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>Bảy cấp vị trí theo thứ tự từ cao xuống thấp (Bảng 2.16); cấp con phải thấp hơn cấp cha, được bỏ qua cấp.</summary>
public static class CapViTriGiaTri
{
    public const string Toa = "Tòa";
    public const string Tang = "Tầng";
    public const string Phong = "Phòng";
    public const string Day = "Dãy";
    public const string Hang = "Hàng";
    public const string Ke = "Kệ";
    public const string O = "Ô";

    /// <summary>Thứ tự từ cao xuống thấp; chỉ số càng lớn càng thấp (gần ô).</summary>
    public static readonly IReadOnlyList<string> ThuTu = new[] { Toa, Tang, Phong, Day, Hang, Ke, O };

    public static int ChiSo(string capViTri) => ThuTu.ToList().IndexOf(capViTri);
}

/// <summary>Giá trị cột ViTri.LoaiViTri (Bảng 2.16); chỉ áp dụng cho CapViTri = Ô.</summary>
public static class LoaiViTriGiaTri
{
    public const string LuuTru = "Lưu trữ";
    public const string TiepNhan = "Tiếp nhận";

    public static readonly IReadOnlyList<string> TatCa = new[] { LuuTru, TiepNhan };
}

/// <summary>
/// Bảng ViTri (Bảng 2.20): cây vị trí nhiều cấp trong kho, tự tham chiếu qua MaViTriCha.
/// Chỉ ô (CapViTri = Ô) có LoaiViTri, MaVachO và kích thước; ô Lưu trữ bắt buộc có kích thước và khối lượng tối đa,
/// ô Tiếp nhận không giới hạn (các cột kích thước để trống).
/// </summary>
public class ViTri
{
    public int MaViTri { get; set; }

    public string TenViTri { get; set; } = "";

    public int MaKho { get; set; }

    /// <summary>Vị trí cấp trên; null nếu đây là gốc (có thể là bất kỳ cấp nào, ví dụ khu tiếp nhận là ô gốc).</summary>
    public int? MaViTriCha { get; set; }

    public string CapViTri { get; set; } = "";

    public string? LoaiViTri { get; set; }

    /// <summary>Mã vạch ô, dùng khi quét mã; chỉ với ô.</summary>
    public string? MaVachO { get; set; }

    public decimal? Dai { get; set; }

    public decimal? Rong { get; set; }

    public decimal? Cao { get; set; }

    public decimal? KhoiLuongToiDa { get; set; }

    /// <summary>1 khi ô đang bị khóa để kiểm kê (cột do Nhóm kiểm tra hàng hóa ghi qua ILockService; T4-3 chỉ lưu).</summary>
    public bool DangKhoa { get; set; }

    public string TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    public bool DangHoatDong => TrangThai == TrangThaiHoatDong.HoatDong;

    public bool LaO => CapViTri == CapViTriGiaTri.O;

    public decimal? TheTichCm3 => Dai is decimal d && Rong is decimal r && Cao is decimal c ? d * r * c : null;
}
