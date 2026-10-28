using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

public sealed record NhaCungCapDong(int MaNCC, string TenNCC, string? SDT, string? DiaChi, string TrangThai)
{
    public bool DangHoatDong => TrangThai == TrangThaiNhaCungCap.HoatDong;
}

public sealed record NhaCungCapMoi(string TenNCC, string? SDT, string? DiaChi);

public sealed record NhaCungCapSuaDoi(string TenNCC, string? SDT, string? DiaChi);

public sealed record DanhMucDong(int MaDM, string TenDM);

public sealed record DanhMucMoi(string TenDM);

public sealed record DanhMucSuaDoi(string TenDM);
