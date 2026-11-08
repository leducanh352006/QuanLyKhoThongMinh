using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>
/// Xác nhận xuất (việc T2-6, issue #15): danh sách phiếu Đã đối chiếu (T2-5) sẵn sàng ghi giảm tồn, màn hình xác nhận. Nghiệp vụ
/// nằm ở <see cref="XacNhanXuatService"/>; controller chỉ nhận form, gọi service và hiện kết quả.
/// Quyền theo bảng PhanQuyen: Xem để vào, Sửa để xác nhận (Thủ kho có quyền Sửa trên XK theo ma trận mặc định).
/// </summary>
[Area("XuatKho")]
[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
public class XacNhanXuatController(INguoiDungHienTai hienTai, IStockService ton, IAuditService audit, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được xác nhận xuất.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private XacNhanXuatService Dv => new(Db, ton, audit);

    /// <summary>Các phiếu Đã đối chiếu — sẵn sàng xác nhận xuất (ghi giảm tồn thật).</summary>
    public async Task<IActionResult> Index()
    {
        var ds = await Db.PhieuXuat.AsNoTracking().Include(p => p.Kho)
            .Where(p => p.TrangThai == PhieuXuat.DaDoiChieu).OrderBy(p => p.MaPX).Take(200).ToListAsync();
        return View(ds);
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

        ViewData["Title"] = $"Xác nhận xuất phiếu {p.SoPhieu}";
        return View((p, kq.GiaTri!));
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> XacNhan(int id)
    {
        var kq = await Dv.XacNhan(id, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return kq.ThanhCong ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Xem), new { id });
    }
}
