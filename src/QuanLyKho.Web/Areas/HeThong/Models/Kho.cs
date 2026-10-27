namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>Giá trị cột Kho.LoaiKho (Bảng 2.16).</summary>
public static class LoaiKhoGiaTri
{
    public const string KhoBanHang = "Kho bán hàng";
    public const string KhoLuuTru = "Kho lưu trữ";

    public static readonly IReadOnlyList<string> TatCa = new[] { KhoBanHang, KhoLuuTru };
}

/// <summary>Bảng Kho (Bảng 2.20).</summary>
public class Kho
{
    public int MaKho { get; set; }

    public string TenKho { get; set; } = "";

    public string LoaiKho { get; set; } = "";

    public string? DiaChi { get; set; }

    /// <summary>Quản lý kho (NguoiDung.MaND); bắt buộc.</summary>
    public int MaQuanLy { get; set; }

    /// <summary>Thời gian chờ bổ sung hàng L, tính theo ngày.</summary>
    public int ThoiGianCho { get; set; }

    public string TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    public bool DangHoatDong => TrangThai == TrangThaiHoatDong.HoatDong;
}
