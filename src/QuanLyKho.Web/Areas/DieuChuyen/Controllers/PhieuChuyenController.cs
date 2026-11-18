using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Controllers;

/// <summary>Một nguồn hàng có thể chuyển: (lô, ô) ở một kho còn tồn khả dụng.</summary>
public sealed record NguonHang(int MaLo, int MaViTri, int MaKho, string Ten, int KhaDung, string MaVachLo, string MaVachO);

/// <summary>Một ô lưu trữ có thể nhận hàng.</summary>
public sealed record ONhan(int MaViTri, int MaKho, string Ten);

/// <summary>Dữ liệu hiển thị một dòng của phiếu (tên đọc được thay cho mã số).</summary>
public sealed record DongHienThi(
    int MaCTC, string TenHang, string MaVachBT, string MaVachLo, string? HanDung, string TenOXuat, string MaVachOXuat,
    string TenONhan, string MaVachONhan, int SoLuong, int? SLNhan, string? LyDoLech);

/// <summary>
/// Phiếu chuyển kho (việc T5-2 đến T5-6): danh sách, lập và sửa phiếu nháp, duyệt (giữ chỗ), hủy, quét mã xuất ở kho xuất và quét mã
/// nhận ở kho nhập. Nghiệp vụ nằm ở <see cref="PhieuChuyenService"/>. Quyền theo bảng PhanQuyen (chứng từ CK): Xem để vào, Thêm để lập,
/// Sửa để sửa phiếu nháp và quét xuất/nhận, Duyệt để duyệt, Xóa để hủy.
/// </summary>
[Area("DieuChuyen")]
[CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Xem)]
public class PhieuChuyenController(
    INguoiDungHienTai hienTai, IAuditService audit, IStockService ton, ICapacityService sucChua, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu chuyển.");

    private PhieuChuyenService Dv => new(Db, ton, sucChua, audit);

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Danh sách (T5-6) ----

    public async Task<IActionResult> Index(string? q, DateOnly? tuNgay, DateOnly? denNgay, string? trangThai)
    {
        var truy = Db.PhieuChuyen.AsNoTracking().Include(p => p.KhoXuat).Include(p => p.KhoNhap).Include(p => p.ChiTiet).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) truy = truy.Where(p => p.SoPhieu.Contains(q.Trim()));
        if (tuNgay is DateOnly tu) truy = truy.Where(p => p.ThoiGian >= DauNgayUtc(tu));
        if (denNgay is DateOnly den) truy = truy.Where(p => p.ThoiGian <= DauNgayUtc(den.AddDays(1)).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(trangThai) && PhieuChuyen.TatCaTrangThai.Contains(trangThai)) truy = truy.Where(p => p.TrangThai == trangThai);

        ViewBag.Q = q;
        ViewBag.TuNgay = tuNgay;
        ViewBag.DenNgay = denNgay;
        ViewBag.TrangThai = trangThai;
        ViewBag.NguoiVanChuyen = await Db.NguoiDung.AsNoTracking().ToDictionaryAsync(n => n.MaND, n => n.HoTen);
        // Hàng đang trên đường: đã xuất khỏi kho xuất nhưng chưa nhận (không thuộc tồn của kho nào).
        ViewBag.DangTrenDuong = await (from d in Db.ChiTietChuyen
                                       join p in Db.PhieuChuyen on d.MaPC equals p.MaPC
                                       where p.TrangThai == PhieuChuyen.DaXuat && d.SLNhan == null
                                       select (int?)d.SoLuong).SumAsync() ?? 0;
        ViewBag.SoPhieuTrenDuong = await Db.PhieuChuyen.CountAsync(p => p.TrangThai == PhieuChuyen.DaXuat);
        return View(await truy.OrderByDescending(p => p.MaPC).Take(200).ToListAsync());
    }

    private static DateTime DauNgayUtc(DateOnly ngay) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), TimeZoneInfo.Local);

    // ---- Lập, sửa (T5-2) ----

    [HttpGet, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao(int? khoNhap) => HienForm(new PhieuChuyen { MaKhoNhap = khoNhap ?? 0, ChiTiet = { new ChiTietChuyen() } });

    [HttpGet]
    public async Task<IActionResult> Sua(int id)
    {
        var p = await Db.PhieuChuyen.AsNoTracking().Include(x => x.ChiTiet).Include(x => x.KhoXuat).Include(x => x.KhoNhap)
            .FirstOrDefaultAsync(x => x.MaPC == id);
        return p is null ? NotFound() : await HienForm(p);
    }

    // Ép MaPC = 0: người chỉ có quyền Thêm không được POST MaPC của phiếu có sẵn để sửa phiếu đó.
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao(PhieuChuyen p)
    {
        p.MaPC = 0;
        return Luu(p);
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Sua)]
    public Task<IActionResult> Sua(PhieuChuyen p) => p.MaPC > 0 ? Luu(p) : Task.FromResult<IActionResult>(NotFound());

    private async Task<IActionResult> Luu(PhieuChuyen p)
    {
        var kq = await Dv.Luu(p, MaNguoiDung());
        if (!kq.ThanhCong)
        {
            ViewData["Loi"] = kq.ThongBao;
            if (p.ChiTiet.Count == 0) p.ChiTiet.Add(new ChiTietChuyen());
            return await HienForm(p);
        }
        TempData["ThongBao"] = kq.ThongBao;
        return RedirectToAction(nameof(Sua), new { id = kq.GiaTri });
    }

    // ---- Duyệt (T5-3), hủy (T5-6) ----

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Duyet)]
    public async Task<IActionResult> Duyet(int id) => Ve(await Dv.Duyet(id, MaNguoiDung()), nameof(Sua), id);

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Xoa)]
    public async Task<IActionResult> Huy(int id, string? quayLai) =>
        Ve(await Dv.Huy(id, MaNguoiDung()), quayLai == "ds" ? nameof(Index) : nameof(Sua), quayLai == "ds" ? null : id);

    // ---- Xuất khỏi kho xuất (T5-4) ----

    [HttpGet]
    public async Task<IActionResult> Xuat(int id)
    {
        var p = await Db.PhieuChuyen.AsNoTracking().Include(x => x.KhoXuat).Include(x => x.KhoNhap).FirstOrDefaultAsync(x => x.MaPC == id);
        if (p is null) return NotFound();
        if (p.TrangThai != PhieuChuyen.DaDuyet)
        {
            TempData["Loi"] = $"Phiếu đang {p.TrangThai}: chỉ xuất hàng cho phiếu Đã duyệt.";
            return RedirectToAction(nameof(Sua), new { id });
        }
        return View((p, await LayDong(id)));
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> Xuat(int id, List<int> maCTC, List<string?> maVachO, List<string?> maVachLo, List<string?> maVachBT)
    {
        var quet = maCTC.Select((ma, i) => new MaQuetDong(ma, maVachO.ElementAtOrDefault(i), maVachLo.ElementAtOrDefault(i), maVachBT.ElementAtOrDefault(i))).ToList();
        var kq = await Dv.Xuat(id, quet, MaNguoiDung());
        return Ve(kq, kq.ThanhCong ? nameof(Sua) : nameof(Xuat), id);
    }

    // ---- Nhận tại kho nhập (T5-5) ----

    [HttpGet]
    public async Task<IActionResult> Nhan(int id)
    {
        var p = await Db.PhieuChuyen.AsNoTracking().Include(x => x.KhoXuat).Include(x => x.KhoNhap).FirstOrDefaultAsync(x => x.MaPC == id);
        if (p is null) return NotFound();
        if (p.TrangThai is not (PhieuChuyen.DaXuat or PhieuChuyen.DaNhan))
        {
            TempData["Loi"] = $"Phiếu đang {p.TrangThai}: chỉ nhận hàng cho phiếu Đã xuất.";
            return RedirectToAction(nameof(Sua), new { id });
        }
        return View((p, await LayDong(id)));
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.CK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> NhanDong(int id, int maCTC, string? maVachO, string? maVachLo, int slNhan, string? lyDoLech) =>
        Ve(await Dv.Nhan(maCTC, maVachO, maVachLo, slNhan, lyDoLech, MaNguoiDung()), nameof(Nhan), id);

    // ---- Dùng chung ----

    private IActionResult Ve(KetQua kq, string action, int? id)
    {
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao ?? (kq.ThanhCong ? "Đã xử lý." : "Không xử lý được.");
        return id is int ma ? RedirectToAction(action, new { id = ma }) : RedirectToAction(action);
    }

    private async Task<List<DongHienThi>> LayDong(int maPC) =>
        (await (from d in Db.ChiTietChuyen.AsNoTracking()
                join l in Db.LoHang.AsNoTracking() on d.MaLo equals l.MaLo
                join b in Db.BienThe.AsNoTracking() on l.MaBT equals b.MaBT
                join s in Db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
                join ox in Db.ViTri.AsNoTracking() on d.MaViTriXuat equals ox.MaViTri
                join vn in Db.ViTri.AsNoTracking() on d.MaViTriNhap equals vn.MaViTri
                where d.MaPC == maPC
                orderby d.MaCTC
                select new { d, l, b, s, ox, vn }).ToListAsync())
        .Select(x => new DongHienThi(
            x.d.MaCTC, $"{x.s.TenSP} ({x.b.ThuocTinh})", x.b.MaVach, x.l.MaVachLo, x.l.HanDung?.ToString("dd/MM/yyyy"),
            x.ox.TenViTri, x.ox.MaVachO ?? "", x.vn.TenViTri, x.vn.MaVachO ?? "", x.d.SoLuong, x.d.SLNhan, x.d.LyDoLech))
        .ToList();

    /// <summary>Form dùng cho lập, sửa phiếu nháp và xem phiếu đã khóa; nạp các danh sách để chọn.</summary>
    private async Task<IActionResult> HienForm(PhieuChuyen p)
    {
        ViewBag.Kho = await Db.Kho.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || k.MaKho == p.MaKhoXuat || k.MaKho == p.MaKhoNhap)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho);
        ViewBag.NguoiDung = await Db.NguoiDung.AsNoTracking().OrderBy(n => n.HoTen).ToDictionaryAsync(n => n.MaND, n => n.HoTen);
        ViewBag.Tuyen = await Db.TuyenVanChuyen.AsNoTracking()
            .ToDictionaryAsync(t => $"{t.MaKhoXuat}-{t.MaKhoNhap}", t => new[] { t.DonGiaVC, t.PhiChuyen });

        if (!p.DaKhoa)
        {
            // Nguồn hàng: mọi (lô, ô lưu trữ) còn tồn khả dụng; dòng đang có trong phiếu luôn giữ lại để không mất lựa chọn đã lưu.
            var dangCo = p.ChiTiet.Select(d => (d.MaLo, d.MaViTriXuat)).ToHashSet();
            var tho = await (from t in Db.TonKho.AsNoTracking()
                             join v in Db.ViTri.AsNoTracking() on t.MaViTri equals v.MaViTri
                             join l in Db.LoHang.AsNoTracking() on t.MaLo equals l.MaLo
                             join b in Db.BienThe.AsNoTracking() on l.MaBT equals b.MaBT
                             join s in Db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
                             where v.LoaiViTri == LoaiViTriGiaTri.LuuTru && v.TrangThai == TrangThaiHoatDong.HoatDong
                             select new { t.MaLo, t.MaViTri, v.MaKho, KhaDung = t.SoLuongTon - t.SoLuongGiu, s.TenSP, b.ThuocTinh, l.MaVachLo, l.HanDung, v.TenViTri, v.MaVachO })
                .ToListAsync();
            ViewBag.Nguon = tho.Where(x => x.KhaDung > 0 || dangCo.Contains((x.MaLo, x.MaViTri)))
                .Select(x => new NguonHang(x.MaLo, x.MaViTri, x.MaKho,
                    $"{x.TenSP} ({x.ThuocTinh}) · lô {x.MaVachLo}{(x.HanDung is DateOnly h ? $" hạn {h:dd/MM/yyyy}" : "")} · {x.TenViTri} · còn {x.KhaDung}",
                    x.KhaDung, x.MaVachLo, x.MaVachO ?? ""))
                .OrderBy(x => x.Ten).ToList();
            ViewBag.ONhan = await Db.ViTri.AsNoTracking()
                .Where(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa)
                .OrderBy(v => v.TenViTri).Select(v => new ONhan(v.MaViTri, v.MaKho, v.TenViTri)).ToListAsync();
        }
        else
        {
            ViewBag.Dong = await LayDong(p.MaPC);
        }
        return View("Form", p);
    }
}
