using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>Tổng quan Area: các chức năng theo vai trò (lấy từ menu của Area), dùng view chung TongQuanArea.</summary>
[Area("NhapKho")]
public class TrangChuController(IMenuService menu, INguoiDungHienTai hienTai) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Nhập kho";
        return View("TongQuanArea", new TongQuanArea("Nhập kho", "Phiếu nhập, kiểm tra hàng nhập, sắp xếp vào ô.", menu.LayChucNangArea(hienTai.Lay()?.TenVaiTro, "NhapKho")));
    }
}
