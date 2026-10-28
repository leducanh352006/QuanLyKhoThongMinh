namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Giá trị cột NhaCungCap.TrangThai.</summary>
public static class TrangThaiNhaCungCap
{
    public const string HoatDong = "Hoạt động";
    public const string NgungHoatDong = "Ngừng hoạt động";
}

/// <summary>Bảng NhaCungCap (việc T3-1). Nhà cung cấp ngừng dùng không chọn được ở phiếu nhập.</summary>
public class NhaCungCap
{
    public int MaNCC { get; set; }

    public string TenNCC { get; set; } = "";

    public string? SDT { get; set; }

    public string? DiaChi { get; set; }

    public string TrangThai { get; set; } = TrangThaiNhaCungCap.HoatDong;

    public bool DangHoatDong => TrangThai == TrangThaiNhaCungCap.HoatDong;
}
