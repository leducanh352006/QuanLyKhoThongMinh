using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Cây vị trí nhiều cấp của một kho: thêm, sửa, khóa (xóa mềm), xóa cứng. Xem được cả Thủ kho; sửa đổi chỉ Quản trị viên, Quản lý kho.</summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho + "," + VaiTroTen.ThuKho)]
public class ViTriController(IViTriQuanLy viTri, IKhoQuanLy kho, INguoiDungHienTai hienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? maKho, CancellationToken ct)
    {
        var dsKho = await kho.LayDanhSach(ct);
        var chonKho = dsKho.FirstOrDefault(k => k.MaKho == maKho) ?? dsKho.FirstOrDefault();
        if (chonKho is null)
        {
            ViewData["Title"] = "Vị trí";
            return View("ChuaCoKho");
        }

        var kq = await viTri.LayCay(chonKho.MaKho, ct);
        ViewData["Title"] = "Vị trí";
        ViewData["DanhSachKho"] = new SelectList(dsKho, "MaKho", "TenKho", chonKho.MaKho);
        ViewData["TenKho"] = chonKho.TenKho;
        ViewData["MaKho"] = chonKho.MaKho;
        return View(kq.GiaTri ?? Array.Empty<ViTriCayDto>());
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Them(int maKho, int? maViTriCha, CancellationToken ct)
    {
        var k = await kho.Lay(maKho, ct);
        if (k is null) return NotFound();
        await NapCap();
        await NapLoaiViTri();
        await NapCha(maKho, maViTriCha, ct);
        ViewData["Title"] = "Thêm vị trí";
        return View(new ViTriThemViewModel { MaKho = maKho, TenKho = k.TenKho, MaViTriCha = maViTriCha, CapViTri = CapViTriGiaTri.Day });
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(ViTriThemViewModel model, CancellationToken ct)
    {
        var k = await kho.Lay(model.MaKho, ct);
        if (k is null) return NotFound();
        model.TenKho = k.TenKho;
        ViewData["Title"] = "Thêm vị trí";

        if (ModelState.IsValid)
        {
            var kq = await viTri.Them(new ViTriMoi(model.TenViTri, model.MaKho, model.MaViTriCha, model.CapViTri,
                model.LoaiViTri, model.MaVachO, model.Dai, model.Rong, model.Cao, model.KhoiLuongToiDa), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index), new { maKho = model.MaKho });
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được vị trí.");
        }

        await NapCap();
        await NapLoaiViTri();
        await NapCha(model.MaKho, model.MaViTriCha, ct);
        return View(model);
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var v = await viTri.LayMot(id, ct);
        if (v is null) return NotFound();
        await NapLoaiViTri();
        ViewData["Title"] = "Sửa vị trí";
        return View(new ViTriSuaViewModel
        {
            MaViTri = v.MaViTri, MaKho = v.MaKho, CapViTri = v.CapViTri, DuongDanDayDu = v.DuongDanDayDu, TenViTri = v.TenViTri,
            LoaiViTri = v.LoaiViTri, MaVachO = v.MaVachO, Dai = v.Dai, Rong = v.Rong, Cao = v.Cao, KhoiLuongToiDa = v.KhoiLuongToiDa,
        });
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(ViTriSuaViewModel model, CancellationToken ct)
    {
        var hienCo = await viTri.LayMot(model.MaViTri, ct);
        if (hienCo is null) return NotFound();
        model.MaKho = hienCo.MaKho;
        model.CapViTri = hienCo.CapViTri;
        model.DuongDanDayDu = hienCo.DuongDanDayDu;
        ViewData["Title"] = "Sửa vị trí";

        if (ModelState.IsValid)
        {
            var kq = await viTri.Sua(model.MaViTri, new ViTriSuaDoi(model.TenViTri, model.LoaiViTri, model.MaVachO, model.Dai, model.Rong, model.Cao, model.KhoiLuongToiDa), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index), new { maKho = model.MaKho });
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        await NapLoaiViTri();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Khoa(int id, int maKho, CancellationToken ct) => DoiTrangThai(id, maKho, false, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> MoLai(int id, int maKho, CancellationToken ct) => DoiTrangThai(id, maKho, true, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, int maKho, CancellationToken ct)
    {
        var kq = await viTri.Xoa(id, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { maKho });
    }

    private async Task<IActionResult> DoiTrangThai(int id, int maKho, bool hoatDong, CancellationToken ct)
    {
        var kq = await viTri.DatTrangThai(id, hoatDong, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { maKho });
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // new SelectList(danh sách chuỗi phẳng, ...) gây NullReferenceException thật trong ASP.NET Core khi render
    // (MultiSelectList.GetListItemsWithValueField); tự dựng List<SelectListItem> để tránh nhánh mã đó.
    private Task NapCap(string? chon = null)
    {
        ViewData["CapViTri"] = CapViTriGiaTri.ThuTu.Select(x => new SelectListItem(x, x, x == chon)).ToList();
        return Task.CompletedTask;
    }

    private Task NapLoaiViTri(string? chon = null)
    {
        ViewData["LoaiViTri"] = LoaiViTriGiaTri.TatCa.Select(x => new SelectListItem(x, x, x == chon)).ToList();
        return Task.CompletedTask;
    }

    private async Task NapCha(int maKho, int? chon, CancellationToken ct)
    {
        var ungVien = await viTri.LayUngVienLamCha(maKho, ct);
        var ds = ungVien.Select(x => new { x.MaViTri, x.DuongDan }).ToList();
        ViewData["ViTriCha"] = new SelectList(ds, "MaViTri", "DuongDan", chon);
    }
}
