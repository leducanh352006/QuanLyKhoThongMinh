using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>
/// Sắp xếp hàng đạt vào ô (việc T1-4, Bảng 2.3 dòng 3 và 4): lập lệnh (kiểm sức chứa, chia vào nhiều ô nếu vượt),
/// thủ kho quét mã ô và mã lô để xác nhận (kiểm lại sức chứa, chuyển hàng từ khu tiếp nhận sang ô bằng IStockService.ChuyenO), hủy lệnh.
/// Mọi thay đổi tồn đi qua IStockService, không ghi thẳng vào TonKho.
/// </summary>
public sealed class SapXepService(KhoDbContext db, IStockService ton, ICapacityService sucChua, IAuditService audit)
{
    // ---- Lập lệnh ----

    /// <summary>
    /// Lập lệnh xếp <paramref name="soLuong"/> hàng đạt của dòng <paramref name="maCTN"/> vào ô <paramref name="maViTri"/>.
    /// Ô không nhận hết và <paramref name="tuChia"/> = true thì phần dư chia sang các ô lưu trữ khác còn chỗ của kho (theo mã ô); không đủ chỗ thì không lập lệnh nào.
    /// </summary>
    public async Task<KetQua<IReadOnlyList<SapXep>>> LapLenh(int maCTN, int maViTri, int soLuong, bool tuChia, int maND, CancellationToken ct = default)
    {
        if (soLuong <= 0) return Loi("Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
        var d = await db.ChiTietPhieuNhap.AsNoTracking().FirstOrDefaultAsync(x => x.MaCTN == maCTN, ct);
        if (d is null) return Loi("Không tìm thấy dòng phiếu nhập.", MaLoiDichVu.KhongTimThay);
        var p = await db.PhieuNhap.AsNoTracking().FirstAsync(x => x.MaPN == d.MaPN, ct);
        if (p.TrangThai != PhieuNhap.DaKiemTra) return Loi($"Chỉ sắp xếp hàng của phiếu Đã kiểm tra, phiếu {p.SoPhieu} đang {p.TrangThai}.", MaLoiDichVu.SaiTrangThai);
        if (d.MaLo is null || (d.SLDat ?? 0) == 0) return Loi("Dòng này không có hàng đạt để sắp xếp.", MaLoiDichVu.SaiTrangThai);

        var daLap = await db.SapXep.AsNoTracking().Where(s => s.MaCTN == maCTN && s.TrangThai != SapXep.DaHuy).ToListAsync(ct);
        var con = d.SLDat!.Value - daLap.Sum(s => s.SoLuong);
        if (soLuong > con) return Loi($"Dòng này chỉ còn {con} hàng đạt chưa lập lệnh xếp (đạt {d.SLDat}).", MaLoiDichVu.DuLieuKhongHopLe);

        var o = await db.ViTri.AsNoTracking().FirstOrDefaultAsync(v => v.MaViTri == maViTri, ct);
        if (o is null) return Loi("Không tìm thấy ô.", MaLoiDichVu.KhongTimThay);
        if (!o.LaO || o.MaKho != p.MaKho || o.LoaiViTri != LoaiViTriGiaTri.LuuTru)
            return Loi($"Hàng đạt chỉ được xếp vào ô Lưu trữ của kho nhập; '{o.TenViTri}' không phải.", MaLoiDichVu.DuLieuKhongHopLe);

        // Ô đã chọn trước; nếu cho chia thì các ô lưu trữ khác còn hoạt động và không khóa theo thứ tự mã ô.
        var uuTien = new List<int> { maViTri };
        if (tuChia)
            uuTien.AddRange(await db.ViTri.AsNoTracking()
                .Where(v => v.MaKho == p.MaKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru
                            && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa && v.MaViTri != maViTri)
                .OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync(ct));

        var ke = new List<(int MaViTri, int SoLuong)>();
        var canXep = soLuong;
        foreach (var ma in uuTien)
        {
            var kq = await sucChua.KiemTra(ma, d.MaBT, canXep, ct);   // đã tính cả hàng đang có và hàng chờ xếp của ô
            // Ô đã chọn bị khóa hoặc không dùng được, hoặc không cho chia mà ô không đủ: báo ngay lý do (thiếu thể tích hay khối lượng).
            if (ma == maViTri && !kq.Du && (!tuChia || kq.MaLoi != MaLoiDichVu.VuotSucChua))
                return Loi(kq.LyDo ?? "Ô không nhận được hàng.", kq.MaLoi ?? MaLoiDichVu.VuotSucChua);
            var duoc = Math.Min(canXep, kq.SoLuongToiDaThem ?? canXep);
            if (duoc <= 0) continue;
            ke.Add((ma, duoc));
            canXep -= duoc;
            if (canXep == 0) break;
        }
        if (canXep > 0) return Loi($"Các ô lưu trữ của kho chỉ còn chỗ cho {soLuong - canXep} trên {soLuong} hàng cần xếp.", MaLoiDichVu.VuotSucChua);

        // StockService.ChuyenO coi hai lần chuyển cùng lô, cùng ô nguồn, cùng số lượng và cùng phiếu là gọi lặp nên bỏ qua lần sau,
        // vì vậy hai lệnh của cùng một dòng không được có cùng số lượng (5 + 5 phải đổi thành 6 + 4).
        if (ke.Select(k => k.SoLuong).Concat(daLap.Select(s => s.SoLuong)).GroupBy(x => x).Any(g => g.Count() > 1))
            return Loi("Hai lệnh xếp của cùng một dòng hàng không được có cùng số lượng. Hãy chia số lượng khác đi, ví dụ 6 và 4 thay vì 5 và 5.", MaLoiDichVu.DuLieuKhongHopLe);

        var lenh = ke.Select(k => new SapXep { MaCTN = maCTN, MaViTri = k.MaViTri, SoLuong = k.SoLuong }).ToList();
        db.SapXep.AddRange(lenh);
        await db.SaveChangesAsync(ct);   // một lần lưu nên các lệnh được lập cùng lúc hoặc không lệnh nào
        await audit.Ghi(maND, "Lập lệnh sắp xếp", LoaiChungTu.NK, p.MaPN, $"{p.SoPhieu}: {lenh.Count} lệnh, tổng {soLuong}", ct);
        return KetQua<IReadOnlyList<SapXep>>.Ok(lenh, $"Đã lập {lenh.Count} lệnh sắp xếp, tổng {soLuong} hàng.");
    }

    // ---- Xác nhận bằng quét mã ----

    /// <summary>
    /// Thủ kho quét mã ô và mã lô để xác nhận lệnh. Quét sai ô hoặc sai lô thì không ghi gì. Đúng thì làm tất cả trong MỘT giao dịch:
    /// chuyển lệnh sang Đã xác nhận (lệnh có điều kiện nên bấm hai lần chỉ ghi một lần), kiểm lại sức chứa của ô, chuyển hàng từ khu tiếp nhận
    /// sang ô bằng IStockService.ChuyenO (từ lúc này hàng mới tính vào tồn khả dụng), hết hàng đạt thì phiếu Hoàn tất. Lỗi ở đâu thì hoàn tác hết.
    /// </summary>
    public async Task<KetQua> XacNhan(int maSX, string? maVachO, string? maVachLo, int maND, CancellationToken ct = default)
    {
        var sx = await db.SapXep.AsNoTracking().FirstOrDefaultAsync(s => s.MaSX == maSX, ct);
        if (sx is null) return KetQua.Loi("Không tìm thấy lệnh sắp xếp.", MaLoiDichVu.KhongTimThay);
        if (sx.TrangThai != SapXep.ChoXacNhan) return KetQua.Loi($"Lệnh đang ở trạng thái {sx.TrangThai}, không xác nhận được.", MaLoiDichVu.SaiTrangThai);

        var d = await db.ChiTietPhieuNhap.AsNoTracking().FirstAsync(x => x.MaCTN == sx.MaCTN, ct);
        var p = await db.PhieuNhap.AsNoTracking().FirstAsync(x => x.MaPN == d.MaPN, ct);
        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == sx.MaViTri, ct);
        var lo = d.MaLo is int maLo ? await db.LoHang.AsNoTracking().FirstOrDefaultAsync(l => l.MaLo == maLo, ct) : null;
        if (lo is null) return KetQua.Loi("Dòng hàng chưa có lô (chưa tiếp nhận).", MaLoiDichVu.SaiTrangThai);
        if (p.TrangThai != PhieuNhap.DaKiemTra) return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, không xếp hàng được.", MaLoiDichVu.SaiTrangThai);

        // 1. Quét đúng ô và đúng lô. Không nói mã đúng là gì, chỉ nói ô cần quét.
        if (!Khop(maVachO, o.MaVachO)) return KetQua.Loi($"Quét sai ô: lệnh này xếp vào ô {o.TenViTri}. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!Khop(maVachLo, lo.MaVachLo)) return KetQua.Loi("Quét sai lô: mã lô không phải của dòng hàng này. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);

        // 2. Hàng đang nằm ở ô Tiếp nhận của kho (T1-2 ghi vào đó); tìm ô còn đủ khả dụng cho số lượng của lệnh.
        var nguon = await (
            from t in db.TonKho.AsNoTracking()
            join v in db.ViTri.AsNoTracking() on t.MaViTri equals v.MaViTri
            where t.MaLo == lo.MaLo && v.MaKho == p.MaKho && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan && t.SoLuongTon - t.SoLuongGiu >= sx.SoLuong
            orderby v.MaViTri
            select (int?)v.MaViTri).FirstOrDefaultAsync(ct);
        if (nguon is null) return KetQua.Loi($"Khu tiếp nhận không còn đủ {sx.SoLuong} của lô {lo.MaVachLo} để xếp.", MaLoiDichVu.VuotTon);

        // Một giao dịch: nơi gọi chưa mở thì mở ở đây. Điểm lưu (savepoint) giúp hoàn tác riêng phần này kể cả khi nơi gọi đã mở giao dịch.
        await using var giaoDichMoi = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var giaoDich = db.Database.CurrentTransaction!;
        await giaoDich.CreateSavepointAsync(DiemLuu, ct);

        var ghi = await GhiXacNhan(sx, d, p, lo, nguon.Value, maND, ct);
        if (!ghi.ThanhCong)
        {
            await giaoDich.RollbackToSavepointAsync(DiemLuu, ct);
            return KetQua.Loi(ghi.ThongBao ?? "Không xác nhận được.", ghi.MaLoi);
        }
        if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);

