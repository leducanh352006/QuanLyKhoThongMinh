using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>
/// Phiếu nhập (việc T1-1), in tem (T1-6) và kiểm tra hàng nhập (T1-2): danh sách, tạo, sửa phiếu nháp, xác nhận, in tem, tiếp nhận hàng.
/// Toàn bộ xử lý nằm trong file này cho dễ đọc. Quyền theo bảng PhanQuyen: Xem để vào, Thêm để tạo, Sửa để sửa, xác nhận và kiểm tra hàng.
/// </summary>
[Area("NhapKho")]
[CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Xem)]
public class PhieuNhapController(INguoiDungHienTai hienTai, IAuditService audit, IHangHoaService hangHoa, IStockService ton, KhoDbContext? db = null) : Controller
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

    /// <summary>
    /// In tem mã (việc T1-6): mỗi dòng của phiếu một tem gồm mã vạch biến thể (BienThe.MaVach) và mã lô. Chỉ in cho phiếu đã xác nhận,
    /// vì phiếu nháp còn đổi dòng hàng nên mã lô (đánh theo số thứ tự dòng) chưa cố định. <paramref name="soTem"/> là số bản in mỗi dòng.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InTem(int id, int soTem = 1)
    {
        var p = await Db.PhieuNhap.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == id);
        if (p is null) return NotFound();
        if (!p.DaKhoa)
        {
            TempData["Loi"] = "Chỉ in tem cho phiếu đã xác nhận. Hãy xác nhận phiếu trước.";
            return RedirectToAction(nameof(Sua), new { id });
        }
        var ma = p.ChiTiet.Select(d => d.MaBT).Distinct().ToList();
        var hang = await Db.BienThe.Where(b => ma.Contains(b.MaBT))
            .Join(Db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, s.TenSP, b.ThuocTinh, b.MaVach })
            .ToDictionaryAsync(x => x.MaBT);
        ViewBag.Phieu = p;
        ViewBag.SoTem = Math.Clamp(soTem, 1, 50);
        return View(p.ChiTiet.OrderBy(d => d.MaCTN).Select((d, i) => new TemIn(
            hang[d.MaBT].TenSP, hang[d.MaBT].ThuocTinh, hang[d.MaBT].MaVach, PhieuNhap.MaLo(p.SoPhieu, i + 1), d.SLDuKien)).ToList());
    }

    // ---- Kiểm tra hàng nhập (việc T1-2) ----

    /// <summary>Màn hình tiếp nhận: phiếu Đã xác nhận thì nhập kết quả, phiếu đã kiểm tra thì chỉ xem.</summary>
    [HttpGet]
    public async Task<IActionResult> KiemTra(int id)
    {
        var p = await Db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == id);
        if (p is null) return NotFound();
        if (p.TrangThai == PhieuNhap.Nhap)
        {
            TempData["Loi"] = "Phiếu còn ở trạng thái Nháp. Hãy xác nhận phiếu trước khi kiểm tra hàng.";
            return RedirectToAction(nameof(Sua), new { id });
        }
        return await HienKiemTra(p, null);
    }

    [HttpPost, ValidateAntiForgeryToken, CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Sua)]
    public async Task<IActionResult> KiemTra(int id, List<DongTiepNhan> dong)
    {
        var p = await Db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == id);
        if (p is null) return NotFound();
        var loi = p.TrangThai == PhieuNhap.DaXacNhan ? await TiepNhan(p, dong) : "Chỉ kiểm tra được phiếu ở trạng thái Đã xác nhận.";
        if (loi is not null)
        {
            ViewData["Loi"] = loi;
            return await HienKiemTra(p, dong);   // giữ nguyên những gì thủ kho đã nhập
        }
        TempData["ThongBao"] = "Đã ghi kết quả kiểm tra. Hàng nằm ở khu tiếp nhận, chưa tính vào tồn khả dụng cho tới khi sắp xếp vào ô.";
        return RedirectToAction(nameof(KiemTra), new { id });
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

    /// <summary>
    /// Tiếp nhận hàng của một phiếu Đã xác nhận. Kiểm tra mã quét và số liệu trước, rồi làm tất cả trong MỘT giao dịch:
    /// chuyển phiếu sang Đã kiểm tra (lệnh có điều kiện nên bấm hai lần chỉ ghi một lần), tạo lô, ghi tăng tồn vào ô Tiếp nhận
    /// (cả hàng đạt và không đạt; hàng đạt chờ sắp xếp ở việc T1-4), lưu kết quả từng dòng. Lỗi ở đâu thì hoàn tác hết.
    /// Trả về lỗi, hoặc null nếu thành công.
    /// </summary>
    private async Task<string?> TiepNhan(PhieuNhap p, List<DongTiepNhan> dong)
    {
        var dongPhieu = p.ChiTiet.OrderBy(d => d.MaCTN).ToList();
        if (dong.Count != dongPhieu.Count || dong.Select(x => x.MaCTN).Distinct().Count() != dongPhieu.Count)
            return "Dữ liệu không khớp với các dòng của phiếu. Hãy mở lại màn hình kiểm tra.";
        var hang = await HangCuaPhieu(p);

        // 1. Kiểm tra từng dòng: số liệu, mã quét đúng biến thể và đúng lô, hạn dùng.
        var viec = new List<(ChiTietPhieuNhap Dong, DongTiepNhan Nhap, string MaLo, string Ten)>();
        for (var i = 0; i < dongPhieu.Count; i++)
        {
            var d = dongPhieu[i];
            var nhap = dong.FirstOrDefault(x => x.MaCTN == d.MaCTN);
            if (nhap is null) return "Dữ liệu không khớp với các dòng của phiếu. Hãy mở lại màn hình kiểm tra.";
            var h = hang[d.MaBT];
            var ten = $"Dòng {i + 1} ({h.TenSP})";
            var maLo = PhieuNhap.MaLo(p.SoPhieu, i + 1);

            var loi = new ChiTietPhieuNhap { SLDuKien = d.SLDuKien, SLThucNhan = nhap.SLThucNhan, SLDat = nhap.SLDat, LyDoLech = nhap.LyDoLech }.KiemTraKetQua();
            if (loi is not null) return $"{ten}: {loi}";
            if (nhap.SLThucNhan > 0)   // dòng không về (thực nhận 0) không có hàng để quét
            {
                if (!string.Equals(nhap.MaVach?.Trim(), h.MaVach, StringComparison.OrdinalIgnoreCase)) return $"{ten}: quét sai mã vạch biến thể.";
                if (!string.Equals(nhap.MaLo?.Trim(), maLo, StringComparison.OrdinalIgnoreCase)) return $"{ten}: quét sai mã lô.";
                if (h.CoHanDung && nhap.HanDung is null) return $"{ten}: hàng quản lý theo hạn dùng, hãy nhập hạn dùng của lô.";
            }
            viec.Add((d, nhap, maLo, ten));
        }

        var oTiepNhan = await Db.ViTri
            .Where(v => v.MaKho == p.MaKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan
                        && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa)
            .OrderBy(v => v.MaViTri).Select(v => (int?)v.MaViTri).FirstOrDefaultAsync();
        if (oTiepNhan is null) return "Kho nhập chưa có ô Tiếp nhận đang hoạt động (và không bị khóa). Nhờ quản trị viên kiểm tra danh mục vị trí.";

        // 2. Ghi trong một giao dịch; IHangHoaService và IStockService dùng chung KhoDbContext nên tham gia giao dịch này.
        await using var giaoDich = await Db.Database.BeginTransactionAsync();
        var da = await Db.PhieuNhap.Where(x => x.MaPN == p.MaPN && x.TrangThai == PhieuNhap.DaXacNhan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuNhap.DaKiemTra));
        if (da == 0) return "Phiếu vừa được kiểm tra rồi hoặc không còn ở trạng thái Đã xác nhận.";   // thoát không commit là hoàn tác

        foreach (var (d, nhap, maLo, ten) in viec)
        {
            int? maLoDaTao = null;
            if (nhap.SLThucNhan > 0)
            {
                var h = hang[d.MaBT];
                var lo = await hangHoa.TaoLo(d.MaBT, h.CoHanDung ? nhap.HanDung : null, maLo);
                if (!lo.ThanhCong) return $"{ten}: {lo.ThongBao}";
                var tang = await ton.GhiTang(new DongTon(lo.GiaTri!.MaLo, oTiepNhan.Value, nhap.SLThucNhan!.Value),
                    LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, p.MaPN), MaNguoiDung());
                if (!tang.ThanhCong) return $"{ten}: {tang.ThongBao}";
                maLoDaTao = lo.GiaTri.MaLo;
            }
            string? lyDo = string.IsNullOrWhiteSpace(nhap.LyDoLech) ? null : nhap.LyDoLech.Trim();
            await Db.ChiTietPhieuNhap.Where(x => x.MaCTN == d.MaCTN).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.MaLo, maLoDaTao).SetProperty(x => x.SLThucNhan, nhap.SLThucNhan)
                .SetProperty(x => x.SLDat, nhap.SLDat).SetProperty(x => x.LyDoLech, lyDo));
        }
        await giaoDich.CommitAsync();

        await audit.Ghi(MaNguoiDung(), "Kiểm tra hàng nhập", LoaiChungTu.NK, p.MaPN,
            $"{p.SoPhieu}: thực nhận {viec.Sum(v => v.Nhap.SLThucNhan)}, đạt {viec.Sum(v => v.Nhap.SLDat)}");
        return null;
    }

    /// <summary>Tên, mã vạch và cờ hạn dùng của các biến thể trên phiếu, tra theo MaBT.</summary>
    private Task<Dictionary<int, HangKiem>> HangCuaPhieu(PhieuNhap p)
    {
        var ma = p.ChiTiet.Select(d => d.MaBT).Distinct().ToList();
        return Db.BienThe.Where(b => ma.Contains(b.MaBT))
            .Join(Db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new HangKiem(b.MaBT, s.TenSP, b.ThuocTinh, b.MaVach, s.CoHanDung))
            .ToDictionaryAsync(x => x.MaBT);
    }

    private async Task<IActionResult> HienKiemTra(PhieuNhap p, List<DongTiepNhan>? nhap)
    {
        var ma = p.ChiTiet.Where(d => d.MaLo != null).Select(d => d.MaLo!.Value).ToList();
        ViewBag.Hang = await HangCuaPhieu(p);
        ViewBag.Lo = await Db.LoHang.AsNoTracking().Where(l => ma.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo);
        ViewBag.Nhap = (nhap ?? new()).GroupBy(x => x.MaCTN).ToDictionary(g => g.Key, g => g.First());
        return View("KiemTra", p);
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

/// <summary>Nội dung một tem: mặt hàng, mã vạch biến thể, mã lô và số lượng của dòng phiếu.</summary>
public record TemIn(string TenSP, string ThuocTinh, string MaVach, string MaLo, int SoLuong);

/// <summary>Một dòng kết quả thủ kho nhập ở màn hình kiểm tra: mã quét, số đếm, hạn dùng và lý do lệch.</summary>
public class DongTiepNhan
{
    public int MaCTN { get; set; }
    public string? MaVach { get; set; }
    public string? MaLo { get; set; }
    public int? SLThucNhan { get; set; }
    public int? SLDat { get; set; }
    public DateOnly? HanDung { get; set; }
    public string? LyDoLech { get; set; }
}

/// <summary>Thông tin mặt hàng của một dòng phiếu, dùng ở màn hình kiểm tra.</summary>
public record HangKiem(int MaBT, string TenSP, string ThuocTinh, string MaVach, bool CoHanDung);
