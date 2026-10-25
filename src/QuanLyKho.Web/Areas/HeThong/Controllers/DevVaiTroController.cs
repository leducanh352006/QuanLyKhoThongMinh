using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Infrastructure.NguoiDung;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Đổi vai trò đang dùng để xem menu từng vai trò. Chỉ có tác dụng ở môi trường Development; việc Đăng nhập sẽ thay thế.</summary>
[Area("HeThong")]
public class DevVaiTroController(IWebHostEnvironment env) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Chon(int maVaiTro, string? returnUrl)
    {
        if (!env.IsDevelopment()) return NotFound();
        if (SeedDataSet.VaiTro.All(v => v.MaVaiTro != maVaiTro)) return BadRequest();

        Response.Cookies.Append(DevNguoiDungHienTai.TenCookie, maVaiTro.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Secure = Request.IsHttps,
        });

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home", new { area = "" });
    }
}
