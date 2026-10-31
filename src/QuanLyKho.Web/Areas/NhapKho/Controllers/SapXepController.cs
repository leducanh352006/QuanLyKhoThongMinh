using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>
/// Sắp xếp hàng đạt vào ô (việc T1-4): danh sách phiếu đã kiểm tra, màn hình lập lệnh và quét mã ô, mã lô để xác nhận.
/// Nghiệp vụ nằm ở <see cref="SapXepService"/>; controller chỉ nhận form, gọi service và hiện kết quả.
/// Quyền theo bảng PhanQuyen: Xem để vào, Sửa để lập lệnh và xác nhận, Xóa để hủy lệnh.
/// </summary>
[Area("NhapKho")]
[CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Xem)]
public class SapXepController(INguoiDungHienTai hienTai, IStockService ton, ICapacityService sucChua, IAuditService audit, IGoiYService goiY, KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được sắp xếp vào ô.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private SapXepService Sx => new(Db, ton, sucChua, audit);

    // ---- Xem ----

    /// <summary>Các phiếu Đã kiểm tra, hàng đạt đang chờ xếp vào ô.</summary>
    public async Task<IActionResult> Index()
    {
        var phieu = await Db.PhieuNhap.AsNoTracking().Include(p => p.Kho).Include(p => p.ChiTiet)
            .Where(p => p.TrangThai == PhieuNhap.DaKiemTra).OrderBy(p => p.MaPN).Take(200).ToListAsync();
        ViewBag.Lenh = await LenhTheoDong(phieu);
        return View(phieu);
    }

    /// <summary>Màn hình sắp xếp một phiếu: từng dòng hàng đạt, các lệnh đã lập, lập lệnh mới, quét mã để xác nhận.</summary>
    [HttpGet]
    public async Task<IActionResult> Xep(int id)
    {
        var p = await Db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == id);
        if (p is null) return NotFound();
        if (p.TrangThai is not (PhieuNhap.DaKiemTra or PhieuNhap.HoanTat))
        {
            TempData["Loi"] = "Chỉ sắp xếp hàng của phiếu đã kiểm tra. Hãy kiểm tra hàng nhập trước.";
            return RedirectToAction("Sua", "PhieuNhap", new { area = "NhapKho", id });
        }

        var dong = p.ChiTiet.OrderBy(d => d.MaCTN).ToList();
        var maBT = dong.Select(d => d.MaBT).Distinct().ToList();
        var maLo = dong.Where(d => d.MaLo != null).Select(d => d.MaLo!.Value).ToList();
        ViewBag.Hang = await Db.BienThe.AsNoTracking().Where(b => maBT.Contains(b.MaBT))
            .Join(Db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " " + b.ThuocTinh })
            .ToDictionaryAsync(x => x.MaBT, x => x.Ten);
        ViewBag.Lo = await Db.LoHang.AsNoTracking().Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => l.MaVachLo);
        ViewBag.Lenh = await LenhTheoDong(new[] { p });
        ViewBag.O = await Db.ViTri.AsNoTracking().Where(v => v.MaKho == p.MaKho && v.CapViTri == CapViTriGiaTri.O).OrderBy(v => v.MaViTri).ToListAsync();

        // Ô gợi ý của G2 (đang chờ duyệt) cho từng biến thể của phiếu; sau khi duyệt, hệ thống lập lệnh và lệnh hiện ở bảng bên dưới.
        var goiYG2 = await goiY.DanhSachChoDuyet(LoaiGoiY.G2);
        ViewBag.GoiY = goiYG2.Where(g => g.ThamChieu.MaPN == p.MaPN).SelectMany(g => g.ChiTiet).Where(c => c.MaViTri != null)
            .GroupBy(c => c.MaBT).ToDictionary(g => g.Key, g => g.First());
        return View(p);
    }

    // ---- Ghi ----

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> LapLenh(int id, int maCTN, int maViTri, int soLuong, bool tuChia)
    {
        var kq = await Sx.LapLenh(maCTN, maViTri, soLuong, tuChia, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Xep), new { id });
    }

    /// <summary>Thủ kho quét mã ô và mã lô. Quét sai thì báo lỗi và không ghi gì.</summary>
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> XacNhan(int id, int maSX, string? maO, string? maLo)
    {
        var kq = await Sx.XacNhan(maSX, maO, maLo, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Xep), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Xoa)]
    public async Task<IActionResult> Huy(int id, int maSX)
    {
        var kq = await Sx.Huy(maSX, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Xep), new { id });
    }

    /// <summary>Các lệnh của mọi dòng thuộc các phiếu đã cho, gom theo dòng phiếu nhập (MaCTN).</summary>
    private async Task<ILookup<int, SapXep>> LenhTheoDong(IEnumerable<PhieuNhap> phieu)
    {
        var ma = phieu.SelectMany(p => p.ChiTiet).Select(d => d.MaCTN).ToList();
        return (await Db.SapXep.AsNoTracking().Where(s => ma.Contains(s.MaCTN)).OrderBy(s => s.MaSX).ToListAsync()).ToLookup(s => s.MaCTN);
    }
}
