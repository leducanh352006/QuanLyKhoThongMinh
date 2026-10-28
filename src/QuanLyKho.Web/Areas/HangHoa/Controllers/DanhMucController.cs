using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Danh sách danh mục sản phẩm: thêm, sửa, xóa (chặn khi còn sản phẩm tham chiếu).</summary>
[Area("HangHoa")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class DanhMucController(IDanhMucQuanLy danhMuc, INguoiDungHienTai hienTai) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Danh mục sản phẩm";
        return View(await danhMuc.LayDanhSach(ct));
    }

    [HttpGet]
    public IActionResult Them()
    {
        ViewData["Title"] = "Thêm danh mục";
        return View(new DanhMucThemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(DanhMucThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm danh mục";
        if (ModelState.IsValid)
        {
            var kq = await danhMuc.Them(new DanhMucMoi(model.TenDM), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được danh mục.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var d = await danhMuc.Lay(id, ct);
        if (d is null) return NotFound();
        ViewData["Title"] = "Sửa danh mục";
        return View(new DanhMucSuaViewModel { MaDM = d.MaDM, TenDM = d.TenDM });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(DanhMucSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa danh mục";
        if (ModelState.IsValid)
        {
            var kq = await danhMuc.Sua(model.MaDM, new DanhMucSuaDoi(model.TenDM), NguoiThucHien(), ct);
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
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        var kq = await danhMuc.Xoa(id, NguoiThucHien(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");
}
