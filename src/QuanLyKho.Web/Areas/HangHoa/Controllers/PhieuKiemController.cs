using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>
/// Phiếu kiểm (việc T3-4): danh sách, tạo phiếu và khai báo người tham gia kiểm. Tạo phiếu là bắt đầu kiểm luôn; toàn bộ nghiệp vụ
/// (khóa ô trước, chụp số hệ thống sau, mở lại ô khi lỗi) nằm ở <see cref="PhieuKiemService"/>, controller chỉ nhận form và hiển thị.
/// Kiểm đếm, ghi nhận và duyệt phát sinh, hoàn tất phiếu là việc của các bước sau, KHÔNG làm ở đây.
/// Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo.
/// </summary>
[Area("HangHoa")]
[CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Xem)]
public class PhieuKiemController(
    INguoiDungHienTai hienTai, IAuditService audit, ILockService khoa, IStockService ton,
    ILogger<PhieuKiemService> log, KhoDbContext? db = null) : Controller
{
    private const string ChuaCoCsdl = "Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu kiểm. Danh sách đang để trống.";

    private PhieuKiemService DichVu(KhoDbContext csdl) => new(new EfPhieuKiemStore(csdl), khoa, ton, audit, log);

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Xem ----

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Chạy không CSDL (demo giao diện): hiện danh sách rỗng kèm lời nhắc thay vì lỗi 500.
        if (db is null)
        {
            ViewData["ChuaCoCsdl"] = ChuaCoCsdl;
            return View(Array.Empty<PhieuKiemDong>());
        }
        return View(await DichVu(db).DanhSach(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Xem(int id, CancellationToken ct)
    {
        if (db is null) return VeDanhSachViChuaCoCsdl();
        var p = await DichVu(db).Xem(id, ct);
        return p is null ? NotFound() : View(p);
    }

    // ---- Tạo ----

    [HttpGet, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Them)]
    public async Task<IActionResult> Tao(CancellationToken ct)
    {
        if (db is null) return VeDanhSachViChuaCoCsdl();
        return await HienForm(DichVu(db), new TaoPhieuKiemVm { ThamGia = new() { new ThamGiaVm() } }, ct);
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Them)]
    public async Task<IActionResult> Tao(TaoPhieuKiemVm vm, CancellationToken ct)
    {
        if (db is null) return VeDanhSachViChuaCoCsdl();
        var dichVu = DichVu(db);
        var kq = await dichVu.Tao(vm, MaNguoiDung(), ct);
        if (!kq.ThanhCong)
        {
            ViewData["Loi"] = kq.ThongBao;
            return await HienForm(dichVu, vm, ct);
        }
        TempData["ThongBao"] = $"Đã tạo phiếu kiểm {kq.GiaTri!.SoPhieu} và bắt đầu kiểm. Các ô thuộc phạm vi đã bị khóa.";
        return RedirectToAction(nameof(Xem), new { id = kq.GiaTri.MaPK });
    }

    // ---- Phần xử lý bên trong ----

    private async Task<IActionResult> HienForm(PhieuKiemService dichVu, TaoPhieuKiemVm vm, CancellationToken ct)
    {
        // Form luôn có ít nhất một dòng người tham gia và không có phần tử null (kể cả khi client gửi sai).
        vm.PhamViMaBT ??= new();
        vm.ThamGia = (vm.ThamGia ?? new()).Where(t => t is not null).ToList();
        if (vm.ThamGia.Count == 0) vm.ThamGia.Add(new ThamGiaVm());
        return View("Tao", new TaoPhieuKiemTrang(vm, await dichVu.LayDanhMucChon(ct)));
    }

    private IActionResult VeDanhSachViChuaCoCsdl()
    {
        TempData["Loi"] = ChuaCoCsdl;
        return RedirectToAction(nameof(Index));
    }
}
