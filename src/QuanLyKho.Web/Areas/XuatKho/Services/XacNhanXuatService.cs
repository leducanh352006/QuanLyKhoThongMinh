using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Một dòng hiển thị ở màn xác nhận xuất: đúng theo từng (lô, ô) sẽ bị ghi giảm tồn thật khi xác nhận (XacNhan gộp và ghi giảm
/// theo đúng các cặp lô/ô này), để thủ kho thấy rõ sẽ trừ ở mã lô nào và ô nào trước khi bấm xác nhận không thể hoàn tác.
/// </summary>
public sealed record DongXacNhanXuat(int MaBT, string TenHang, string MaVachLo, DateOnly? HanDung, string TenViTri, int SoLuong);

/// <summary>
/// Xác nhận xuất (việc T2-6, issue #15, Bảng 2.4 dòng 6, mục 2.4.2): bước cuối của luồng xuất kho, sau khi đã đối chiếu khớp
/// (T2-5). Ghi giảm tồn thật cho đúng số đã lấy ở từng (lô, ô) qua <see cref="IStockService.GhiGiam"/> với <c>tuGiuCho: true</c>
/// (giảm cả SoLuongTon lẫn SoLuongGiu, vì hàng đã được giữ chỗ từ khi duyệt — T2-3), rồi khóa phiếu (Hoàn tất). Idempotent với
/// bấm lại: <see cref="IStockService"/> tự chống ghi sổ cái hai lần cho cùng (chứng từ, loại, lô, ô, số lượng), và việc chuyển
/// trạng thái dùng <c>ExecuteUpdateAsync</c> có điều kiện "còn đúng Đã đối chiếu" nên hai yêu cầu đồng thời chỉ một cái thắng.
/// <para>Gộp các dòng LayHang theo (MaLo, MaViTri) trước khi gọi GhiGiam: nếu hai dòng của phiếu (ví dụ hai dòng cùng biến thể
/// được PhanBo phân vào cùng một lô/ô) có cùng số lượng thực lấy, StockService.GhiGiam coi lần gọi thứ hai là trùng với lần
/// đầu (cùng chứng từ, loại, lô, ô, số lượng) và bỏ qua — gọi gộp một lần cho mỗi (lô, ô) tránh thiếu tồn bị trừ.</para>
/// </summary>
public sealed class XacNhanXuatService(KhoDbContext db, IStockService ton, IAuditService audit)
{
    public async Task<KetQua<IReadOnlyList<DongXacNhanXuat>>> Xem(int maPX, CancellationToken ct = default)
    {
        var p = await db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == maPX, ct);
        if (p is null) return KetQua<IReadOnlyList<DongXacNhanXuat>>.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);

        var ten = await db.BienThe.AsNoTracking().Where(b => p.ChiTiet.Select(c => c.MaBT).Contains(b.MaBT))
            .Join(db.SanPham.AsNoTracking(), b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .ToDictionaryAsync(x => x.MaBT, x => x.Ten, ct);

        // Chi tiết đúng theo từng (lô, ô) sẽ bị ghi giảm khi xác nhận (xem XacNhan): mã lô, hạn dùng và ô lấy hàng, không chỉ
        // tổng số lượng theo mặt hàng.
        var raw = await (
            from l in db.LayHang.AsNoTracking()
            join c in db.ChiTietPhieuXuat.AsNoTracking() on l.MaCTX equals c.MaCTX
            join lo in db.LoHang.AsNoTracking() on l.MaLo equals lo.MaLo
            join o in db.ViTri.AsNoTracking() on l.MaViTri equals o.MaViTri
            where c.MaPX == maPX && l.SLThucLay > 0
            select new { c.MaBT, lo.MaVachLo, lo.HanDung, o.TenViTri, SoLuong = l.SLThucLay!.Value }
        ).ToListAsync(ct);

        var dong = raw.OrderBy(x => x.MaBT).ThenBy(x => x.MaVachLo).ThenBy(x => x.TenViTri)
            .Select(x => new DongXacNhanXuat(x.MaBT, ten.GetValueOrDefault(x.MaBT, $"biến thể #{x.MaBT}"), x.MaVachLo, x.HanDung, x.TenViTri, x.SoLuong))
            .ToList();
        return KetQua<IReadOnlyList<DongXacNhanXuat>>.Ok(dong);
    }

    /// <summary>Ghi giảm tồn cho đúng số đã lấy ở từng dòng LayHang của phiếu rồi chuyển Hoàn tất. Gọi lại khi đã Hoàn tất bị từ chối ngay.</summary>
    public async Task<KetQua> XacNhan(int maPX, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuXuat.AsNoTracking().FirstOrDefaultAsync(x => x.MaPX == maPX, ct);
        if (p is null) return KetQua.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuXuat.DaDoiChieu)
            return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, không xác nhận xuất được.", MaLoiDichVu.SaiTrangThai);

        var layHang = await (
            from l in db.LayHang.AsNoTracking()
            join c in db.ChiTietPhieuXuat.AsNoTracking() on l.MaCTX equals c.MaCTX
            where c.MaPX == maPX
            select l
        ).ToListAsync(ct);

        // Phòng vệ dữ liệu: phiếu Đã đối chiếu nhưng chưa có dòng lấy hàng nào, hoặc còn dòng chưa quét mã xác nhận (SLThucLay
        // null), thì không được khóa phiếu mà không trừ tồn nào — từ chối ngay, trước khi mở giao dịch.
        if (layHang.Count == 0 || layHang.Any(l => l.SLThucLay is null))
            return KetQua.Loi($"Phiếu {p.SoPhieu} chưa có đủ dòng lấy hàng đã quét mã xác nhận, chưa xác nhận xuất được.", MaLoiDichVu.SaiTrangThai);

        // Sắp theo (MaLo, MaViTri) cố định để hai phiếu xuất xác nhận đồng thời trên tập lô/ô giao nhau luôn khóa theo cùng thứ
        // tự, tránh chờ khóa chéo (deadlock) giữa IStockService.GhiGiam của hai giao dịch.
        var theoLoO = layHang.Where(l => l.SLThucLay is > 0)
            .GroupBy(l => (l.MaLo, l.MaViTri))
            .Select(g => new DongTon(g.Key.MaLo, g.Key.MaViTri, g.Sum(l => l.SLThucLay!.Value)))
            .OrderBy(d => d.MaLo).ThenBy(d => d.MaViTri)
            .ToList();
        if (theoLoO.Count == 0)
            return KetQua.Loi($"Phiếu {p.SoPhieu} chưa có dòng lấy hàng nào có số lượng thực lấy lớn hơn 0, chưa xác nhận xuất được.", MaLoiDichVu.SaiTrangThai);

        await using var giaoDichMoi = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;

        foreach (var dong in theoLoO)
        {
            var ketQua = await ton.GhiGiam(
                dong, LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, maPX), maND, tuGiuCho: true, ct);
            if (!ketQua.ThanhCong)
            {
                if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
                return ketQua;
            }
        }

        // Điều kiện "còn Đã đối chiếu" trong câu lệnh để hai yêu cầu xác nhận đồng thời chỉ một cái đổi được trạng thái; cái thua
        // (ghi == 0) coi như phiếu đã được xác nhận ở nơi khác, không ghi sổ cái lần nữa (StockService.GhiGiam cũng tự chống trùng).
        var ghi = await db.PhieuXuat.Where(x => x.MaPX == maPX && x.TrangThai == PhieuXuat.DaDoiChieu)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.HoanTat), ct);
        if (ghi == 0)
        {
            if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
            return KetQua.Loi("Phiếu vừa được xác nhận xuất ở nơi khác.", MaLoiDichVu.SaiTrangThai);
        }

        if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);
        await audit.Ghi(maND, "Xác nhận xuất", LoaiChungTu.XK, maPX, $"{p.SoPhieu}: đã ghi giảm tồn theo số thực lấy, hoàn tất xuất kho.", ct);
        return KetQua.Ok($"Đã xác nhận xuất phiếu {p.SoPhieu}. Tồn kho đã ghi giảm, phiếu hoàn tất và bị khóa.");
    }
}
