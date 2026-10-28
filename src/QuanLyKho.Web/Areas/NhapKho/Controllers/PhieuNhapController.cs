using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>
/// Phiếu nhập (việc T1-1): danh sách, tạo, sửa phiếu nháp, xác nhận. Toàn bộ xử lý nằm trong file này cho dễ đọc.
/// Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo, Sửa để sửa và xác nhận.
/// </summary>
[Area("NhapKho")]
[CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Xem)]
public class PhieuNhapController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu nhập.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Xem ----

    public async Task<IActionResult> Index() =>
        View(await Db.PhieuNhap.Include(p => p.Kho).Include(p => p.NhaCungCap).Include(p => p.ChiTiet)
            .OrderByDescending(p => p.MaPN).Take(200).ToListAsync());

    [HttpGet, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao() => HienForm(new PhieuNhap { ChiTiet = { new ChiTietPhieuNhap() } });

    [HttpGet]
    public async Task<IActionResult> Sua(int id)
    {
        var p = await Db.PhieuNhap.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == id);
        return p is null ? NotFound() : await HienForm(p);
    }

    // ---- Lưu (tạo mới và sửa dùng chung một form, phân biệt bằng MaPN: 0 là phiếu mới) ----

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao(PhieuNhap p) => Luu(p);

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Sua)]
    public Task<IActionResult> Sua(PhieuNhap p) => Luu(p);

    /// <summary>Nháp thành Đã xác nhận. Một lệnh có điều kiện nên bấm hai lần cũng chỉ ghi một lần.</summary>
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> XacNhan(int id)
    {
        var n = await Db.PhieuNhap.Where(p => p.MaPN == id && p.TrangThai == PhieuNhap.Nhap && p.MaNCC != null && p.ChiTiet.Any())
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TrangThai, PhieuNhap.DaXacNhan));
        if (n == 1)
        {
            await audit.Ghi(MaNguoiDung(), "Xác nhận phiếu nhập", LoaiChungTu.NK, id);
            TempData["ThongBao"] = "Đã xác nhận phiếu. Phiếu bị khóa, không sửa được nữa.";
        }
        else TempData["Loi"] = "Không xác nhận được: phiếu không còn ở trạng thái Nháp, hoặc chưa có nhà cung cấp hoặc dòng hàng.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    // ---- Phần xử lý bên trong ----

    private async Task<IActionResult> Luu(PhieuNhap p)
    {
        var loi = p.KiemTra() ?? await KiemTraDanhMuc(p) ?? await Ghi(p);
        if (loi is not null)
        {
            ViewData["Loi"] = loi;
            if (p.ChiTiet.Count == 0) p.ChiTiet.Add(new ChiTietPhieuNhap());   // form luôn có ít nhất một dòng
            return await HienForm(p);
        }
        TempData["ThongBao"] = "Đã lưu phiếu nháp.";
        return RedirectToAction(nameof(Sua), new { id = p.MaPN });
    }

    /// <summary>Kho, nhà cung cấp phải còn hoạt động; mặt hàng phải có thật.</summary>
    private async Task<string?> KiemTraDanhMuc(PhieuNhap p)
    {
        if (!await Db.Kho.AnyAsync(k => k.MaKho == p.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong))
            return "Kho nhập không tồn tại hoặc đã ngừng hoạt động.";
        if (!await Db.NhaCungCap.AnyAsync(n => n.MaNCC == p.MaNCC && n.TrangThai == TrangThaiNhaCungCap.HoatDong))
            return "Nhà cung cấp không tồn tại hoặc đã ngừng dùng.";
        var ma = p.ChiTiet.Select(d => d.MaBT).Distinct().ToList();
        return await Db.BienThe.CountAsync(b => ma.Contains(b.MaBT)) == ma.Count ? null : "Có mặt hàng không tồn tại.";
    }

    /// <summary>Ghi vào CSDL. Trả về lỗi, hoặc null nếu thành công (phiếu mới thì p.MaPN được điền).</summary>
    private async Task<string?> Ghi(PhieuNhap p)
    {
        // Chỉ lấy các trường được phép từ form, không tin trạng thái, người lập... do trình duyệt gửi lên.
        var dong = p.ChiTiet.Select(d => new ChiTietPhieuNhap { MaBT = d.MaBT, SLDuKien = d.SLDuKien }).ToList();
        var dienGiai = string.IsNullOrWhiteSpace(p.DienGiai) ? null : p.DienGiai.Trim();

        if (p.MaPN == 0)
        {
            var moi = new PhieuNhap
            {
                SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành NK + mã ngay bên dưới
                LoaiPhieu = p.LoaiPhieu, ThoiGian = DateTime.UtcNow, MaKho = p.MaKho, MaNCC = p.MaNCC,
                MaNguoiLap = MaNguoiDung(), DienGiai = dienGiai, ChiTiet = dong,
            };
            await using var tx = await Db.Database.BeginTransactionAsync();
            Db.PhieuNhap.Add(moi);
            await Db.SaveChangesAsync();                 // lấy được MaPN
            moi.SoPhieu = $"NK{moi.MaPN:D6}";
            await Db.SaveChangesAsync();
            await tx.CommitAsync();
            p.MaPN = moi.MaPN;
            await audit.Ghi(MaNguoiDung(), "Tạo phiếu nhập", LoaiChungTu.NK, moi.MaPN, moi.SoPhieu);
            return null;
        }

        var cu = await Db.PhieuNhap.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == p.MaPN);
        if (cu is null) return "Không tìm thấy phiếu.";
        if (cu.DaKhoa) return "Phiếu đã xác nhận nên bị khóa, không sửa được.";
        cu.LoaiPhieu = p.LoaiPhieu;
        cu.MaKho = p.MaKho;
        cu.MaNCC = p.MaNCC;
        cu.DienGiai = dienGiai;
        Db.ChiTietPhieuNhap.RemoveRange(cu.ChiTiet);   // xóa dòng cũ, thay bằng dòng mới từ form
        cu.ChiTiet = dong;
        await Db.SaveChangesAsync();
        await audit.Ghi(MaNguoiDung(), "Sửa phiếu nhập", LoaiChungTu.NK, cu.MaPN, cu.SoPhieu);
        return null;
    }

    /// <summary>Hiện form kèm danh sách để chọn. Kho và nhà cung cấp ngừng dùng chỉ hiện nếu chính phiếu này đang dùng.</summary>
    private async Task<IActionResult> HienForm(PhieuNhap p)
    {
        ViewBag.Kho = await Db.Kho.Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || k.MaKho == p.MaKho)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho);
        ViewBag.NhaCungCap = await Db.NhaCungCap.Where(n => n.TrangThai == TrangThaiNhaCungCap.HoatDong || n.MaNCC == p.MaNCC)
            .OrderBy(n => n.TenNCC).ToDictionaryAsync(n => n.MaNCC, n => n.TenNCC);
        ViewBag.BienThe = await Db.BienThe.Join(Db.SanPham, b => b.MaSP, s => s.MaSP,
                (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ") - " + b.MaVach })
            .OrderBy(x => x.Ten).ToDictionaryAsync(x => x.MaBT, x => x.Ten);
        return View("Form", p);
    }
}
