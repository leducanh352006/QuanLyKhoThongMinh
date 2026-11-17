using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Tổng quan Area: các chức năng theo vai trò (lấy từ menu của Area), dùng view chung TongQuanArea.</summary>
[Area("HangHoa")]
public class TrangChuController(IMenuService menu, INguoiDungHienTai hienTai) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Hàng hóa";
        return View("TongQuanArea", new TongQuanArea("Hàng hóa", "Sản phẩm, biến thể, lô, hạn dùng và kiểm kê.", menu.LayChucNangArea(hienTai.Lay()?.TenVaiTro, "HangHoa")));
    }
}
