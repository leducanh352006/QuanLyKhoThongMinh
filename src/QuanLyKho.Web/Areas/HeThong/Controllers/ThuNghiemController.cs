using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Trang thử các thành phần dùng chung. Chỉ mở ở môi trường Development.</summary>
[Area("HeThong")]
public class ThuNghiemController(IWebHostEnvironment env) : Controller
{
    public IActionResult QuetMa()
    {
        if (!env.IsDevelopment()) return NotFound();
        ViewData["Title"] = "Thử thành phần quét mã";
        return View();
    }
}
