using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Controllers;

/// <summary>
/// Tuyến vận chuyển (việc T5-1, issue #28): tham số chi phí giữa từng cặp kho, dùng làm đầu vào cho gợi ý G4. Không phải chứng
/// từ (không có LoaiChungTu riêng) nên kiểm quyền bằng vai trò như Kho, Nhà cung cấp — không dùng CanQuyen.
/// </summary>
[Area("DieuChuyen")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class TuyenVanChuyenController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được tuyến vận chuyển.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private TuyenVanChuyenService Dv => new(Db, audit);

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Tuyến vận chuyển";
        return View(await Dv.LayDanhSach(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm tuyến vận chuyển";
        await NapDanhSachKho(ct);
        return View(new TuyenVanChuyenThemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(TuyenVanChuyenThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm tuyến vận chuyển";
        if (ModelState.IsValid)
        {
            var kq = await Dv.Them(new TuyenVanChuyen { MaKhoXuat = model.MaKhoXuat, MaKhoNhap = model.MaKhoNhap, DonGiaVC = model.DonGiaVC, PhiChuyen = model.PhiChuyen }, MaNguoiDung(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được tuyến vận chuyển.");
        }

        await NapDanhSachKho(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int maKhoXuat, int maKhoNhap, CancellationToken ct)
    {
        var t = await Dv.Lay(maKhoXuat, maKhoNhap, ct);
        if (t is null) return NotFound();
        ViewData["Title"] = "Sửa tuyến vận chuyển";
        return View(new TuyenVanChuyenSuaViewModel
        {
            MaKhoXuat = t.MaKhoXuat, MaKhoNhap = t.MaKhoNhap,
            TenKhoXuat = t.KhoXuat?.TenKho ?? $"Kho #{t.MaKhoXuat}", TenKhoNhap = t.KhoNhap?.TenKho ?? $"Kho #{t.MaKhoNhap}",
            DonGiaVC = t.DonGiaVC, PhiChuyen = t.PhiChuyen,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(TuyenVanChuyenSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa tuyến vận chuyển";
        if (ModelState.IsValid)
        {
            var kq = await Dv.Sua(model.MaKhoXuat, model.MaKhoNhap, model.DonGiaVC, model.PhiChuyen, MaNguoiDung(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        return View(model);
    }

    private async Task NapDanhSachKho(CancellationToken ct) =>
        ViewBag.Kho = await Db.Kho.Where(k => k.TrangThai == Areas.HeThong.Models.TrangThaiHoatDong.HoatDong)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho, ct);
}
