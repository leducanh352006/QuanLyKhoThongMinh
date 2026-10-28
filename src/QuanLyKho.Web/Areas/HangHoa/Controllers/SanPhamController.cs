using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Danh sách sản phẩm: thêm, sửa. Xem được cả Thủ kho; sửa đổi chỉ Quản trị viên, Quản lý kho.</summary>
[Area("HangHoa")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho + "," + VaiTroTen.ThuKho)]
public class SanPhamController(ISanPhamQuanLy sanPham, IDanhMucQuanLy danhMuc, INguoiDungHienTai hienTai) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Sản phẩm";
        return View(await sanPham.LayDanhSach(ct));
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken ct)
    {
        await NapDanhMuc(null, ct);
        ViewData["Title"] = "Thêm sản phẩm";
        return View(new SanPhamThemViewModel());
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(SanPhamThemViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm sản phẩm";
        if (ModelState.IsValid)
        {
            var kq = await sanPham.Them(new SanPhamMoi(model.TenSP, model.MaDM, model.DonViTinh, model.CoHanDung), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được sản phẩm.");
        }

        await NapDanhMuc(model.MaDM, ct);
        return View(model);
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var sp = await sanPham.Lay(id, ct);
        if (sp is null) return NotFound();
        await NapDanhMuc(sp.MaDM, ct);
        ViewData["Title"] = "Sửa sản phẩm";
        return View(new SanPhamSuaViewModel { MaSP = sp.MaSP, TenSP = sp.TenSP, MaDM = sp.MaDM, DonViTinh = sp.DonViTinh, CoHanDung = sp.CoHanDung });
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(SanPhamSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa sản phẩm";
        if (ModelState.IsValid)
        {
            var kq = await sanPham.Sua(model.MaSP, new SanPhamSuaDoi(model.TenSP, model.MaDM, model.DonViTinh, model.CoHanDung), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        await NapDanhMuc(model.MaDM, ct);
        return View(model);
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private async Task NapDanhMuc(int? chon, CancellationToken ct)
        => ViewData["DanhMuc"] = new SelectList(await danhMuc.LayDanhSach(ct), "MaDM", "TenDM", chon);
}
