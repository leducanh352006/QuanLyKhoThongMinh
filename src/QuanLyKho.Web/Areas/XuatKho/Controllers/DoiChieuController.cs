using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>
/// Đối chiếu hàng đã lấy (việc T2-5, issue #16): danh sách phiếu Đang lấy hàng đã quét mã xong hết (T2-4), màn hình so yêu cầu
/// với số thực lấy. Nghiệp vụ nằm ở <see cref="DoiChieuService"/>; controller chỉ nhận form, gọi service và hiện kết quả.
/// Quyền theo bảng PhanQuyen: Xem để vào, Sửa để đối chiếu (Thủ kho có quyền Sửa trên XK theo ma trận mặc định).
/// </summary>
[Area("XuatKho")]
[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
public class DoiChieuController(INguoiDungHienTai hienTai, IStockService ton, IAuditService audit, IGoiYG1Service goiYG1, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được đối chiếu.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private DoiChieuService Dv => new(Db, ton, audit, goiYG1);

    /// <summary>Các phiếu Đang lấy hàng đã quét mã xong hết (không còn dòng LayHang nào SLThucLay null) — sẵn sàng đối chiếu.</summary>
    public async Task<IActionResult> Index()
    {
        var ungVien = await Db.PhieuXuat.AsNoTracking().Include(p => p.Kho)
            .Where(p => p.TrangThai == PhieuXuat.DangLayHang).OrderBy(p => p.MaPX).Take(200).ToListAsync();
        if (ungVien.Count == 0) return View(ungVien);

        var maPX = ungVien.Select(p => p.MaPX).ToList();
        var conChoLay = await (
            from l in Db.LayHang.AsNoTracking()
            join c in Db.ChiTietPhieuXuat.AsNoTracking() on l.MaCTX equals c.MaCTX
            where maPX.Contains(c.MaPX) && l.SLThucLay == null
            select c.MaPX
        ).Distinct().ToListAsync();

        var saiSangDoiChieu = ungVien.Where(p => !conChoLay.Contains(p.MaPX)).ToList();
        return View(saiSangDoiChieu);
    }

    [HttpGet]
    public async Task<IActionResult> Xem(int id)
    {
        var p = await Db.PhieuXuat.AsNoTracking().Include(x => x.Kho).FirstOrDefaultAsync(x => x.MaPX == id);
        if (p is null) return NotFound();

        var kq = await Dv.Xem(id);
        if (!kq.ThanhCong)
        {
            TempData["Loi"] = kq.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Đối chiếu phiếu {p.SoPhieu}";
        return View((p, kq.GiaTri!));
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> XacNhan(int id)
    {
        var kq = await Dv.DoiChieu(id, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return kq is { ThanhCong: true, GiaTri: false }
            ? RedirectToAction("Lay", "LayHang", new { area = "XuatKho", id })   // còn dòng vừa gợi ý lại: quay về màn Lấy hàng
            : RedirectToAction(nameof(Xem), new { id });
    }
}
