using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>Menu của Area Điều chuyển kho. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class DieuChuyenMenu : IMenuNguon
{
    public int ThuTu => 50;

    public MenuNhom LayNhom() => new("Điều chuyển kho", new[]
    {
        new MenuMuc("Tổng quan", "Phiếu chuyển kho, tuyến vận chuyển, định mức kho.", "DieuChuyen", "TrangChu", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
    });
}
