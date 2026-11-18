using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Controllers;

/// <summary>
/// Định mức kho (việc T5-1, issue #28): tồn tối thiểu (m) và số ngày an toàn (N) của một biến thể tại một kho, dùng làm đầu vào
/// cho gợi ý G3. Không phải chứng từ nên kiểm quyền bằng vai trò, không dùng CanQuyen.
/// </summary>
[Area("DieuChuyen")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class DinhMucKhoController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được định mức kho.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private DinhMucKhoService Dv => new(Db, audit);

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Định mức kho";
        return View(await Dv.LayDanhSach(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm định mức kho";
        await NapDanhSachChon(ct);
        return View(new DinhMucKhoThemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(DinhMucKhoThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm định mức kho";
        if (ModelState.IsValid)
        {
            var kq = await Dv.Them(new Models.DinhMucKho { MaKho = model.MaKho, MaBT = model.MaBT, TonToiThieu = model.TonToiThieu, NgayAnToan = model.NgayAnToan }, MaNguoiDung(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được định mức kho.");
        }

        await NapDanhSachChon(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int maKho, int maBT, CancellationToken ct)
    {
        var d = await Dv.Lay(maKho, maBT, ct);
        if (d is null) return NotFound();

        var tenKho = await Db.Kho.Where(k => k.MaKho == maKho).Select(k => k.TenKho).FirstOrDefaultAsync(ct) ?? $"Kho #{maKho}";
        var tenBt = await Db.BienThe.Where(b => b.MaBT == maBT).Join(Db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => s.TenSP + " (" + b.ThuocTinh + ")").FirstOrDefaultAsync(ct) ?? $"Biến thể #{maBT}";

        ViewData["Title"] = "Sửa định mức kho";
        return View(new DinhMucKhoSuaViewModel { MaKho = d.MaKho, MaBT = d.MaBT, TenKho = tenKho, TenBienThe = tenBt, TonToiThieu = d.TonToiThieu, NgayAnToan = d.NgayAnToan });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(DinhMucKhoSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa định mức kho";
        if (ModelState.IsValid)
        {
            var kq = await Dv.Sua(model.MaKho, model.MaBT, model.TonToiThieu, model.NgayAnToan, MaNguoiDung(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        return View(model);
    }

    private async Task NapDanhSachChon(CancellationToken ct)
    {
        ViewBag.Kho = await Db.Kho.Where(k => k.TrangThai == Areas.HeThong.Models.TrangThaiHoatDong.HoatDong)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho, ct);
        ViewBag.BienThe = await Db.BienThe.Join(Db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .OrderBy(x => x.Ten).ToDictionaryAsync(x => x.MaBT, x => x.Ten, ct);
    }
}
