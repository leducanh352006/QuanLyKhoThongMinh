using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Bản thật của <see cref="IXuatKhoService"/> (việc T2-8), thay <c>XuatKhoServiceStub</c>: hàm cho nhóm Kiểm tra hàng hóa tạo phiếu xuất tự động.
/// Chạy trong một giao dịch và tham gia giao dịch của nơi gọi nếu đã mở sẵn trên cùng KhoDbContext (khi đó nơi gọi tự hoàn tác nếu lỗi).
/// </summary>
public sealed class XuatKhoService(KhoDbContext db, IStockService ton, IAuditService audit) : IXuatKhoService
{
    /// <summary>
    /// Phiếu Thiếu do kiểm kê: lưu thẳng ở trạng thái Hoàn tất (coi như đã được quản lý kho duyệt khi duyệt phát sinh kiểm kê) rồi ghi giảm
    /// tồn đúng lô và ô của từng dòng (loại biến động Xuất kho, chứng từ XK). Không qua lấy hàng vì hàng thật đã thiếu, không có gì để lấy.
    /// Mỗi phiếu kiểm chỉ có một phiếu Thiếu. ChiTietPhieuXuat không có cột lô/ô (bảng đó do T2-2, T2-4 thêm sau) nên lô, ô của mỗi dòng
    /// chỉ dùng để gọi IStockService, không lưu lại trên dòng phiếu; sổ cái BienDongTon (chứng từ XK, mã phiếu) mới là nơi ghi lại.
    /// </summary>
    public async Task<KetQua<PhieuRef>> TaoPhieuXuatThieu(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default)
    {
        var loi = maPK <= 0 ? "Phiếu Thiếu do kiểm kê phải có mã phiếu kiểm." : KiemTraDong(dong) ?? await KiemTraDanhMuc(maKho, maNguoiLap, dong, ct);
        if (loi is not null) return KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        var daCo = await db.PhieuXuat.Where(p => p.MaPK == maPK && p.LoaiPhieu == PhieuXuat.ThieuDoKiemKe).Select(p => p.SoPhieu).FirstOrDefaultAsync(ct);
        if (daCo is not null) return KetQua<PhieuRef>.Loi($"Phiếu kiểm {maPK} đã có phiếu xuất Thiếu do kiểm kê {daCo}.", MaLoiDichVu.SaiTrangThai);

        var phieu = TaoPhieu(maKho, dong, maNguoiLap, maPK);
        return await TrongGiaoDich(phieu, async () =>
        {
            // Gộp các dòng cùng lô và ô thành một lần ghi: StockService coi hai lần ghi giống hệt nhau là một lần gọi lại và bỏ qua lần sau.
            foreach (var nhom in dong.GroupBy(d => (MaLo: d.MaLo!.Value, MaViTri: d.MaViTri!.Value)))
            {
                var kq = await ton.GhiGiam(new DongTon(nhom.Key.MaLo, nhom.Key.MaViTri, nhom.Sum(d => d.SoLuong)),
                    LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, phieu.MaPX), maNguoiLap, tuGiuCho: false, ct);
                if (!kq.ThanhCong) return KetQua<PhieuRef>.Loi(kq.ThongBao ?? "Không ghi được tồn kho.", kq.MaLoi);
            }
            await audit.Ghi(maNguoiLap, "Tạo phiếu xuất Thiếu do kiểm kê", LoaiChungTu.XK, phieu.MaPX, $"{phieu.SoPhieu}: phiếu kiểm {maPK}", ct);
            return KetQua<PhieuRef>.Ok(new PhieuRef(phieu.MaPX, phieu.SoPhieu));
        }, ct);
    }

    /// <summary>Quy tắc không cần CSDL: có dòng, số lượng dương, đã chọn mặt hàng, mỗi dòng phải có lô và ô (nơi phát hiện thiếu). Trả về lỗi, hoặc null nếu hợp lệ.</summary>
    public static string? KiemTraDong(IReadOnlyList<DongPhieu> dong)
    {
        if (dong.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
        if (dong.Any(d => d.MaBT <= 0)) return "Chọn mặt hàng cho mỗi dòng.";
        if (dong.Any(d => d.SoLuong <= 0)) return "Số lượng mỗi dòng phải lớn hơn 0.";
        if (dong.Any(d => d.MaLo is null || d.MaViTri is null)) return "Phiếu Thiếu do kiểm kê: mỗi dòng phải có lô và ô.";
        return null;
    }

    /// <summary>Kho, người lập, mặt hàng phải có thật; lô phải đúng biến thể và ô phải đúng kho (Bảng 2.17 dòng 2 và 3).</summary>
    private async Task<string?> KiemTraDanhMuc(int maKho, int maNguoiLap, IReadOnlyList<DongPhieu> dong, CancellationToken ct)
    {
        if (!await db.Kho.AnyAsync(k => k.MaKho == maKho && k.TrangThai == TrangThaiHoatDong.HoatDong, ct))
            return $"Kho {maKho} không tồn tại hoặc đã ngừng hoạt động.";
        if (!await db.NguoiDung.AnyAsync(n => n.MaND == maNguoiLap, ct)) return $"Không tìm thấy người lập {maNguoiLap}.";

        var maBT = dong.Select(d => d.MaBT).Distinct().ToList();
        if (await db.BienThe.CountAsync(b => maBT.Contains(b.MaBT), ct) != maBT.Count) return "Có mặt hàng không tồn tại.";

        var maLo = dong.Select(d => d.MaLo!.Value).Distinct().ToList();
        var loCua = await db.LoHang.Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => l.MaBT, ct);
        var maViTri = dong.Select(d => d.MaViTri!.Value).Distinct().ToList();
        var oCua = await db.ViTri.Where(v => maViTri.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri, ct);
        foreach (var d in dong)
        {
            if (!loCua.TryGetValue(d.MaLo!.Value, out var btCuaLo)) return $"Không tìm thấy lô {d.MaLo}.";
            if (btCuaLo != d.MaBT) return $"Lô {d.MaLo} không thuộc biến thể {d.MaBT}.";
            if (!oCua.TryGetValue(d.MaViTri!.Value, out var o)) return $"Không tìm thấy vị trí {d.MaViTri}.";
            if (o.MaKho != maKho) return $"'{o.TenViTri}' không thuộc kho {maKho}.";
            if (o.CapViTri != CapViTriGiaTri.O) return $"Hàng chỉ được để ở ô, '{o.TenViTri}' là cấp {o.CapViTri}.";
        }
        return null;
    }

    private static PhieuXuat TaoPhieu(int maKho, IReadOnlyList<DongPhieu> dong, int maNguoiLap, int maPK) => new()
    {
        SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành XK + mã ngay sau khi có MaPX
        LoaiPhieu = PhieuXuat.ThieuDoKiemKe, TrangThai = PhieuXuat.HoanTat, ThoiGian = DateTime.UtcNow, MaKho = maKho, MaPK = maPK,
        MaNguoiLap = maNguoiLap, MaNguoiDuyet = maNguoiLap, DienGiai = $"Thiếu do kiểm kê, phiếu kiểm {maPK}",
        ChiTiet = dong.Select(d => new ChiTietPhieuXuat { MaBT = d.MaBT, SoLuong = d.SoLuong }).ToList(),
    };

    /// <summary>
    /// Lưu phiếu (lấy MaPX rồi đặt số phiếu XK + mã) và chạy <paramref name="tiep"/> trong cùng giao dịch. Thất bại thì hoàn tác và gỡ phiếu khỏi bộ theo dõi.
    /// Nếu nơi gọi đã mở giao dịch thì chỉ tham gia, việc hoàn tác khi thất bại thuộc về nơi gọi.
    /// </summary>
    private async Task<KetQua<PhieuRef>> TrongGiaoDich(PhieuXuat phieu, Func<Task<KetQua<PhieuRef>>> tiep, CancellationToken ct)
    {
        var tuMo = db.Database.CurrentTransaction is null;
        await using var giaoDich = tuMo ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            db.PhieuXuat.Add(phieu);
            await db.SaveChangesAsync(ct);                       // lấy được MaPX
            phieu.SoPhieu = $"XK{phieu.MaPX:D6}";
            await db.SaveChangesAsync(ct);

            var kq = await tiep();
            if (kq.ThanhCong && giaoDich is not null) await giaoDich.CommitAsync(ct);
            if (!kq.ThanhCong && tuMo) GoKhoiTheoDoi(phieu);
            return kq;
        }
        catch
        {
            if (tuMo) GoKhoiTheoDoi(phieu);
            throw;
        }
    }

    private void GoKhoiTheoDoi(PhieuXuat phieu)
    {
        foreach (var d in phieu.ChiTiet) db.Entry(d).State = EntityState.Detached;
        db.Entry(phieu).State = EntityState.Detached;
    }
}
