using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>Menu của Area Xuất kho. Nhóm phụ trách thêm mục mới (mỗi màn hình một <see cref="MenuMuc"/>) vào đây.</summary>
public sealed class XuatKhoMenu : IMenuNguon
{
    public int ThuTu => 30;

    public MenuNhom LayNhom() => new("Xuất kho", new[]
    {
        new MenuMuc("Tổng quan", "Phiếu xuất, lấy hàng, đối chiếu và xác nhận xuất.", "XuatKho", "TrangChu", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho, VaiTroTen.NhanVienBanHang }),
        new MenuMuc("Phiếu xuất", "Danh sách, tạo, sửa và duyệt phiếu xuất kho.", "XuatKho", "PhieuXuat", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho, VaiTroTen.NhanVienBanHang }),
        new MenuMuc("Lấy hàng", "Quét mã ô, mã lô, mã vạch để xác nhận lấy hàng theo phiếu đã duyệt.", "XuatKho", "LayHang", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
        new MenuMuc("Đối chiếu", "So hàng đã lấy với phiếu, gợi ý lại phần thiếu hoặc xuất một phần.", "XuatKho", "DoiChieu", "Index", new[] { VaiTroTen.QuanLyKho, VaiTroTen.ThuKho }),
    });
}
