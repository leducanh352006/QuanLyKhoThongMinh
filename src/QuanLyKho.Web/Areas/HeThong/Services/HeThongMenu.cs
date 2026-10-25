using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Menu của Area Hệ thống. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class HeThongMenu : IMenuNguon
{
    public int ThuTu => 10;

    public MenuNhom LayNhom() => new("Hệ thống", new[]
    {
        new MenuMuc("Tổng quan", "Người dùng, phân quyền, kho, vị trí, tồn kho và nhật ký.", "HeThong", "TrangChu", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
    });
}
