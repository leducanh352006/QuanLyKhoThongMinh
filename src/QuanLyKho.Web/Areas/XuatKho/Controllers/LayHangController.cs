using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>Một dòng cần lấy, đủ dữ liệu để hiện trên màn hình quét mã (điện thoại) và kiểm phía trình duyệt trước khi gửi lên.</summary>
public sealed record DongLayHang(
    int MaLH, string TenHang, string MaVachBT, string MaVachLo, string? HanDung, string TenO, string MaVachO,
    int SLGiu, int? SLThucLay, string? LyDoLech);

/// <summary>
/// Lấy hàng theo gợi ý đã duyệt, quét mã xác nhận (việc T2-4, issue #20): danh sách phiếu Đã duyệt/Đang lấy hàng, màn hình quét
/// mã cho từng phiếu. Nghiệp vụ nằm ở <see cref="LayHangService"/>; controller chỉ nhận form, gọi service và hiện kết quả.
/// Quyền theo bảng PhanQuyen: Xem để vào, Sửa để quét mã xác nhận (Thủ kho có quyền Sửa trên XK theo ma trận mặc định).
/// </summary>
[Area("XuatKho")]
[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
public class LayHangController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được lấy hàng.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    private LayHangService Dv => new(Db, audit);

    /// <summary>Các phiếu Đã duyệt hoặc Đang lấy hàng, còn dòng chưa xác nhận.</summary>
    public async Task<IActionResult> Index()
    {
        var phieu = await Db.PhieuXuat.AsNoTracking().Include(p => p.Kho)
            .Where(p => p.TrangThai == PhieuXuat.DaDuyet || p.TrangThai == PhieuXuat.DangLayHang)
            .OrderBy(p => p.MaPX).Take(200).ToListAsync();

        var maPX = phieu.Select(p => p.MaPX).ToList();
        ViewBag.TongDong = await (
            from l in Db.LayHang.AsNoTracking()
            join c in Db.ChiTietPhieuXuat.AsNoTracking() on l.MaCTX equals c.MaCTX
            where maPX.Contains(c.MaPX)
            group l by c.MaPX into g
            select new { MaPX = g.Key, Tong = g.Count(), ConLai = g.Count(x => x.SLThucLay == null) }
        ).ToDictionaryAsync(x => x.MaPX, x => (x.Tong, x.ConLai));

        return View(phieu);
    }

    /// <summary>Màn hình quét mã cho một phiếu: các dòng chưa xác nhận trước (theo thứ tự đi), đã xác nhận sau.</summary>
    [HttpGet]
    public async Task<IActionResult> Lay(int id)
    {
        var p = await Db.PhieuXuat.AsNoTracking().Include(x => x.Kho).FirstOrDefaultAsync(x => x.MaPX == id);
        if (p is null) return NotFound();
        if (p.TrangThai is not (PhieuXuat.DaDuyet or PhieuXuat.DangLayHang))
        {
            TempData["Loi"] = "Chỉ lấy hàng của phiếu Đã duyệt hoặc Đang lấy hàng.";
            return RedirectToAction("Sua", "PhieuXuat", new { area = "XuatKho", id });
        }

        var tho = await (
            from l in Db.LayHang.AsNoTracking()
            join c in Db.ChiTietPhieuXuat.AsNoTracking() on l.MaCTX equals c.MaCTX
            join b in Db.BienThe.AsNoTracking() on c.MaBT equals b.MaBT
            join s in Db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
            join lo in Db.LoHang.AsNoTracking() on l.MaLo equals lo.MaLo
            join v in Db.ViTri.AsNoTracking() on l.MaViTri equals v.MaViTri
            where c.MaPX == id
            select new { l, b, s, lo, v }
        ).ToListAsync();

        var duongDi = await DuongDiAsync(p.MaKho, tho.Select(x => x.v.MaViTri).Distinct().ToList());
        var dong = tho
            .Select(x => (Dong: new DongLayHang(
                x.l.MaLH, x.s.TenSP + " (" + x.b.ThuocTinh + ")", x.b.MaVach, x.lo.MaVachLo,
                x.lo.HanDung?.ToString("dd/MM/yyyy"), x.v.TenViTri, x.v.MaVachO ?? "",
                x.l.SLGiu, x.l.SLThucLay, x.l.LyDoLech), DuongDi: duongDi.GetValueOrDefault(x.v.MaViTri, "")))
            .OrderBy(x => x.Dong.SLThucLay != null)   // chưa xác nhận hiện trước
            .ThenBy(x => x.DuongDi)
            .Select(x => x.Dong)
            .ToList();

        ViewData["Title"] = $"Lấy hàng phiếu {p.SoPhieu}";
        return View((p, dong));
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> XacNhan(int id, int maLH, string? maVachO, string? maVachLo, string? maVachBT, int slThucLay, string? lyDoLech)
    {
        var kq = await Dv.XacNhan(maLH, maVachO, maVachLo, maVachBT, slThucLay, lyDoLech, MaNguoiDung());
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Lay), new { id });
    }

    /// <summary>Đường dẫn vị trí (tên các cấp cha nối tới từng ô, trong cùng kho) để sắp "thứ tự đi" — giống GoiYG1Service.</summary>
    private async Task<Dictionary<int, string>> DuongDiAsync(int maKho, List<int> oCanDuongDan)
    {
        var tatCa = await Db.ViTri.AsNoTracking().Where(v => v.MaKho == maKho).ToListAsync();
        var theoMa = tatCa.ToDictionary(v => v.MaViTri);
        string DuongDan(ViTri v) => v.MaViTriCha is int cha && theoMa.TryGetValue(cha, out var c) ? DuongDan(c) + "/" + v.TenViTri : v.TenViTri;
        return oCanDuongDan.Where(theoMa.ContainsKey).ToDictionary(ma => ma, ma => DuongDan(theoMa[ma]));
    }
}
