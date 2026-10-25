using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Trang tổng quan tạm của Area. TV4 Lê Đức Anh thay bằng màn hình thật của nhóm khi làm việc của mình.</summary>
[Area("HeThong")]
public class TrangChuController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Hệ thống";
        ViewData["MoTa"] = "Người dùng, phân quyền, kho, vị trí, tồn kho và nhật ký.";
        ViewData["Nhom"] = "Nhóm 1 – Hệ thống nền, kho và tồn kho";
        ViewData["NguoiPhuTrach"] = "TV4 Lê Đức Anh";
        return View();
    }
}
