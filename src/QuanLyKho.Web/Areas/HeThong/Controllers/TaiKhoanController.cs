using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Đăng nhập, đăng xuất, trang từ chối truy cập (việc T4-2).</summary>
[Area("HeThong")]
public class TaiKhoanController(ITaiKhoanService taiKhoan, INguoiDungHienTai hienTai) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        if (hienTai.Lay() is not null) return VeTrangChu(returnUrl);
        ViewData["Title"] = "Đăng nhập";
        return View(new DangNhapViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Đăng nhập";
        if (!ModelState.IsValid)
        {
            model.MatKhau = "";
            return View(model);
        }

        var kq = await taiKhoan.DangNhap(model.TenDangNhap, model.MatKhau, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!kq.ThanhCong)
        {
            if (kq.MaLoi == MaLoiTaiKhoan.QuaNhieuLan) Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Đăng nhập không thành công.");
            model.MatKhau = "";   // không đưa mật khẩu lại ra trang
            return View(model);
        }

        await HttpContext.SignInAsync(
            XacThucCookie.Scheme,
            NguoiDungHienTaiService.TaoPrincipal(kq.GiaTri!, XacThucCookie.Scheme),
            new AuthenticationProperties { IsPersistent = false });
        return VeTrangChu(model.ReturnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat(CancellationToken ct)
    {
        if (hienTai.Lay() is { } nguoi) await taiKhoan.GhiDangXuat(nguoi.MaND, ct);
        await HttpContext.SignOutAsync(XacThucCookie.Scheme);
        return RedirectToAction(nameof(DangNhap));
    }

    /// <summary>Trang người đã đăng nhập nhưng không đủ quyền bị chuyển tới (mã 403).</summary>
    [AllowAnonymous]
    public IActionResult TuChoi()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Title"] = "Không đủ quyền";
        return View();
    }

    // Chỉ chuyển tới địa chỉ nội bộ để không bị lợi dụng chuyển người dùng sang trang lạ.
    private IActionResult VeTrangChu(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home", new { area = "" });
}
