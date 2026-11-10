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
        new MenuMuc("Tuyến vận chuyển", "Đơn giá và phí mỗi chuyến giữa từng cặp kho — tham số cho gợi ý G4.", "DieuChuyen", "TuyenVanChuyen", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
        new MenuMuc("Định mức kho", "Tồn tối thiểu và số ngày an toàn từng biến thể — tham số cho gợi ý G3.", "DieuChuyen", "DinhMucKho", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
    });
}
