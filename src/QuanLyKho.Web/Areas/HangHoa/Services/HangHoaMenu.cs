using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Menu của Area Hàng hóa. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class HangHoaMenu : IMenuNguon
{
    public int ThuTu => 40;

    public MenuNhom LayNhom() => new("Hàng hóa", new[]
    {
        new MenuMuc("Tổng quan", "Sản phẩm, biến thể, lô, hạn dùng và kiểm kê.", "HangHoa", "TrangChu", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
        new MenuMuc("Nhà cung cấp", "Thêm, sửa, ngừng dùng nhà cung cấp.", "HangHoa", "NhaCungCap", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
        new MenuMuc("Danh mục sản phẩm", "Thêm, sửa, xóa danh mục sản phẩm.", "HangHoa", "DanhMuc", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
    });
}
