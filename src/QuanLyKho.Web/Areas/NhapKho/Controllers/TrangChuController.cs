using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>Trang tổng quan tạm của Area. TV1 Nguyễn Văn Hiếu thay bằng màn hình thật của nhóm khi làm việc của mình.</summary>
[Area("NhapKho")]
public class TrangChuController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Nhập kho";
        ViewData["MoTa"] = "Phiếu nhập, kiểm tra hàng nhập, sắp xếp vào ô.";
        ViewData["Nhom"] = "Nhóm 2 – Nhập kho (kèm gợi ý G2)";
        ViewData["NguoiPhuTrach"] = "TV1 Nguyễn Văn Hiếu";
        return View();
    }
}
