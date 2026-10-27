using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Dùng cho ô chọn người quản lý trên form (record có Property thật, an toàn cho SelectList dựa trên phản chiếu tên trường).</summary>
public sealed record NguoiQuanLyDto(int MaND, string HoTen);

/// <summary>Một dòng kho để hiển thị, kèm tên người quản lý (không mang theo bản băm mật khẩu).</summary>
public sealed record KhoDong(int MaKho, string TenKho, string LoaiKho, string? DiaChi, int MaQuanLy, string TenQuanLy, int ThoiGianCho, string TrangThai)
{
    public bool DangHoatDong => TrangThai == TrangThaiHoatDong.HoatDong;
}

public sealed record KhoMoi(string TenKho, string LoaiKho, string? DiaChi, int MaQuanLy, int ThoiGianCho);

public sealed record KhoSuaDoi(string TenKho, string LoaiKho, string? DiaChi, int MaQuanLy, int ThoiGianCho);
