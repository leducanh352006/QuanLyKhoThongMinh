using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Danh sách nhà cung cấp: thêm, sửa, ngừng dùng. Nhà cung cấp ngừng dùng không chọn được ở phiếu nhập.</summary>
[Area("HangHoa")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class NhaCungCapController(INhaCungCapQuanLy nhaCungCap, INguoiDungHienTai hienTai) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Nhà cung cấp";
        return View(await nhaCungCap.LayDanhSach(ct));
    }

    [HttpGet]
    public IActionResult Them()
    {
        ViewData["Title"] = "Thêm nhà cung cấp";
        return View(new NhaCungCapThemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(NhaCungCapThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm nhà cung cấp";
        if (ModelState.IsValid)
        {
            var kq = await nhaCungCap.Them(new NhaCungCapMoi(model.TenNCC, model.SDT, model.DiaChi), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được nhà cung cấp.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var n = await nhaCungCap.Lay(id, ct);
        if (n is null) return NotFound();
        ViewData["Title"] = "Sửa nhà cung cấp";
        return View(new NhaCungCapSuaViewModel { MaNCC = n.MaNCC, TenNCC = n.TenNCC, SDT = n.SDT, DiaChi = n.DiaChi });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(NhaCungCapSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa nhà cung cấp";
        if (ModelState.IsValid)
        {
            var kq = await nhaCungCap.Sua(model.MaNCC, new NhaCungCapSuaDoi(model.TenNCC, model.SDT, model.DiaChi), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Ngung(int id, CancellationToken ct) => DoiTrangThai(id, false, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> MoLai(int id, CancellationToken ct) => DoiTrangThai(id, true, ct);

    private async Task<IActionResult> DoiTrangThai(int id, bool hoatDong, CancellationToken ct)
    {
        var kq = await nhaCungCap.DatTrangThai(id, hoatDong, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");
}
