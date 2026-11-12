using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>Một dòng kết quả đối chiếu: yêu cầu trên phiếu so với tổng số thực lấy (LayHang.SLThucLay) của dòng đó.</summary>
public sealed record DongDoiChieu(int MaCTX, int MaBT, string TenHang, int YeuCau, int DaLay, string? LyDoLech) { public int Thieu => YeuCau - DaLay; }

/// <summary>
/// Đối chiếu hàng đã lấy (việc T2-5, issue #16, Bảng 2.4 dòng 5, mục 2.4.2): so <c>LayHang.SLThucLay</c> với số lượng trên từng
/// dòng phiếu (<c>ChiTietPhieuXuat.SoLuong</c>) sau khi thủ kho đã quét mã lấy hàng xong (T2-4). Khớp hết thì chuyển bước xác nhận
/// xuất (Đã đối chiếu, việc T2-6). Lệch thì tự động xử lý — không cần quản lý kho duyệt lại — theo thứ tự: giải phóng phần giữ chỗ
/// dư (đã giữ nhưng không lấy được), rồi với từng dòng còn thiếu: gợi ý lại đúng phần thiếu trên tồn khả dụng hiện tại (đủ thì giữ
/// chỗ ngay, quay lại Đang lấy hàng để lấy tiếp); hết tồn thì xuất một phần (chốt số lượng dòng bằng đúng số đã lấy được) — luôn có
/// lối thoát vì tồn kho hữu hạn nên mỗi vòng chỉ xuất một phần hoặc giảm bớt phần còn thiếu, không lặp vô hạn.
/// </summary>
public sealed class DoiChieuService(KhoDbContext db, IStockService ton, IAuditService audit, IGoiYG1Service goiYG1)
{
    public async Task<KetQua<IReadOnlyList<DongDoiChieu>>> Xem(int maPX, CancellationToken ct = default)
    {
        var p = await db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == maPX, ct);
        if (p is null) return KetQua<IReadOnlyList<DongDoiChieu>>.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);

        var theoDong = await TongDaLay(maPX, ct);
        var lyDo = await LyDoLechTheoDong(maPX, ct);
        var ten = await TenCacBienThe(p.ChiTiet.Select(c => c.MaBT).Distinct().ToList(), ct);
        var dong = p.ChiTiet.Select(c => new DongDoiChieu(c.MaCTX, c.MaBT, ten.GetValueOrDefault(c.MaBT, $"biến thể #{c.MaBT}"), c.SoLuong, theoDong.GetValueOrDefault(c.MaCTX), lyDo.GetValueOrDefault(c.MaCTX))).ToList();
        return KetQua<IReadOnlyList<DongDoiChieu>>.Ok(dong);
    }

    /// <summary>
    /// Thực hiện đối chiếu. Trả <see cref="KetQua{T}.GiaTri"/> = true nếu phiếu đã chuyển Đã đối chiếu (khớp hết, hoặc mọi dòng
    /// thiếu đều đã xuất một phần); false nếu còn dòng vừa được gợi ý lại — phiếu vẫn Đang lấy hàng, quay lại màn Lấy hàng.
    /// </summary>
    public async Task<KetQua<bool>> DoiChieu(int maPX, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuXuat.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == maPX, ct);
        if (p is null) return KetQua<bool>.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuXuat.DangLayHang)
            return KetQua<bool>.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, không đối chiếu được.", MaLoiDichVu.SaiTrangThai);

        var layHang = await DongLayHangCuaPhieu(maPX, ct).ToListAsync(ct);
        if (layHang.Any(l => l.SLThucLay is null))
            return KetQua<bool>.Loi("Còn dòng chưa lấy hàng (quét mã); hãy lấy xong rồi mới đối chiếu.", MaLoiDichVu.SaiTrangThai);

        await using var giaoDichMoi = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;

        // Giải phóng phần giữ chỗ dư của những dòng lấy thiếu (đã giữ nhưng không lấy được) — để tồn khả dụng phản ánh đúng trước
        // khi gợi ý lại; lấy đủ thì SLThucLay == SLGiu, không có gì để trả. CHƯA chốt lại các dòng này vội (xem ChotLaiDongDaTraDu)
        // vì GanLaiPhanThieu bên dưới còn cần đọc chúng để loại khỏi ứng viên gợi ý lại.
        var daTraDu = layHang.Where(l => l.SLThucLay!.Value < l.SLGiu).ToList();
        foreach (var l in daTraDu)
        {
            var tra = await ton.TraCho(new DongTon(l.MaLo, l.MaViTri, l.SLGiu - l.SLThucLay!.Value), ct);
            if (!tra.ThanhCong)
            {
                if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
                return KetQua<bool>.Loi(tra.ThongBao ?? "Không trả được phần giữ chỗ dư.", tra.MaLoi);
            }
        }

        var theoDong = layHang.GroupBy(l => l.MaCTX).ToDictionary(g => g.Key, g => g.Sum(l => l.SLThucLay!.Value));
        var thieu = p.ChiTiet.Where(c => theoDong.GetValueOrDefault(c.MaCTX) < c.SoLuong).ToList();

        if (thieu.Count == 0)
        {
            ChotLaiDongDaTraDu(daTraDu);
            await ChuyenDaDoiChieu(maPX, ct);
            await db.SaveChangesAsync(ct);
            if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);
            await audit.Ghi(maND, "Đối chiếu phiếu xuất", LoaiChungTu.XK, maPX, $"{p.SoPhieu}: khớp đủ, chuyển Đã đối chiếu.", ct);
            return KetQua<bool>.Ok(true, "Đã lấy đủ, khớp với phiếu. Chuyển sang bước xác nhận xuất.");
        }

        var baoCao = new List<string>();
        var ganLaiDuoc = false;
        foreach (var d in thieu)
        {
            var conThieu = d.SoLuong - theoDong.GetValueOrDefault(d.MaCTX);
            var ten = await TenCacBienThe(new List<int> { d.MaBT }, ct);
            var tenHang = ten.GetValueOrDefault(d.MaBT, $"biến thể #{d.MaBT}");

            var gioiHan = await GanLaiPhanThieu(p, d, conThieu, ct);
            if (gioiHan is null)
            {
                ganLaiDuoc = true;
                baoCao.Add($"{tenHang}: thiếu {conThieu}, đã gợi ý lại đủ trên tồn khả dụng hiện tại — chờ lấy tiếp.");
            }
            else if (gioiHan == KhongTheXuatMotPhan)
            {
                if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
                return KetQua<bool>.Loi(
                    $"{tenHang}: thiếu {conThieu}, không còn tồn khả dụng để gợi ý lại, và chưa lấy được đơn vị nào nên không xuất một " +
                    "phần được (số lượng dòng phải lớn hơn 0). Hãy hủy phiếu hoặc hủy riêng dòng này.", MaLoiDichVu.DuLieuKhongHopLe);
            }
            else
            {
                d.SoLuong = theoDong.GetValueOrDefault(d.MaCTX);
                baoCao.Add($"{tenHang}: thiếu {conThieu}, {gioiHan} — xuất một phần (chốt còn {d.SoLuong}).");
            }
        }

        // Chốt lại các dòng đã trả phần dư bây giờ (sau khi GanLaiPhanThieu không cần đọc chúng làm ứng viên loại trừ nữa), để
        // nếu phiếu còn quay lại đối chiếu lần nữa (còn dòng vừa gợi ý lại chưa lấy xong) thì không trả nhầm các dòng này lần nữa.
        ChotLaiDongDaTraDu(daTraDu);
        if (!ganLaiDuoc) await ChuyenDaDoiChieu(maPX, ct);
        await db.SaveChangesAsync(ct);
        if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);

        var moTa = string.Join(" ", baoCao);
        await audit.Ghi(maND, "Đối chiếu phiếu xuất", LoaiChungTu.XK, maPX, $"{p.SoPhieu}: {moTa}", ct);
        return ganLaiDuoc
            ? KetQua<bool>.Ok(false, "Lệch so với phiếu. " + moTa + " Quay lại màn Lấy hàng để lấy phần vừa gợi ý.")
            : KetQua<bool>.Ok(true, "Lệch so với phiếu, đã xuất một phần theo số thực lấy. " + moTa + " Chuyển sang bước xác nhận xuất.");
    }

    private const string KhongTheXuatMotPhan = "\0khong-the-xuat-mot-phan";

    /// <summary>
    /// Gợi ý lại đúng phần còn thiếu của một dòng trên tồn khả dụng hiện tại, giữ chỗ ngay nếu đủ (không qua duyệt, vì phiếu đã
    /// được duyệt từ T2-3 — chỉ bổ sung giữ chỗ cho đúng phần thiếu). Loại hẳn các (MaLo, MaViTri) đã có dòng LayHang của chính
    /// dòng này khỏi ứng viên — nếu không, FEFO/FIFO thường chọn lại đúng lô/ô vừa lấy thiếu (khả dụng ở đó vừa được trả lại ở
    /// trên), trong khi lô/ô khác vẫn còn hàng. Trả null nếu gán lại thành công (giữ chỗ xong, chờ lấy tiếp); trả lý do hết tồn
    /// (để xuất một phần) nếu không đủ, hoặc <see cref="KhongTheXuatMotPhan"/> nếu dòng chưa lấy được gì (đã lấy = 0) nên không
    /// thể "xuất một phần" (vi phạm ràng buộc số lượng dòng phải &gt; 0).
    /// </summary>
    private async Task<string?> GanLaiPhanThieu(PhieuXuat p, ChiTietPhieuXuat d, int conThieu, CancellationToken ct)
    {
        var daDung = await db.LayHang.AsNoTracking().Where(x => x.MaCTX == d.MaCTX)
            .Select(x => new ValueTuple<int, int>(x.MaLo, x.MaViTri)).ToListAsync(ct);
        var loaiTru = daDung.Count == 0 ? null : new HashSet<(int MaLo, int MaViTri)>(daDung);

        var aoPhieu = new PhieuXuat { MaKho = p.MaKho, ChiTiet = { new ChiTietPhieuXuat { MaCTX = d.MaCTX, MaBT = d.MaBT, SoLuong = conThieu } } };
        var (phan, loi) = await goiYG1.PhanBo(aoPhieu, ct, loaiTru);

        // Giữ lại những gì đã giữ chỗ/thêm trong vòng lặp dưới để trả lại đầy đủ nếu một phần sau đó lỗi giữa chừng — không được
        // để sót phần giữ chỗ (rò chỗ giữ) khi GanLaiPhanThieu này coi như thất bại.
        var daGiu = new List<(int MaLo, int MaViTri, int SoLuong)>();
        var daThem = new List<LayHang>();
        if (loi is null)
        {
            foreach (var (maCTX, maLo, maViTri, soLuong) in phan)
            {
                var giu = await ton.GiuCho(new DongTon(maLo, maViTri, soLuong), ct);
                if (!giu.ThanhCong) { loi = giu.ThongBao; break; }
                daGiu.Add((maLo, maViTri, soLuong));
                // Phòng hiếm khi có đổi tồn chen giữa làm xuất hiện lại đúng lô/ô vừa loại trừ ở trên — khóa duy nhất
                // (MaCTX, MaLo, MaViTri) không cho ghi trùng; coi như không gán lại được lô/ô đó.
                if (await db.LayHang.AnyAsync(x => x.MaCTX == maCTX && x.MaLo == maLo && x.MaViTri == maViTri, ct))
                {
                    loi = "lô/ô vừa gợi ý lại đã có dòng lấy hàng khác của chính dòng này (hiếm gặp)";
                    break;
                }
                var moi = new LayHang { MaCTX = maCTX, MaLo = maLo, MaViTri = maViTri, SLGiu = soLuong };
                db.LayHang.Add(moi);
                daThem.Add(moi);
            }
        }
        if (loi is null) return null;

        foreach (var (maLo, maViTri, soLuong) in daGiu) await ton.TraCho(new DongTon(maLo, maViTri, soLuong), ct);
        foreach (var m in daThem) db.LayHang.Remove(m);

        return (await TongDaLay(p.MaPX, ct)).GetValueOrDefault(d.MaCTX) <= 0
            ? KhongTheXuatMotPhan
            : "không còn tồn khả dụng để gợi ý lại";
    }

    /// <summary>
    /// Chốt lại các dòng LayHang đã trả phần giữ chỗ dư ở đầu <see cref="DoiChieu"/>, để một lần đối chiếu sau (nếu phiếu còn quay
    /// lại Đang lấy hàng vì có dòng vừa được gợi ý lại) không trả nhầm các dòng này thêm lần nữa — <see cref="StockService"/> không
    /// tự chống gọi trùng cho <c>TraCho</c>/<c>GiuCho</c> (khác <c>GhiTang</c>/<c>GhiGiam</c> có sổ cái để so trùng).
    /// Lấy thiếu một phần (SLThucLay &gt; 0): chốt SLGiu = SLThucLay nên dòng không còn thỏa <c>SLThucLay &lt; SLGiu</c> nữa.
    /// Lấy được 0 (SLThucLay = 0): không thể chốt SLGiu = 0 (ràng buộc CK_LayHang_SLGiu &gt; 0) nên xóa hẳn dòng — đã trả lại toàn
    /// bộ chỗ giữ, không còn gì để theo dõi; lý do lệch vẫn còn trong Nhật ký (đã ghi ở bước Xác nhận lấy hàng, T2-4).
    /// </summary>
    private void ChotLaiDongDaTraDu(List<LayHang> daTraDu)
    {
        foreach (var l in daTraDu)
        {
            if (l.SLThucLay!.Value > 0) l.SLGiu = l.SLThucLay.Value;
            else db.LayHang.Remove(l);
        }
    }

    private async Task ChuyenDaDoiChieu(int maPX, CancellationToken ct) =>
        await db.PhieuXuat.Where(x => x.MaPX == maPX && x.TrangThai == PhieuXuat.DangLayHang)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.DaDoiChieu), ct);

    private IQueryable<LayHang> DongLayHangCuaPhieu(int maPX, CancellationToken ct) =>
        from l in db.LayHang
        join c in db.ChiTietPhieuXuat on l.MaCTX equals c.MaCTX
        where c.MaPX == maPX
        select l;

    private async Task<Dictionary<int, int>> TongDaLay(int maPX, CancellationToken ct) =>
        await DongLayHangCuaPhieu(maPX, ct).AsNoTracking().Where(l => l.SLThucLay != null)
            .GroupBy(l => l.MaCTX).Select(g => new { MaCTX = g.Key, Tong = g.Sum(l => l.SLThucLay!.Value) })
            .ToDictionaryAsync(x => x.MaCTX, x => x.Tong, ct);

    /// <summary>Lý do lệch (LayHang.LyDoLech) gộp theo dòng phiếu, để màn Xem có "biên bản" cho từng dòng bị lệch.</summary>
    private async Task<Dictionary<int, string>> LyDoLechTheoDong(int maPX, CancellationToken ct)
    {
        var lyDo = await DongLayHangCuaPhieu(maPX, ct).AsNoTracking().Where(l => l.LyDoLech != null)
            .Select(l => new { l.MaCTX, l.LyDoLech }).ToListAsync(ct);
        return lyDo.GroupBy(x => x.MaCTX).ToDictionary(g => g.Key, g => string.Join("; ", g.Select(x => x.LyDoLech).Distinct()));
    }

    private async Task<Dictionary<int, string>> TenCacBienThe(List<int> maBT, CancellationToken ct) =>
        await db.BienThe.AsNoTracking().Where(b => maBT.Contains(b.MaBT))
            .Join(db.SanPham.AsNoTracking(), b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .ToDictionaryAsync(x => x.MaBT, x => x.Ten, ct);
}
