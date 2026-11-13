using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>
/// Kiểm đếm quét mã (việc T3-5): màn hình đếm của một phiếu kiểm đang ở trạng thái Đang kiểm, các tab Khớp, Thừa, Thiếu, Chưa kiểm.
/// Nghiệp vụ nằm ở <see cref="KiemDemService"/>. Quyền theo bảng PhanQuyen: Xem để vào, Sửa để ghi số đếm.
/// </summary>
[Area("HangHoa")]
[CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Xem)]
public class KiemDemController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    private const string ChuaCoCsdl = "Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được kiểm đếm.";

    private KiemDemService DichVu(KhoDbContext csdl) => new(new EfKiemDemStore(csdl), audit);

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    [HttpGet]
    public async Task<IActionResult> Index(int id, TabKiemDem tab = TabKiemDem.TatCa, CancellationToken ct = default)
    {
        if (db is null) return VeDanhSachPhieu();
        // Giữ mã ô vừa quét để đếm tiếp các lô khác trong cùng ô mà không phải quét lại.
        var form = new GhiNhanDemVm { MaVachO = TempData["MaVachO"] as string, MaHang = TempData["MaHang"] as string };
        var trang = await DichVu(db).LayTrang(id, tab, form, ct);
        return trang is null ? NotFound() : View(trang);
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> GhiNhan(int id, GhiNhanDemVm vm, TabKiemDem tab = TabKiemDem.TatCa, CancellationToken ct = default)
    {
        if (db is null) return VeDanhSachPhieu();
        var kq = await DichVu(db).GhiNhan(id, vm, MaNguoiDung(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        TempData["MaVachO"] = vm.MaVachO?.Trim();
        if (!kq.ThanhCong) TempData["MaHang"] = vm.MaHang?.Trim();   // lỗi thì giữ lại mã hàng để sửa, thành công thì để trống cho lô kế tiếp
        return RedirectToAction(nameof(Index), new { id, tab });
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> BoDem(int id, int maDK, TabKiemDem tab = TabKiemDem.TatCa, CancellationToken ct = default)
    {
        if (db is null) return VeDanhSachPhieu();
        var kq = await DichVu(db).BoDem(id, maDK, MaNguoiDung(), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { id, tab });
    }

    private IActionResult VeDanhSachPhieu()
    {
        TempData["Loi"] = ChuaCoCsdl;
        return RedirectToAction("Index", "PhieuKiem");
    }
}
