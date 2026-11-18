using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.DieuChuyen.Controllers;

/// <summary>Tổng quan Area: các chức năng theo vai trò (lấy từ menu của Area), dùng view chung TongQuanArea.</summary>
[Area("DieuChuyen")]
public class TrangChuController(IMenuService menu, INguoiDungHienTai hienTai) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Điều chuyển kho";
        return View("TongQuanArea", new TongQuanArea("Điều chuyển kho", "Phiếu chuyển kho, tuyến vận chuyển, định mức kho.", menu.LayChucNangArea(hienTai.Lay()?.TenVaiTro, "DieuChuyen")));
    }
}
