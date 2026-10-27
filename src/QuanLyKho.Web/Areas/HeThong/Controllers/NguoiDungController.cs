using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Quản lý người dùng: thêm, sửa, khóa, đặt lại mật khẩu. Chỉ Quản trị viên (Bảng 2.2), kiểm ở phía máy chủ.</summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien)]
public class NguoiDungController(INguoiDungQuanLy nguoiDung, INguoiDungHienTai hienTai) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Người dùng";
        ViewData["MaNguoiDangDung"] = hienTai.Lay()?.MaND;
        return View(await nguoiDung.LayDanhSach(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken ct)
    {
        await NapVaiTro(null, ct);
        ViewData["Title"] = "Thêm người dùng";
        return View(new NguoiDungThemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(NguoiDungThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm người dùng";
        if (ModelState.IsValid)
        {
            var kq = await nguoiDung.Them(new NguoiDungMoi(model.TenDangNhap, model.MatKhau, model.HoTen, model.Email, model.SDT, model.MaVaiTro), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được người dùng.");
        }

        model.MatKhau = "";
        await NapVaiTro(model.MaVaiTro, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var n = await nguoiDung.Lay(id, ct);
        if (n is null) return NotFound();
        await NapVaiTro(n.MaVaiTro, ct);
        ViewData["Title"] = "Sửa người dùng";
        return View(new NguoiDungSuaViewModel { MaND = n.MaND, TenDangNhap = n.TenDangNhap, HoTen = n.HoTen, Email = n.Email, SDT = n.SDT, MaVaiTro = n.MaVaiTro });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(NguoiDungSuaViewModel model, CancellationToken ct)
    {
        var n = await nguoiDung.Lay(model.MaND, ct);
        if (n is null) return NotFound();
        model.TenDangNhap = n.TenDangNhap;   // tên đăng nhập không đổi được
        ViewData["Title"] = "Sửa người dùng";

        if (ModelState.IsValid)
        {
            var kq = await nguoiDung.Sua(model.MaND, new NguoiDungSuaDoi(model.HoTen, model.Email, model.SDT, model.MaVaiTro), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        await NapVaiTro(model.MaVaiTro, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Khoa(int id, CancellationToken ct) => DoiTrangThai(id, false, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> MoKhoa(int id, CancellationToken ct) => DoiTrangThai(id, true, ct);

    [HttpGet]
    public async Task<IActionResult> DatLaiMatKhau(int id, CancellationToken ct)
    {
        var n = await nguoiDung.Lay(id, ct);
        if (n is null) return NotFound();
        ViewData["Title"] = "Đặt lại mật khẩu";
        return View(new DatLaiMatKhauViewModel { MaND = n.MaND, TenDangNhap = n.TenDangNhap });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(DatLaiMatKhauViewModel model, CancellationToken ct)
    {
        var n = await nguoiDung.Lay(model.MaND, ct);
        if (n is null) return NotFound();
        model.TenDangNhap = n.TenDangNhap;
        ViewData["Title"] = "Đặt lại mật khẩu";

        if (ModelState.IsValid)
        {
            var kq = await nguoiDung.DatLaiMatKhau(model.MaND, model.MatKhauMoi, NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không đặt lại được mật khẩu.");
        }

        model.MatKhauMoi = "";
        return View(model);
    }

    private async Task<IActionResult> DoiTrangThai(int id, bool hoatDong, CancellationToken ct)
    {
        var kq = await nguoiDung.DatTrangThai(id, hoatDong, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private async Task NapVaiTro(int? chon, CancellationToken ct)
        => ViewData["VaiTro"] = new SelectList(await nguoiDung.LayVaiTro(ct), nameof(VaiTro.MaVaiTro), nameof(VaiTro.TenVaiTro), chon);
}
