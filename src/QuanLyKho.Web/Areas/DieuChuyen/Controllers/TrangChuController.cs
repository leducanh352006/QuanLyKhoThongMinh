using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.DieuChuyen.Controllers;

/// <summary>Trang tổng quan tạm của Area. TV5 Nguyễn Minh Hiếu thay bằng màn hình thật của nhóm khi làm việc của mình.</summary>
[Area("DieuChuyen")]
public class TrangChuController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Điều chuyển kho";
        ViewData["MoTa"] = "Phiếu chuyển kho, tuyến vận chuyển, định mức kho.";
        ViewData["Nhom"] = "Nhóm 5 – Điều chuyển giữa các kho (kèm gợi ý G3, G4)";
        ViewData["NguoiPhuTrach"] = "TV5 Nguyễn Minh Hiếu";
        return View();
    }
}
