using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Trang tổng quan tạm của Area. TV3 Dương Tuấn Anh thay bằng màn hình thật của nhóm khi làm việc của mình.</summary>
[Area("HangHoa")]
public class TrangChuController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Hàng hóa";
        ViewData["MoTa"] = "Sản phẩm, biến thể, lô, hạn dùng và kiểm kê.";
        ViewData["Nhom"] = "Nhóm 4 – Danh mục hàng hóa và kiểm tra hàng hóa";
        ViewData["NguoiPhuTrach"] = "TV3 Dương Tuấn Anh";
        return View();
    }
}
