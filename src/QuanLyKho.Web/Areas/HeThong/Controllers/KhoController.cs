using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Danh sách kho: thêm, sửa, ngừng hoạt động. Thêm kho (Bảng 2.2) dành cho Quản trị viên; Quản lý kho chỉ xem và sửa.</summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class KhoController(IKhoQuanLy kho, INguoiDungHienTai hienTai) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Kho";
        return View(await kho.LayDanhSach(ct));
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien)]
    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken ct)
    {
        await NapNguoiQuanLy(null, ct);
        await NapLoaiKho(null);
        ViewData["Title"] = "Thêm kho";
        return View(new KhoThemViewModel());
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(KhoThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm kho";
        if (ModelState.IsValid)
        {
            var kq = await kho.Them(new KhoMoi(model.TenKho, model.LoaiKho, model.DiaChi, model.MaQuanLy, model.ThoiGianCho), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được kho.");
        }

        await NapNguoiQuanLy(model.MaQuanLy, ct);
        await NapLoaiKho(model.LoaiKho);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var k = await kho.Lay(id, ct);
        if (k is null) return NotFound();
        await NapNguoiQuanLy(k.MaQuanLy, ct);
        await NapLoaiKho(k.LoaiKho);
        ViewData["Title"] = "Sửa kho";
        return View(new KhoSuaViewModel { MaKho = k.MaKho, TenKho = k.TenKho, LoaiKho = k.LoaiKho, DiaChi = k.DiaChi, MaQuanLy = k.MaQuanLy, ThoiGianCho = k.ThoiGianCho });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(KhoSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa kho";
        if (ModelState.IsValid)
        {
            var kq = await kho.Sua(model.MaKho, new KhoSuaDoi(model.TenKho, model.LoaiKho, model.DiaChi, model.MaQuanLy, model.ThoiGianCho), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        await NapNguoiQuanLy(model.MaQuanLy, ct);
        await NapLoaiKho(model.LoaiKho);
        return View(model);
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien)]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Ngung(int id, CancellationToken ct) => DoiTrangThai(id, false, ct);

    [Authorize(Roles = VaiTroTen.QuanTriVien)]
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> MoLai(int id, CancellationToken ct) => DoiTrangThai(id, true, ct);

    private async Task<IActionResult> DoiTrangThai(int id, bool hoatDong, CancellationToken ct)
    {
        var kq = await kho.DatTrangThai(id, hoatDong, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private async Task NapNguoiQuanLy(int? chon, CancellationToken ct)
        => ViewData["NguoiQuanLy"] = new SelectList(await kho.LayNguoiCoTheQuanLy(ct), "MaND", "HoTen", chon);

    // new SelectList(danh sách chuỗi phẳng, selectedValue) gây NullReferenceException thật trong ASP.NET Core khi render
    // (MultiSelectList.GetListItemsWithValueField); tự dựng List<SelectListItem> để tránh nhánh mã đó.
    private Task NapLoaiKho(string? chon)
    {
        ViewData["LoaiKho"] = LoaiKhoGiaTri.TatCa.Select(x => new SelectListItem(x, x, x == chon)).ToList();
        return Task.CompletedTask;
    }
}
