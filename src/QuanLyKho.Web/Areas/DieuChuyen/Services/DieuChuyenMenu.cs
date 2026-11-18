using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>Menu của Area Điều chuyển kho. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class DieuChuyenMenu : IMenuNguon
{
    public int ThuTu => 50;

    public MenuNhom LayNhom() => new("Điều chuyển kho", new[]
    {
        new MenuMuc("Tổng quan", "Phiếu chuyển kho, tuyến vận chuyển, định mức kho.", "DieuChuyen", "TrangChu", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
        new MenuMuc("Phiếu chuyển kho", "Lập, duyệt phiếu chuyển hàng giữa hai kho; quét mã xuất ở kho đi, quét mã nhận ở kho đến.", "DieuChuyen", "PhieuChuyen", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
        new MenuMuc("Tuyến vận chuyển", "Đơn giá mỗi đơn vị hàng và phí mỗi chuyến giữa từng cặp kho; phiếu chuyển chỉ lập được trên tuyến đã khai báo.", "DieuChuyen", "TuyenVanChuyen", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
        new MenuMuc("Định mức kho", "Tồn tối thiểu của từng mặt hàng ở từng kho; báo mặt hàng đang dưới định mức để chuyển bổ sung.", "DieuChuyen", "DinhMucKho", "Index", new[] { VaiTroTen.QuanTriVien, VaiTroTen.QuanLyKho }),
    });
}
