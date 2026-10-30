using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>
/// Phiếu kiểm (việc T3-4): danh sách, tạo phiếu và khai báo người tham gia kiểm. Tạo phiếu là bắt đầu kiểm luôn
/// (không có bước Nháp/Duyệt riêng ở việc này): khóa ô liên quan qua ILockService và chụp tồn hệ thống vào ChiTiet
/// ngay lúc tạo. Nhập kết quả đếm, đối chiếu lệch, hoàn tất phiếu và sinh phiếu Thừa/Thiếu do kiểm kê là việc của
/// bước sau, KHÔNG làm ở đây.
/// Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo (Bảng 2.2: duyệt phát sinh kiểm kê chỉ Quản lý kho, nhưng
/// việc này chưa có hành động duyệt phát sinh nên chỉ cần quyền Thêm).
/// </summary>
[Area("HangHoa")]
[CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Xem)]
public class PhieuKiemController(INguoiDungHienTai hienTai, IAuditService audit, ILockService khoa, KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu kiểm.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Xem ----

    public async Task<IActionResult> Index() =>
        View(await Db.PhieuKiem.Include(p => p.Kho).Include(p => p.ChiTiet).Include(p => p.ThamGia)
            .OrderByDescending(p => p.MaPK).Take(200).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Xem(int id)
    {
        var p = await Db.PhieuKiem.Include(x => x.Kho).Include(x => x.ChiTiet).Include(x => x.ThamGia).ThenInclude(t => t.NguoiDung)
            .FirstOrDefaultAsync(x => x.MaPK == id);
        return p is null ? NotFound() : await HienForm(p);
    }

    [HttpGet, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao() => HienForm(new PhieuKiem { ThamGia = { new ThamGiaKiem() } });

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.KK, QuyenThaoTac.Them)]
    public async Task<IActionResult> Tao(PhieuKiem p)
    {
        p.MaPK = 0;
        var loi = p.KiemTra() ?? await KiemTraDanhMuc(p) ?? await TaoPhieu(p);
        if (loi is not null)
        {
            ViewData["Loi"] = loi;
            if (p.ThamGia.Count == 0) p.ThamGia.Add(new ThamGiaKiem());   // form luôn có ít nhất một dòng
            return await HienForm(p);
        }
        TempData["ThongBao"] = "Đã tạo phiếu kiểm và bắt đầu kiểm. Các ô liên quan đã bị khóa.";
        return RedirectToAction(nameof(Xem), new { id = p.MaPK });
    }

    // ---- Phần xử lý bên trong ----

    /// <summary>Kho phải còn hoạt động; người tham gia phải là nhân viên có thật và còn hoạt động; phạm vi (nếu có) phải là mặt hàng có thật.</summary>
    private async Task<string?> KiemTraDanhMuc(PhieuKiem p)
    {
        if (!await Db.Kho.AnyAsync(k => k.MaKho == p.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong))
            return "Kho kiểm kê không tồn tại hoặc đã ngừng hoạt động.";

        var maND = p.ThamGia.Select(t => t.MaND).Distinct().ToList();
        var soNguoiHopLe = await Db.NguoiDung.CountAsync(n => maND.Contains(n.MaND) && n.TrangThai == TrangThaiNguoiDung.HoatDong);
        if (soNguoiHopLe != maND.Count) return "Người tham gia kiểm phải là nhân viên có thật và còn hoạt động.";

        if (p.LoaiKiemKe == PhieuKiem.MotPhan)
        {
            var soMatHangHopLe = await Db.BienThe.CountAsync(b => p.PhamViMaBT.Contains(b.MaBT));
            if (soMatHangHopLe != p.PhamViMaBT.Distinct().Count()) return "Có mặt hàng trong phạm vi kiểm không tồn tại.";
        }
        return null;
    }

    /// <summary>
    /// Xác định phạm vi (lô tại từng ô lưu trữ, loại khu Tiếp nhận vì hàng ở đó chưa kiểm/xếp), khóa các ô đó,
    /// rồi ghi phiếu kèm tồn hệ thống chụp ngay lúc này. Trả về lỗi, hoặc null nếu thành công (p.MaPK được điền).
    /// </summary>
    private async Task<string?> TaoPhieu(PhieuKiem p)
    {
        var pham = await (
            from t in Db.TonKho
            join l in Db.LoHang on t.MaLo equals l.MaLo
            join v in Db.ViTri on t.MaViTri equals v.MaViTri
            where v.MaKho == p.MaKho && v.LoaiViTri != LoaiViTriGiaTri.TiepNhan
            select new { l.MaBT, t.MaLo, t.MaViTri, t.SoLuongTon }
        ).ToListAsync();
        if (p.LoaiKiemKe == PhieuKiem.MotPhan)
            pham = pham.Where(x => p.PhamViMaBT.Contains(x.MaBT)).ToList();
        if (pham.Count == 0) return "Không có tồn kho nào trong phạm vi đã chọn để kiểm.";

        // Khóa từng ô liên quan; nếu một ô khóa không được thì mở lại các ô đã khóa trong lượt này rồi báo lỗi (không khóa nửa chừng).
        var oCanKhoa = pham.Select(x => x.MaViTri).Distinct().ToList();
        var daKhoa = new List<int>();
        foreach (var maViTri in oCanKhoa)
        {
            var kq = await khoa.KhoaO(maViTri, MaNguoiDung());
            if (!kq.ThanhCong)
            {
                foreach (var cu in daKhoa) await khoa.MoO(cu, MaNguoiDung());
                return kq.ThongBao;
            }
            daKhoa.Add(maViTri);
        }

        var thamChieu = string.IsNullOrWhiteSpace(p.ThamChieu) ? null : p.ThamChieu.Trim();
        var dienGiai = string.IsNullOrWhiteSpace(p.DienGiai) ? null : p.DienGiai.Trim();
        var thamGia = p.ThamGia.Select(t => new ThamGiaKiem { MaND = t.MaND, GhiChu = string.IsNullOrWhiteSpace(t.GhiChu) ? null : t.GhiChu.Trim() }).ToList();

        var moi = new PhieuKiem
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành KK + mã ngay bên dưới
            MaKho = p.MaKho, LoaiKiemKe = p.LoaiKiemKe, ThamChieu = thamChieu, DienGiai = dienGiai,
            ThoiGianBatDau = DateTime.UtcNow, MaNguoiLap = MaNguoiDung(), TrangThai = PhieuKiem.DangKiem,
            ChiTiet = pham.Select(x => new ChiTietPhieuKiem { MaBT = x.MaBT, MaLo = x.MaLo, MaViTri = x.MaViTri, SoLuongHeThong = x.SoLuongTon }).ToList(),
            ThamGia = thamGia,
        };
        await using var tx = await Db.Database.BeginTransactionAsync();
        Db.PhieuKiem.Add(moi);
        await Db.SaveChangesAsync();                 // lấy được MaPK
        moi.SoPhieu = $"KK{moi.MaPK:D6}";
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        p.MaPK = moi.MaPK;
        await audit.Ghi(MaNguoiDung(), "Tạo phiếu kiểm", LoaiChungTu.KK, moi.MaPK, moi.SoPhieu);
        return null;
    }

    /// <summary>Hiện form kèm danh sách để chọn.</summary>
    private async Task<IActionResult> HienForm(PhieuKiem p)
    {
        ViewBag.Kho = await Db.Kho.Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || k.MaKho == p.MaKho)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho);
        ViewBag.BienThe = await Db.BienThe.Join(Db.SanPham, b => b.MaSP, s => s.MaSP,
                (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .OrderBy(x => x.Ten).ToDictionaryAsync(x => x.MaBT, x => x.Ten);
        ViewBag.NguoiDung = await Db.NguoiDung.Where(n => n.TrangThai == TrangThaiNguoiDung.HoatDong)
            .OrderBy(n => n.HoTen).ToDictionaryAsync(n => n.MaND, n => n.HoTen);
        return View("Form", p);
    }
}
