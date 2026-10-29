using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Controllers;

/// <summary>
/// Phiếu xuất (việc T2-1): danh sách, tạo, sửa phiếu nháp, và một bước duyệt tối giản chỉ để khóa phiếu (đủ cho tiêu chí hoàn thành
/// của việc này). Gợi ý G1 (T2-2) và duyệt kèm giữ chỗ tồn (T2-3) sẽ MỞ RỘNG đúng hành động Duyệt bên dưới, không tạo hành động mới.
/// Trả lại hàng mua và hủy hàng (việc T2-7, Bảng 2.4 dòng 7) dùng chung Tao/Sua/Duyet ở trên.
/// Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo, Sửa để sửa, Duyệt để duyệt.
/// </summary>
[Area("XuatKho")]
[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
public class PhieuXuatController(INguoiDungHienTai hienTai, IAuditService audit, KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu xuất.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Xem ----

    public async Task<IActionResult> Index() =>
        View(await Db.PhieuXuat.Include(p => p.Kho).Include(p => p.ChiTiet)
            .OrderByDescending(p => p.MaPX).Take(200).ToListAsync());

    [HttpGet, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao() => HienForm(new PhieuXuat { ChiTiet = { new ChiTietPhieuXuat() } });

    [HttpGet]
    public async Task<IActionResult> Sua(int id)
    {
        var p = await Db.PhieuXuat.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == id);
        return p is null ? NotFound() : await HienForm(p);
    }

    // ---- Lưu (tạo mới và sửa dùng chung một form, phân biệt bằng MaPX: 0 là phiếu mới) ----

    // Ép MaPX = 0 (bỏ qua giá trị người dùng gửi lên): nếu không, người chỉ có quyền Thêm có thể POST vào đây
    // với MaPX của một phiếu có sẵn để sửa phiếu đó mà không có quyền Sửa.
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Them)]
    public Task<IActionResult> Tao(PhieuXuat p)
    {
        p.MaPX = 0;
        return Luu(p);
    }

    // Đối xứng: người chỉ có quyền Sửa không được POST MaPX = 0 vào đây để tạo phiếu mới mà không có quyền Thêm.
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Sua)]
    public Task<IActionResult> Sua(PhieuXuat p) => p.MaPX > 0 ? Luu(p) : Task.FromResult<IActionResult>(NotFound());

    /// <summary>
    /// Nháp thành Đã duyệt: khóa phiếu và ghi người duyệt. Một lệnh có điều kiện nên bấm hai lần cũng chỉ ghi một lần.
    /// CHƯA giữ chỗ tồn và chưa gắn gợi ý G1 — việc T2-3 sẽ thêm vào đúng hành động này.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]
    public async Task<IActionResult> Duyet(int id)
    {
        var p = await Db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == id);
        if (p is null) return NotFound();

        var tuChoi = p.TrangThai != PhieuXuat.Nhap ? "Phiếu không còn ở trạng thái Nháp."
            : p.KiemTra() ?? await KiemTraDanhMuc(p)
            ?? (QuyTacDuyet.KiemTra(MaNguoiDung(), p.MaNguoiLap, "phiếu xuất") is { ThanhCong: false } kq ? kq.ThongBao : null);
        if (tuChoi is not null)
        {
            TempData["Loi"] = tuChoi;
            return RedirectToAction(nameof(Sua), new { id });
        }

        var n = await Db.PhieuXuat.Where(x => x.MaPX == id && x.TrangThai == PhieuXuat.Nhap)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.DaDuyet).SetProperty(x => x.MaNguoiDuyet, MaNguoiDung()));
        if (n == 1)
        {
            await audit.Ghi(MaNguoiDung(), "Duyệt phiếu xuất", LoaiChungTu.XK, id, p.SoPhieu);
            TempData["ThongBao"] = "Đã duyệt phiếu. Phiếu bị khóa, không sửa được nữa.";
        }
        else TempData["Loi"] = "Không duyệt được: phiếu vừa bị đổi trạng thái ở nơi khác.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    // ---- Phần xử lý bên trong ----

    private async Task<IActionResult> Luu(PhieuXuat p)
    {
        // Trả lại hàng mua lấy hàng không đạt từ khu tiếp nhận (Bảng 2.4 dòng 7): kiểm theo số chưa trả hết của phiếu nhập gốc,
        // không xét tồn khả dụng (hàng ở đó vốn đã bị loại khỏi tồn khả dụng, xem KiemTraTonKhaDung).
        var loi = p.KiemTra() ?? await KiemTraDanhMuc(p)
            ?? (p.LoaiPhieu == PhieuXuat.TraLaiHangMua ? await KiemTraTraHang(p) : await KiemTraTonKhaDung(p))
            ?? await Ghi(p);
        if (loi is not null)
        {
            ViewData["Loi"] = loi;
            if (p.ChiTiet.Count == 0) p.ChiTiet.Add(new ChiTietPhieuXuat());   // form luôn có ít nhất một dòng
            return await HienForm(p);
        }
        TempData["ThongBao"] = "Đã lưu phiếu nháp.";
        return RedirectToAction(nameof(Sua), new { id = p.MaPX });
    }

    /// <summary>Kho phải còn hoạt động; mặt hàng phải có thật; phiếu trả lại hàng mua phải tham chiếu phiếu nhập có thật.</summary>
    private async Task<string?> KiemTraDanhMuc(PhieuXuat p)
    {
        if (!await Db.Kho.AnyAsync(k => k.MaKho == p.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong))
            return "Kho xuất không tồn tại hoặc đã ngừng hoạt động.";
        if (p.MaPNGoc is int maPNGoc && !await Db.PhieuNhap.AnyAsync(n => n.MaPN == maPNGoc))
            return "Không tìm thấy phiếu nhập gốc.";
        var ma = p.ChiTiet.Select(d => d.MaBT).Distinct().ToList();
        return await Db.BienThe.CountAsync(b => ma.Contains(b.MaBT)) == ma.Count ? null : "Có mặt hàng không tồn tại.";
    }

    /// <summary>
    /// Số lượng mỗi dòng không được vượt tổng tồn khả dụng của biến thể trong kho xuất (Bảng 2.4 dòng 1), gộp tồn mọi lô và mọi ô
    /// LƯU TRỮ (không tính ô Tiếp nhận: hàng chưa kiểm, chưa xếp thì chưa khả dụng — IStockService cũng từ chối giữ chỗ, xuất từ đó).
    /// Áp dụng cho loại Khác và Hủy hàng; phiếu Trả lại hàng mua dùng <see cref="KiemTraTraHang"/> thay vì hàm này (Bảng 2.4 dòng 7).
    /// </summary>
    private async Task<string?> KiemTraTonKhaDung(PhieuXuat p)
    {
        var gop = p.ChiTiet.GroupBy(d => d.MaBT).Select(g => new { MaBT = g.Key, SoLuong = g.Sum(d => d.SoLuong) }).ToList();
        var ma = gop.Select(g => g.MaBT).ToList();
        var khaDung = await (
            from t in Db.TonKho
            join l in Db.LoHang on t.MaLo equals l.MaLo
            join v in Db.ViTri on t.MaViTri equals v.MaViTri
            where v.MaKho == p.MaKho && v.LoaiViTri != LoaiViTriGiaTri.TiepNhan && ma.Contains(l.MaBT)
            group t by l.MaBT into g
            select new { MaBT = g.Key, KhaDung = g.Sum(x => x.SoLuongTon - x.SoLuongGiu) }
        ).ToDictionaryAsync(x => x.MaBT, x => x.KhaDung);

        foreach (var d in gop)
            if (d.SoLuong > khaDung.GetValueOrDefault(d.MaBT))
                return $"{await TenBienThe(d.MaBT)}: vượt tồn khả dụng trong kho (còn {khaDung.GetValueOrDefault(d.MaBT)}).";
        return null;
    }

    /// <summary>
    /// Trả lại hàng mua (việc T2-7, Bảng 2.4 dòng 7): lấy từ hàng không đạt (SLThucNhan - SLDat) còn lại của từng dòng biến thể trên
    /// phiếu nhập gốc, trừ đi phần đã trả ở các phiếu trả khác (chưa hủy) tham chiếu cùng phiếu nhập đó. Không xét tồn khả dụng vì
    /// hàng không đạt nằm ở khu tiếp nhận, vốn đã bị loại khỏi tồn khả dụng (xem <see cref="KiemTraTonKhaDung"/>).
    /// </summary>
    private async Task<string?> KiemTraTraHang(PhieuXuat p)
    {
        var maPNGoc = p.MaPNGoc!.Value;   // bắt buộc có giá trị với loại Trả lại hàng mua, đã kiểm ở PhieuXuat.KiemTra()
        var gop = p.ChiTiet.GroupBy(d => d.MaBT).Select(g => new { MaBT = g.Key, SoLuong = g.Sum(d => d.SoLuong) }).ToList();

        var khongDat = await Db.ChiTietPhieuNhap.Where(d => d.MaPN == maPNGoc)
            .GroupBy(d => d.MaBT)
            .Select(g => new { MaBT = g.Key, SoLuong = g.Sum(d => (d.SLThucNhan ?? 0) - (d.SLDat ?? 0)) })
            .ToDictionaryAsync(x => x.MaBT, x => x.SoLuong);

        var daTra = await Db.ChiTietPhieuXuat.Join(Db.PhieuXuat, d => d.MaPX, x => x.MaPX, (d, x) => new { d.MaBT, d.SoLuong, x.MaPX, x.MaPNGoc, x.LoaiPhieu, x.TrangThai })
            .Where(x => x.MaPX != p.MaPX && x.MaPNGoc == maPNGoc && x.LoaiPhieu == PhieuXuat.TraLaiHangMua && x.TrangThai != PhieuXuat.DaHuy)
            .GroupBy(x => x.MaBT)
            .Select(g => new { MaBT = g.Key, SoLuong = g.Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.MaBT, x => x.SoLuong);

        foreach (var d in gop)
        {
            var conLai = khongDat.GetValueOrDefault(d.MaBT) - daTra.GetValueOrDefault(d.MaBT);
            if (d.SoLuong > conLai)
                return $"{await TenBienThe(d.MaBT)}: vượt số không đạt còn lại của phiếu nhập gốc (còn trả được {Math.Max(0, conLai)}).";
        }
        return null;
    }

    private async Task<string> TenBienThe(int maBT)
        => await Db.BienThe.Where(b => b.MaBT == maBT).Join(Db.SanPham, b => b.MaSP, s => s.MaSP,
            (b, s) => s.TenSP + " (" + b.ThuocTinh + ")").FirstOrDefaultAsync() ?? $"biến thể #{maBT}";

    /// <summary>Ghi vào CSDL. Trả về lỗi, hoặc null nếu thành công (phiếu mới thì p.MaPX được điền).</summary>
    private async Task<string?> Ghi(PhieuXuat p)
    {
        // Chỉ lấy các trường được phép từ form, không tin trạng thái, người lập... do trình duyệt gửi lên.
        var dong = p.ChiTiet.Select(d => new ChiTietPhieuXuat { MaBT = d.MaBT, SoLuong = d.SoLuong }).ToList();
        var doiTuong = string.IsNullOrWhiteSpace(p.DoiTuong) ? null : p.DoiTuong.Trim();
        var thamChieu = string.IsNullOrWhiteSpace(p.ThamChieu) ? null : p.ThamChieu.Trim();
        var lyDo = string.IsNullOrWhiteSpace(p.LyDo) ? null : p.LyDo.Trim();
        var dienGiai = string.IsNullOrWhiteSpace(p.DienGiai) ? null : p.DienGiai.Trim();
        var maPNGoc = p.LoaiPhieu == PhieuXuat.TraLaiHangMua ? p.MaPNGoc : null;

        if (p.MaPX == 0)
        {
            var moi = new PhieuXuat
            {
                SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành XK + mã ngay bên dưới
                LoaiPhieu = p.LoaiPhieu, ThoiGian = DateTime.UtcNow, MaKho = p.MaKho, DoiTuong = doiTuong, ThamChieu = thamChieu,
                MaPNGoc = maPNGoc, MaNguoiLap = MaNguoiDung(), LyDo = lyDo, DienGiai = dienGiai, ChiTiet = dong,
            };
            await using var tx = await Db.Database.BeginTransactionAsync();
            Db.PhieuXuat.Add(moi);
            await Db.SaveChangesAsync();                 // lấy được MaPX
            moi.SoPhieu = $"XK{moi.MaPX:D6}";
            await Db.SaveChangesAsync();
            await tx.CommitAsync();
            p.MaPX = moi.MaPX;
            await audit.Ghi(MaNguoiDung(), "Tạo phiếu xuất", LoaiChungTu.XK, moi.MaPX, moi.SoPhieu);
            return null;
        }

        var cu = await Db.PhieuXuat.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == p.MaPX);
        if (cu is null) return "Không tìm thấy phiếu.";
        if (cu.DaKhoa) return "Phiếu đã duyệt nên bị khóa, không sửa được.";
        cu.LoaiPhieu = p.LoaiPhieu;
        cu.MaKho = p.MaKho;
        cu.DoiTuong = doiTuong;
        cu.ThamChieu = thamChieu;
        cu.MaPNGoc = maPNGoc;
        cu.LyDo = lyDo;
        cu.DienGiai = dienGiai;
        Db.ChiTietPhieuXuat.RemoveRange(cu.ChiTiet);   // xóa dòng cũ, thay bằng dòng mới từ form
        cu.ChiTiet = dong;
        await Db.SaveChangesAsync();
        await audit.Ghi(MaNguoiDung(), "Sửa phiếu xuất", LoaiChungTu.XK, cu.MaPX, cu.SoPhieu);
        return null;
    }

    /// <summary>Hiện form kèm danh sách để chọn. Kho ngừng dùng chỉ hiện nếu chính phiếu này đang dùng.</summary>
    private async Task<IActionResult> HienForm(PhieuXuat p)
    {
        ViewBag.Kho = await Db.Kho.Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || k.MaKho == p.MaKho)
            .OrderBy(k => k.TenKho).ToDictionaryAsync(k => k.MaKho, k => k.TenKho);
        ViewBag.BienThe = await Db.BienThe.Join(Db.SanPham, b => b.MaSP, s => s.MaSP,
                (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ") - " + b.MaVach })
            .OrderBy(x => x.Ten).ToDictionaryAsync(x => x.MaBT, x => x.Ten);
        ViewBag.PhieuNhap = await Db.PhieuNhap.Where(n => n.TrangThai != PhieuNhap.Nhap)
            .OrderByDescending(n => n.MaPN).Take(200).ToDictionaryAsync(n => n.MaPN, n => n.SoPhieu);
        return View("Form", p);
    }
}
