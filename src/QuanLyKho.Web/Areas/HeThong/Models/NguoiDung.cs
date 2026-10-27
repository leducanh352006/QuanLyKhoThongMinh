namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>Giá trị cột NguoiDung.TrangThai (Bảng 2.16).</summary>
public static class TrangThaiNguoiDung
{
    public const string HoatDong = "Hoạt động";
    public const string NgungHoatDong = "Ngừng hoạt động";
}

/// <summary>Bảng NguoiDung (Bảng 2.19). Mật khẩu chỉ lưu dạng băm trong <see cref="MatKhauHash"/>, không bao giờ lưu mật khẩu gốc.</summary>
public class NguoiDung
{
    public int MaND { get; set; }

    public string TenDangNhap { get; set; } = "";

    public string MatKhauHash { get; set; } = "";

    public string HoTen { get; set; } = "";

    public string? Email { get; set; }

    public string? SDT { get; set; }

    public int MaVaiTro { get; set; }

    public string TrangThai { get; set; } = TrangThaiNguoiDung.HoatDong;

    public bool DangHoatDong => TrangThai == TrangThaiNguoiDung.HoatDong;
}
