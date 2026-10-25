using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>Trang tổng quan tạm của Area. TV2 Trần Tiến Đức thay bằng màn hình thật của nhóm khi làm việc của mình.</summary>
[Area("XuatKho")]
public class TrangChuController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Xuất kho";
        ViewData["MoTa"] = "Phiếu xuất, lấy hàng, đối chiếu và xác nhận xuất.";
        ViewData["Nhom"] = "Nhóm 3 – Xuất kho (kèm gợi ý G1)";
        ViewData["NguoiPhuTrach"] = "TV2 Trần Tiến Đức";
        return View();
    }
}
