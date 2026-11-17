using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>Tổng quan Area: các chức năng theo vai trò (lấy từ menu của Area), dùng view chung TongQuanArea.</summary>
[Area("XuatKho")]
public class TrangChuController(IMenuService menu, INguoiDungHienTai hienTai) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Xuất kho";
        return View("TongQuanArea", new TongQuanArea("Xuất kho", "Phiếu xuất, lấy hàng, đối chiếu và xác nhận xuất.", menu.LayChucNangArea(hienTai.Lay()?.TenVaiTro, "XuatKho")));
    }
}
