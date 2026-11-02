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
/// Phiếu xuất (việc T2-1): danh sách, tạo, sửa phiếu nháp, Duyệt (giữ chỗ tồn — việc T2-8, phân bổ FIFO tối giản vì chưa có gợi ý G1)
/// và Hủy (trả chỗ — việc T2-8). Gợi ý G1 (T2-2) sẽ THAY phần phân bổ FIFO trong Duyet bằng gợi ý AI, không tạo hành động mới;
/// AI lỗi hoặc không có dữ liệu thì FIFO vẫn chạy được (mục 1.2.4: làm tay được toàn bộ quy trình).
/// Trả lại hàng mua và hủy hàng (việc T2-7, Bảng 2.4 dòng 7) dùng chung Tao/Sua/Duyet/Huy ở trên.
/// Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo, Sửa để sửa, Duyệt để duyệt, Xóa để hủy.
/// </summary>
[Area("XuatKho")]
[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
public class PhieuXuatController(INguoiDungHienTai hienTai, IAuditService audit, IStockService ton, KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa dùng được phiếu xuất.");

    private int MaNguoiDung() => hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập.");

    // ---- Xem ----

    /// <summary>Danh sách tìm theo số phiếu (chứa, không phân biệt hoa thường) và lọc theo khoảng thời gian lập phiếu (việc T2-8).</summary>
    public async Task<IActionResult> Index(string? q, DateOnly? tuNgay, DateOnly? denNgay)
    {
        var truy = Db.PhieuXuat.Include(p => p.Kho).Include(p => p.ChiTiet).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) truy = truy.Where(p => p.SoPhieu.Contains(q.Trim()));
        if (tuNgay is DateOnly tu) truy = truy.Where(p => p.ThoiGian >= DauNgayUtc(tu));
        if (denNgay is DateOnly den) truy = truy.Where(p => p.ThoiGian <= DauNgayUtc(den.AddDays(1)).AddTicks(-1));

        ViewBag.Q = q;
        ViewBag.TuNgay = tuNgay;
        ViewBag.DenNgay = denNgay;
        return View(await truy.OrderByDescending(p => p.MaPX).Take(200).ToListAsync());
    }

    /// <summary>Đổi ngày theo giờ máy chủ sang đầu ngày giờ UTC để lọc ThoiGian (lưu UTC).</summary>
    private static DateTime DauNgayUtc(DateOnly ngay) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), TimeZoneInfo.Local);

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
    /// Nháp thành Đã duyệt: phân bổ lô/ô FIFO cho từng dòng, giữ chỗ qua IStockService.GiuCho, ghi bảng LayHang rồi khóa phiếu.
    /// Phân bổ và duyệt cùng một giao dịch: giữ chỗ không đủ (tranh chấp đồng thời) thì hoàn tác hết, phiếu vẫn ở Nháp.
    /// Việc T2-2 (gợi ý G1) sẽ thay <see cref="PhanBoFifo"/> bằng gợi ý AI; FIFO ở đây là phần "làm tay" khi chưa có hoặc AI lỗi (mục 1.2.4).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]
    public async Task<IActionResult> Duyet(int id)
    {
        var p = await Db.PhieuXuat.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == id);
        if (p is null) return NotFound();

        var tuChoi = p.TrangThai != PhieuXuat.Nhap ? "Phiếu không còn ở trạng thái Nháp."
            : p.KiemTra() ?? await KiemTraDanhMuc(p)
            ?? (QuyTacDuyet.KiemTra(MaNguoiDung(), p.MaNguoiLap, "phiếu xuất") is { ThanhCong: false } kq ? kq.ThongBao : null);
        if (tuChoi is not null)
        {
            TempData["Loi"] = tuChoi;
            return RedirectToAction(nameof(Sua), new { id });
        }

        // Trả lại hàng mua lấy từ khu tiếp nhận: không qua G1/FIFO (Bảng 2.4 dòng 7), lô là chính lô của phiếu nhập gốc, ô là ô Tiếp nhận của kho.
        var (phan, loiPhanBo) = p.LoaiPhieu == PhieuXuat.TraLaiHangMua ? await PhanBoTraHang(p) : await PhanBoFifo(p);
        if (loiPhanBo is not null)
        {
            TempData["Loi"] = loiPhanBo;
            return RedirectToAction(nameof(Sua), new { id });
        }

        await using var giaoDich = await Db.Database.BeginTransactionAsync();
        var n = await Db.PhieuXuat.Where(x => x.MaPX == id && x.TrangThai == PhieuXuat.Nhap)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.DaDuyet).SetProperty(x => x.MaNguoiDuyet, MaNguoiDung()));
        if (n != 1)
        {
            TempData["Loi"] = "Không duyệt được: phiếu vừa bị đổi trạng thái ở nơi khác.";
            return RedirectToAction(nameof(Sua), new { id });
        }

        foreach (var (maCTX, maLo, maViTri, soLuong) in phan)
        {
            var kqGiu = await ton.GiuCho(new DongTon(maLo, maViTri, soLuong));
            if (!kqGiu.ThanhCong)
            {
                TempData["Loi"] = kqGiu.ThongBao ?? "Không giữ chỗ được tồn kho.";
                return RedirectToAction(nameof(Sua), new { id });   // thoát không commit giaoDich: hoàn tác cả đổi trạng thái lẫn GiuCho đã chạy
            }
            Db.LayHang.Add(new LayHang { MaCTX = maCTX, MaLo = maLo, MaViTri = maViTri, SLGiu = soLuong });
        }
        await Db.SaveChangesAsync();
        await giaoDich.CommitAsync();

        await audit.Ghi(MaNguoiDung(), "Duyệt phiếu xuất", LoaiChungTu.XK, id, p.SoPhieu);
        TempData["ThongBao"] = "Đã duyệt phiếu và giữ chỗ tồn. Phiếu bị khóa, không sửa được nữa.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    /// <summary>
    /// Hủy phiếu Nháp hoặc Đã duyệt (việc T2-8, Bảng 2.4): trả chỗ qua IStockService.TraCho cho từng dòng LayHang đang giữ (nếu có,
    /// tức phiếu đã Đã duyệt) rồi xóa các dòng đó, sau đó khóa phiếu. Phiếu Thiếu do kiểm kê (IXuatKhoService.TaoPhieuXuatThieu)
    /// đã ở trạng thái Hoàn tất, không hủy được bằng hành động này.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xoa)]
    public async Task<IActionResult> Huy(int id)
    {
        var p = await Db.PhieuXuat.AsNoTracking().FirstOrDefaultAsync(x => x.MaPX == id);
        if (p is null) return NotFound();
        if (p.LoaiPhieu == PhieuXuat.ThieuDoKiemKe)
        {
            TempData["Loi"] = "Phiếu Thiếu do kiểm kê do hệ thống tự sinh, không hủy được ở đây.";
            return RedirectToAction(nameof(Sua), new { id });
        }

        await using var giaoDich = await Db.Database.BeginTransactionAsync();
        var n = await Db.PhieuXuat.Where(x => x.MaPX == id && (x.TrangThai == PhieuXuat.Nhap || x.TrangThai == PhieuXuat.DaDuyet))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.DaHuy));
        if (n != 1)
        {
            TempData["Loi"] = "Không hủy được: phiếu không còn ở trạng thái Nháp hoặc Đã duyệt.";
            return RedirectToAction(nameof(Sua), new { id });
        }

        var giu = await (from l in Db.LayHang join c in Db.ChiTietPhieuXuat on l.MaCTX equals c.MaCTX where c.MaPX == id select l).ToListAsync();
        foreach (var g in giu)
        {
            var kqTra = await ton.TraCho(new DongTon(g.MaLo, g.MaViTri, g.SLGiu));
            if (!kqTra.ThanhCong)
            {
                TempData["Loi"] = kqTra.ThongBao ?? "Không trả được chỗ đã giữ.";
                return RedirectToAction(nameof(Sua), new { id });
            }
        }
        if (giu.Count > 0) { Db.LayHang.RemoveRange(giu); await Db.SaveChangesAsync(); }
        await giaoDich.CommitAsync();

        await audit.Ghi(MaNguoiDung(), "Hủy phiếu xuất", LoaiChungTu.XK, id, p.SoPhieu);
        TempData["ThongBao"] = giu.Count > 0 ? "Đã hủy phiếu và trả lại tồn đã giữ chỗ." : "Đã hủy phiếu.";
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

    /// <summary>
    /// Phân bổ lô/ô khi duyệt cho loại Khác, Hủy hàng (việc T2-8): FIFO tối giản trên ô LƯU TRỮ của kho, lô hết hạn sớm trước,
    /// không có hạn thì lô nhỏ trước (xấp xỉ "nhập trước", đúng thứ tự ưu tiên của gợi ý G1 ở Bảng 1.4). Việc T2-2 sẽ thay hàm này
    /// bằng gợi ý G1 có AI; đây là phần "làm tay" khi AI lỗi hoặc chưa có (mục 1.2.4).
    /// </summary>
    private async Task<(List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> Phan, string? Loi)> PhanBoFifo(PhieuXuat p)
    {
        var phan = new List<(int, int, int, int)>();
        var daDung = new Dictionary<(int MaLo, int MaViTri), int>();

        foreach (var d in p.ChiTiet)
        {
            var canLay = d.SoLuong;
            var ungVien = await (
                from t in Db.TonKho
                join l in Db.LoHang on t.MaLo equals l.MaLo
                join v in Db.ViTri on t.MaViTri equals v.MaViTri
                where v.MaKho == p.MaKho && v.LoaiViTri != LoaiViTriGiaTri.TiepNhan && l.MaBT == d.MaBT
                orderby l.HanDung ?? DateOnly.MaxValue, l.MaLo
                select new { l.MaLo, t.MaViTri, KhaDung = t.SoLuongTon - t.SoLuongGiu }
            ).ToListAsync();

            foreach (var u in ungVien)
            {
                if (canLay <= 0) break;
                var daTru = daDung.GetValueOrDefault((u.MaLo, u.MaViTri));
                var conLai = u.KhaDung - daTru;
                if (conLai <= 0) continue;
                var lay = Math.Min(canLay, conLai);
                phan.Add((d.MaCTX, u.MaLo, u.MaViTri, lay));
                daDung[(u.MaLo, u.MaViTri)] = daTru + lay;
                canLay -= lay;
            }
            if (canLay > 0) return (phan, $"{await TenBienThe(d.MaBT)}: không đủ tồn khả dụng để giữ chỗ (còn thiếu {canLay}).");
        }
        return (phan, null);
    }

    /// <summary>
    /// Phân bổ lô/ô khi duyệt cho loại Trả lại hàng mua (việc T2-8, Bảng 2.4 dòng 7): lấy đúng (các) lô của phiếu nhập gốc tại ô
    /// Tiếp nhận của kho xuất, không qua FIFO/G1 (hàng không đạt không lẫn với hàng bán được nên không áp dụng cách chọn thông thường).
    /// </summary>
    private async Task<(List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> Phan, string? Loi)> PhanBoTraHang(PhieuXuat p)
    {
        var phan = new List<(int, int, int, int)>();
        var oTiepNhan = await Db.ViTri
            .Where(v => v.MaKho == p.MaKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan
                        && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa)
            .OrderBy(v => v.MaViTri).Select(v => (int?)v.MaViTri).FirstOrDefaultAsync();
        if (oTiepNhan is null) return (phan, "Kho xuất chưa có ô Tiếp nhận đang hoạt động (và không bị khóa).");

        var loTheoBienThe = await Db.ChiTietPhieuNhap.Where(d => d.MaPN == p.MaPNGoc && d.MaLo != null)
            .GroupBy(d => d.MaBT).ToDictionaryAsync(g => g.Key, g => g.Select(d => d.MaLo!.Value).Distinct().ToList());

        var daDung = new Dictionary<(int MaLo, int MaViTri), int>();
        foreach (var d in p.ChiTiet)
        {
            var canLay = d.SoLuong;
            if (!loTheoBienThe.TryGetValue(d.MaBT, out var cacLo))
                return (phan, $"{await TenBienThe(d.MaBT)}: không có lô nào của phiếu nhập gốc.");

            var ton = await Db.TonKho.Where(t => cacLo.Contains(t.MaLo) && t.MaViTri == oTiepNhan.Value)
                .ToDictionaryAsync(t => t.MaLo, t => t.SoLuongTon - t.SoLuongGiu);
            foreach (var maLo in cacLo)
            {
                if (canLay <= 0) break;
                var daTru = daDung.GetValueOrDefault((maLo, oTiepNhan.Value));
                var conLai = ton.GetValueOrDefault(maLo) - daTru;
                if (conLai <= 0) continue;
                var lay = Math.Min(canLay, conLai);
                phan.Add((d.MaCTX, maLo, oTiepNhan.Value, lay));
                daDung[(maLo, oTiepNhan.Value)] = daTru + lay;
                canLay -= lay;
            }
            if (canLay > 0) return (phan, $"{await TenBienThe(d.MaBT)}: không đủ hàng không đạt ở khu tiếp nhận để giữ chỗ (còn thiếu {canLay}).");
        }
        return (phan, null);
    }

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