        await audit.Ghi(maND, "Xác nhận sắp xếp vào ô", LoaiChungTu.NK, p.MaPN, $"{p.SoPhieu}: {sx.SoLuong} lô {lo.MaVachLo} vào ô {o.TenViTri}", ct);
        return KetQua.Ok($"Đã xếp {sx.SoLuong} hàng vào ô {o.TenViTri}, hàng đã tính vào tồn khả dụng." + (ghi.GiaTri ? $" Phiếu {p.SoPhieu} đã hoàn tất." : ""));
    }

    private const string DiemLuu = "XacNhanSapXep";

    /// <summary>Các bước ghi của XacNhan (đã nằm trong giao dịch). GiaTri = true khi mọi hàng đạt của phiếu đã xếp xong. Trả lỗi thì nơi gọi hoàn tác.</summary>
    private async Task<KetQua<bool>> GhiXacNhan(SapXep sx, ChiTietPhieuNhap d, PhieuNhap p, LoHang lo, int nguon, int maND, CancellationToken ct)
    {
        // 1. Chuyển lệnh sang Đã xác nhận bằng lệnh có điều kiện nên bấm hai lần chỉ ghi một lần.
        var da = await db.SapXep.Where(s => s.MaSX == sx.MaSX && s.TrangThai == SapXep.ChoXacNhan).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.TrangThai, SapXep.DaXacNhan).SetProperty(x => x.MaNguoiXN, (int?)maND).SetProperty(x => x.ThoiGianXN, (DateTime?)DateTime.UtcNow), ct);
        if (da == 0) return KetQua<bool>.Loi("Lệnh vừa được xác nhận hoặc hủy ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        // 2. Kiểm lại sức chứa tại thời điểm xác nhận. Lệnh này đã là Đã xác nhận ở trên nên không bị tính hai lần.
        var sc = await sucChua.KiemTra(sx.MaViTri, d.MaBT, sx.SoLuong, ct);
        if (!sc.Du) return KetQua<bool>.Loi(sc.LyDo ?? "Ô không còn đủ chỗ.", sc.MaLoi ?? MaLoiDichVu.VuotSucChua);

        // 3. Chuyển hàng. ChuyenO bỏ qua khi thấy sổ cái đã có đúng một lần chuyển như vậy, nên kiểm lại hàng thật sự đã sang ô.
        var truoc = (await ton.LayTon(lo.MaLo, sx.MaViTri, ct))?.SoLuongTon ?? 0;
        var chuyen = await ton.ChuyenO(lo.MaLo, nguon, sx.MaViTri, sx.SoLuong, new ThamChieuChungTu(LoaiChungTu.NK, p.MaPN), maND, ct);
        if (!chuyen.ThanhCong) return KetQua<bool>.Loi(chuyen.ThongBao ?? "Không chuyển được hàng.", chuyen.MaLoi);
        var sau = (await ton.LayTon(lo.MaLo, sx.MaViTri, ct))?.SoLuongTon ?? 0;
        if (sau - truoc != sx.SoLuong)
            return KetQua<bool>.Loi("Sổ cái tồn kho đã có một lần chuyển cùng lô, cùng số lượng của phiếu này nên bỏ qua lần chuyển này. Hãy hủy lệnh và lập lại với số lượng khác.", MaLoiDichVu.SaiTrangThai);

        // 4. Mọi hàng đạt của phiếu đã xếp xong thì phiếu Hoàn tất.
        var dong = await db.ChiTietPhieuNhap.AsNoTracking().Where(x => x.MaPN == p.MaPN).ToListAsync(ct);
        var ma = dong.Select(x => x.MaCTN).ToList();
        var daXep = (await db.SapXep.AsNoTracking().Where(s => ma.Contains(s.MaCTN) && s.TrangThai == SapXep.DaXacNhan).ToListAsync(ct))
            .GroupBy(s => s.MaCTN).ToDictionary(g => g.Key, g => g.Sum(s => s.SoLuong));
        var xong = dong.All(x => (x.SLDat ?? 0) <= daXep.GetValueOrDefault(x.MaCTN));
        if (xong)
            await db.PhieuNhap.Where(x => x.MaPN == p.MaPN && x.TrangThai == PhieuNhap.DaKiemTra)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuNhap.HoanTat), ct);
        return KetQua<bool>.Ok(xong);
    }

    // ---- Hủy lệnh ----

    /// <summary>Hủy lệnh đang chờ xác nhận (trả lại chỗ đã chiếm trong ô). Muốn xếp vào ô khác thì hủy rồi lập lệnh mới.</summary>
    public async Task<KetQua> Huy(int maSX, int maND, CancellationToken ct = default)
    {
        var n = await db.SapXep.Where(s => s.MaSX == maSX && s.TrangThai == SapXep.ChoXacNhan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, SapXep.DaHuy), ct);
        if (n == 0) return KetQua.Loi("Chỉ hủy được lệnh đang chờ xác nhận.", MaLoiDichVu.SaiTrangThai);
        var maPN = await db.SapXep.Where(s => s.MaSX == maSX).Join(db.ChiTietPhieuNhap, s => s.MaCTN, c => c.MaCTN, (s, c) => c.MaPN).FirstAsync(ct);
        await audit.Ghi(maND, "Hủy lệnh sắp xếp", LoaiChungTu.NK, maPN, $"lệnh {maSX}", ct);
        return KetQua.Ok("Đã hủy lệnh sắp xếp.");
    }

    // ---- Nội bộ ----

    /// <summary>Mã quét khớp mã vạch (bỏ khoảng trắng hai đầu, không phân biệt hoa thường). Ô hoặc lô chưa có mã vạch thì không bao giờ khớp.</summary>
    public static bool Khop(string? maQuet, string? maVach) =>
        !string.IsNullOrWhiteSpace(maVach) && string.Equals(maQuet?.Trim(), maVach, StringComparison.OrdinalIgnoreCase);

    private static KetQua<IReadOnlyList<SapXep>> Loi(string thongBao, string maLoi) => KetQua<IReadOnlyList<SapXep>>.Loi(thongBao, maLoi);
}

/// <summary>
/// Nguồn hàng chờ xếp thật cho <see cref="ICapacityService"/> (T4-5): lệnh Chờ xác nhận đã chiếm chỗ trong ô nhưng chưa vào TonKho,
/// nên tính vào Vđ, Kđ khi kiểm tra sức chứa. Đăng ký trong NhapKhoModule thay bản mặc định "không có hàng chờ".
/// </summary>
public sealed class HangChoXepTuSapXep(KhoDbContext db) : INguonHangChoXep
{
    public async Task<IReadOnlyList<DongChoXep>> LayTheoO(int maViTri, CancellationToken ct = default) =>
        await (from s in db.SapXep.AsNoTracking()
               where s.MaViTri == maViTri && s.TrangThai == SapXep.ChoXacNhan
               join c in db.ChiTietPhieuNhap.AsNoTracking() on s.MaCTN equals c.MaCTN
               select new DongChoXep(c.MaBT, s.SoLuong)).ToListAsync(ct);
}
