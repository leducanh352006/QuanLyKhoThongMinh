using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>
/// Ghi nhận phát sinh (việc T3-6): màn hình các dòng lệch của một phiếu kiểm, chọn loại phát sinh và nhập nguyên nhân.
/// Nghiệp vụ nằm ở <see cref="PhatSinhService"/>. Quyền theo bảng PhanQuyen: Xem để vào, Sửa để ghi nhận.
/// </summary>
[Area("HangHoa")]
[CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Xem)]
public class PhatSinhController(INguoiDungHienTai hienTai, IAuditService audit, ICapacityService sucChua, KhoDbContext? db = null) : Controller
{
    private const string ChuaCoCsdl = "Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được ghi nhận phát sinh.";

    private PhatSinhService DichVu(KhoDbContext csdl) => new(new EfKiemDemStore(csdl), sucChua, audit);

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    [HttpGet]
    public async Task<IActionResult> Index(int id, LocPhatSinh loc = LocPhatSinh.TatCa, CancellationToken ct = default)
    {
        if (db is null) return VeDanhSachPhieu();
        var trang = await DichVu(db).LayTrang(id, loc, ct: ct);
        return trang is null ? NotFound() : View(trang);
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> PhanLoai(int id, PhanLoaiVm vm, LocPhatSinh loc = LocPhatSinh.TatCa, CancellationToken ct = default)
    {
        if (db is null) return VeDanhSachPhieu();
        var dichVu = DichVu(db);
        var kq = await dichVu.PhanLoai(id, vm, MaNguoiDung(), ct);
        if (kq.ThanhCong)
        {
            TempData["ThongBao"] = kq.ThongBao;
            return RedirectToAction(nameof(Index), new { id, loc });
        }

        // Lỗi thì hiện lại màn hình với dữ liệu người dùng vừa nhập, để sửa tiếp không phải chọn lại.
        var trang = await dichVu.LayTrang(id, loc, vm, ct);
        if (trang is null) return NotFound();
        ViewData["Loi"] = kq.ThongBao;
        return View(nameof(Index), trang);
    }

    private IActionResult VeDanhSachPhieu()
    {
        TempData["Loi"] = ChuaCoCsdl;
        return RedirectToAction("Index", "PhieuKiem");
    }
}
