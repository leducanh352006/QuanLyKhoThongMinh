using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>Menu của Area Nhập kho. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class NhapKhoMenu : IMenuNguon
{
    public int ThuTu => 20;

    public MenuNhom LayNhom() => new("Nhập kho", new[]
    {
        new MenuMuc("Tổng quan", "Phiếu nhập, kiểm tra hàng nhập, sắp xếp vào ô.", "NhapKho", "TrangChu", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
    });
}
