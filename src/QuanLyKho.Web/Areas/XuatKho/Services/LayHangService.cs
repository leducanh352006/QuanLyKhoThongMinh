using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Lấy hàng theo gợi ý đã duyệt, quét mã xác nhận (việc T2-4, issue #20, Bảng 2.4 dòng 4, Bảng 2.23): thủ kho quét mã ô, mã lô,
/// mã vạch biến thể cho từng dòng <see cref="LayHang"/> đã được giữ chỗ lúc duyệt phiếu (T2-3); sai bất kỳ mã nào thì từ chối,
/// không ghi gì. Lấy thiếu so với số đã giữ (SLGiu) phải ghi SLThucLay kèm LyDoLech (ràng buộc CSDL CK_LayHang_LyDoLech đã có từ
/// T2-8, ở đây kiểm thêm phía dịch vụ để báo lỗi rõ ràng trước khi chạm CSDL).
/// <para>
/// Giả định (issue không nói rõ, ghi lại để nhóm xác nhận): việc này KHÔNG trừ tồn thật (không gọi GhiGiam) — chỉ ghi nhận số
/// thực lấy. Lần xác nhận đầu tiên của phiếu chuyển Đã duyệt → Đang lấy hàng; chuyển tiếp "Đã đối chiếu"/"Hoàn tất" cùng việc
/// trừ tồn thật (GhiGiam, tuGiuCho: true) để dành cho việc đối chiếu và xác nhận xuất (T2-5), chưa làm ở đây.
/// </para>
/// </summary>
public sealed class LayHangService(KhoDbContext db, IAuditService audit)
{
    /// <summary>
    /// Xác nhận một dòng lấy hàng bằng mã đã quét. Đúng cả ba mã (ô, lô, biến thể) mới ghi; sai bất kỳ mã nào thì từ chối,
    /// không ghi gì và không nói mã đúng là gì (chỉ nói cần quét gì), giống <c>SapXepService.XacNhan</c> (T1-4).
    /// </summary>
    public async Task<KetQua> XacNhan(int maLH, string? maVachO, string? maVachLo, string? maVachBT, int slThucLay, string? lyDoLech, int maND, CancellationToken ct = default)
    {
        var l = await db.LayHang.AsNoTracking().FirstOrDefaultAsync(x => x.MaLH == maLH, ct);
        if (l is null) return KetQua.Loi("Không tìm thấy dòng lấy hàng.", MaLoiDichVu.KhongTimThay);
        if (l.SLThucLay is not null) return KetQua.Loi("Dòng này đã xác nhận lấy hàng rồi.", MaLoiDichVu.SaiTrangThai);

        var c = await db.ChiTietPhieuXuat.AsNoTracking().FirstAsync(x => x.MaCTX == l.MaCTX, ct);
        var p = await db.PhieuXuat.AsNoTracking().FirstAsync(x => x.MaPX == c.MaPX, ct);
        if (p.TrangThai is not (PhieuXuat.DaDuyet or PhieuXuat.DangLayHang))
            return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, không lấy hàng được.", MaLoiDichVu.SaiTrangThai);

        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == l.MaViTri, ct);
        var lo = await db.LoHang.AsNoTracking().FirstAsync(x => x.MaLo == l.MaLo, ct);
        var bt = await db.BienThe.AsNoTracking().FirstAsync(x => x.MaBT == c.MaBT, ct);

        if (!Khop(maVachO, o.MaVachO)) return KetQua.Loi($"Quét sai ô: dòng này lấy ở ô {o.TenViTri}. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!Khop(maVachLo, lo.MaVachLo)) return KetQua.Loi("Quét sai lô: mã lô không phải của dòng hàng này. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!Khop(maVachBT, bt.MaVach)) return KetQua.Loi("Quét sai mã vạch: không phải mặt hàng của dòng này. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);

        if (slThucLay < 0 || slThucLay > l.SLGiu)
            return KetQua.Loi($"Số lượng thực lấy phải từ 0 đến {l.SLGiu} (số đã giữ chỗ). Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        string? lyDo = slThucLay == l.SLGiu ? null : string.IsNullOrWhiteSpace(lyDoLech) ? null : lyDoLech.Trim();
        if (slThucLay != l.SLGiu && lyDo is null)
            return KetQua.Loi("Lấy thiếu so với số đã giữ chỗ thì phải ghi lý do lệch. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);

        await using var giaoDichMoi = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;

        // Lệnh có điều kiện "chưa xác nhận" nên bấm hai lần (hoặc hai người cùng xác nhận) chỉ ghi một lần.
        var ghi = await db.LayHang.Where(x => x.MaLH == maLH && x.SLThucLay == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SLThucLay, slThucLay).SetProperty(x => x.LyDoLech, lyDo), ct);
        if (ghi == 0)
        {
            if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
            return KetQua.Loi("Dòng này vừa được xác nhận ở nơi khác.", MaLoiDichVu.SaiTrangThai);
        }

        // Lần quét đầu tiên của phiếu: Đã duyệt -> Đang lấy hàng (điều kiện nên chỉ đổi một lần, các dòng sau không chạm lại).
        await db.PhieuXuat.Where(x => x.MaPX == p.MaPX && x.TrangThai == PhieuXuat.DaDuyet)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuXuat.DangLayHang), ct);

        if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);

        var ketQua = slThucLay == l.SLGiu ? $"Đã lấy đủ {slThucLay}." : $"Đã ghi nhận lấy thiếu: {slThucLay}/{l.SLGiu} ({lyDo}).";
        await audit.Ghi(maND, "Xác nhận lấy hàng", LoaiChungTu.XK, p.MaPX,
            $"{p.SoPhieu}: lô {lo.MaVachLo} ô {o.TenViTri} — {ketQua}", ct);
        return KetQua.Ok(ketQua);
    }

    /// <summary>Mã quét khớp mã vạch (bỏ khoảng trắng hai đầu, không phân biệt hoa thường). Chưa có mã vạch thì không bao giờ khớp.</summary>
    public static bool Khop(string? maQuet, string? maVach) =>
        !string.IsNullOrWhiteSpace(maVach) && string.Equals(maQuet?.Trim(), maVach, StringComparison.OrdinalIgnoreCase);
}
