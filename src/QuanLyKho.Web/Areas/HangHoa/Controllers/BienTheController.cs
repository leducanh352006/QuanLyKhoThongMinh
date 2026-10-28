using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Danh sách biến thể (size, màu...) của một sản phẩm: thêm nhiều biến thể, sửa. Mã vạch phải duy nhất.</summary>
[Area("HangHoa")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho + "," + VaiTroTen.ThuKho)]
public class BienTheController(IBienTheQuanLy bienThe, ISanPhamQuanLy sanPham, INguoiDungHienTai hienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? maSP, CancellationToken ct)
    {
        var dsSanPham = await sanPham.LayDanhSach(ct);
        var chonSanPham = dsSanPham.FirstOrDefault(s => s.MaSP == maSP) ?? dsSanPham.FirstOrDefault();
        if (chonSanPham is null)
        {
            ViewData["Title"] = "Biến thể";
            return View("ChuaCoSanPham");
        }

        ViewData["Title"] = "Biến thể";
        ViewData["DanhSachSanPham"] = new SelectList(dsSanPham, "MaSP", "TenSP", chonSanPham.MaSP);
        ViewData["TenSP"] = chonSanPham.TenSP;
        ViewData["MaSP"] = chonSanPham.MaSP;
        return View(await bienThe.LayTheoSanPham(chonSanPham.MaSP, ct));
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Them(int maSP, CancellationToken ct)
    {
        var sp = await sanPham.Lay(maSP, ct);
        if (sp is null) return NotFound();
        ViewData["Title"] = "Thêm biến thể";
        return View(new BienTheThemViewModel { MaSP = maSP, TenSP = sp.TenSP });
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(BienTheThemViewModel model, CancellationToken ct)
    {
        var sp = await sanPham.Lay(model.MaSP, ct);
        if (sp is null) return NotFound();
        model.TenSP = sp.TenSP;
        ViewData["Title"] = "Thêm biến thể";

        if (ModelState.IsValid)
        {
            var kq = await bienThe.Them(new BienTheMoi(model.MaSP, model.MaVach, model.ThuocTinh, model.Dai, model.Rong, model.Cao, model.KhoiLuong), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index), new { maSP = model.MaSP });
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không thêm được biến thể.");
        }

        return View(model);
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken ct)
    {
        var bt = await bienThe.Lay(id, ct);
        if (bt is null) return NotFound();
        var sp = await sanPham.Lay(bt.MaSP, ct);
        ViewData["Title"] = "Sửa biến thể";
        return View(new BienTheSuaViewModel
        {
            MaBT = bt.MaBT, MaSP = bt.MaSP, TenSP = sp?.TenSP ?? "?", MaVach = bt.MaVach, ThuocTinh = bt.ThuocTinh,
            Dai = bt.Dai, Rong = bt.Rong, Cao = bt.Cao, KhoiLuong = bt.KhoiLuong,
        });
    }

    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(BienTheSuaViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa biến thể";
        if (ModelState.IsValid)
        {
            var kq = await bienThe.Sua(model.MaBT, new BienTheSuaDoi(model.MaVach, model.ThuocTinh, model.Dai, model.Rong, model.Cao, model.KhoiLuong), NguoiThucHien(), ct);
            if (kq.ThanhCong)
            {
                TempData["ThongBao"] = kq.ThongBao;
                return RedirectToAction(nameof(Index), new { maSP = model.MaSP });
            }
            ModelState.AddModelError(string.Empty, kq.ThongBao ?? "Không lưu được thay đổi.");
        }

        return View(model);
    }

    private int NguoiThucHien() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");
}
