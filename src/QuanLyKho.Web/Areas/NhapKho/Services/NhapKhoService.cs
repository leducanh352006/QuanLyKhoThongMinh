using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>
/// Bản thật của <see cref="INhapKhoService"/> (việc T1-8), thay <c>NhapKhoServiceStub</c>: hai hàm cho nhóm khác tạo phiếu nhập tự động.
/// Cả hai chạy trong một giao dịch và tham gia giao dịch của nơi gọi nếu đã mở sẵn trên cùng KhoDbContext (khi đó nơi gọi tự hoàn tác nếu lỗi).
/// </summary>
public sealed class NhapKhoService(KhoDbContext db, IStockService ton, IAuditService audit) : INhapKhoService
{
    /// <summary>
    /// Phiếu Thừa do kiểm kê: lưu thẳng ở trạng thái Hoàn tất rồi ghi tăng tồn đúng lô và ô của từng dòng (loại biến động Nhận hàng, chứng từ NK).
    /// Ô đang bị khóa kiểm vẫn được ghi, vì chính đợt kiểm kê đó khóa ô. Mỗi phiếu kiểm chỉ có một phiếu Thừa.
    /// Lô và ô của dòng nằm ở sổ cái BienDongTon (tra theo chứng từ NK và mã phiếu), dòng phiếu chỉ giữ biến thể và số lượng.
    /// </summary>
    public async Task<KetQua<PhieuRef>> TaoPhieuNhapThua(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default)
    {
        var loi = maPK <= 0 ? "Phiếu Thừa do kiểm kê phải có mã phiếu kiểm." : KiemTraDong(dong, canLoVaViTri: true) ?? await KiemTraDanhMuc(maKho, maNguoiLap, dong, true, ct);
        if (loi is not null) return KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        var daCo = await db.PhieuNhap.Where(p => p.MaPK == maPK && p.LoaiPhieu == PhieuNhap.LoaiThua).Select(p => p.SoPhieu).FirstOrDefaultAsync(ct);
        if (daCo is not null) return KetQua<PhieuRef>.Loi($"Phiếu kiểm {maPK} đã có phiếu nhập Thừa do kiểm kê {daCo}.", MaLoiDichVu.SaiTrangThai);

        var phieu = TaoPhieu(maKho, dong, maNguoiLap, PhieuNhap.LoaiThua, PhieuNhap.HoanTat, null, $"Thừa do kiểm kê, phiếu kiểm {maPK}", maPK);
        return await TrongGiaoDich(phieu, async () =>
        {
            // Gộp các dòng cùng lô và ô thành một lần ghi: StockService coi hai lần ghi giống hệt nhau là một lần gọi lại và bỏ qua lần sau.
            foreach (var nhom in dong.GroupBy(d => (MaLo: d.MaLo!.Value, MaViTri: d.MaViTri!.Value)))
            {
                var kq = await ton.GhiTang(new DongTon(nhom.Key.MaLo, nhom.Key.MaViTri, nhom.Sum(d => d.SoLuong)),
                    LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, phieu.MaPN), maNguoiLap, ct);
                if (!kq.ThanhCong) return KetQua<PhieuRef>.Loi(kq.ThongBao ?? "Không ghi được tồn kho.", kq.MaLoi);
            }
            await audit.Ghi(maNguoiLap, "Tạo phiếu nhập Thừa do kiểm kê", LoaiChungTu.NK, phieu.MaPN, $"{phieu.SoPhieu}: phiếu kiểm {maPK}", ct);
            return KetQua<PhieuRef>.Ok(new PhieuRef(phieu.MaPN, phieu.SoPhieu));
        }, ct);
    }

    /// <summary>
    /// Phiếu nháp từ gợi ý G3: loại Mua hàng, chưa có nhà cung cấp (quản lý kho chọn khi sửa phiếu), không động tới tồn.
    /// Sau đó phiếu đi đúng quy trình thường: xác nhận, kiểm tra hàng, sắp xếp. MaLo và MaViTri của dòng (nếu có) được bỏ qua.
    /// </summary>
    public async Task<KetQua<PhieuRef>> TaoPhieuNhapNhap(int maKho, IReadOnlyList<DongPhieu> dong, int maNguoiLap, int? maGoiY = null, CancellationToken ct = default)
    {
        var loi = KiemTraDong(dong, canLoVaViTri: false) ?? await KiemTraDanhMuc(maKho, maNguoiLap, dong, false, ct);
        if (loi is not null) return KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        var dienGiai = maGoiY is null ? "Lập từ gợi ý G3" : $"Lập từ gợi ý G3 số {maGoiY}";
        var phieu = TaoPhieu(maKho, dong, maNguoiLap, "Mua hàng", PhieuNhap.Nhap, null, dienGiai, null);
        return await TrongGiaoDich(phieu, async () =>
        {
            await audit.Ghi(maNguoiLap, "Tạo phiếu nhập nháp từ gợi ý G3", LoaiChungTu.NK, phieu.MaPN, $"{phieu.SoPhieu}: {dienGiai}", ct);
            return KetQua<PhieuRef>.Ok(new PhieuRef(phieu.MaPN, phieu.SoPhieu));
        }, ct);
    }

    /// <summary>Quy tắc không cần CSDL: có dòng, số lượng dương, đã chọn mặt hàng; phiếu Thừa còn phải có lô và ô. Trả về lỗi, hoặc null nếu hợp lệ.</summary>
    public static string? KiemTraDong(IReadOnlyList<DongPhieu> dong, bool canLoVaViTri)
    {
        if (dong.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
        if (dong.Any(d => d.MaBT <= 0)) return "Chọn mặt hàng cho mỗi dòng.";
        if (dong.Any(d => d.SoLuong <= 0)) return "Số lượng mỗi dòng phải lớn hơn 0.";
        if (canLoVaViTri && dong.Any(d => d.MaLo is null || d.MaViTri is null)) return "Phiếu Thừa do kiểm kê: mỗi dòng phải có lô và ô.";
        return null;
    }

    /// <summary>Kho, người lập, mặt hàng phải có thật; phiếu Thừa còn phải đúng lô của biến thể và đúng ô của kho (Bảng 2.17 dòng 2 và 3).</summary>
    private async Task<string?> KiemTraDanhMuc(int maKho, int maNguoiLap, IReadOnlyList<DongPhieu> dong, bool canLoVaViTri, CancellationToken ct)
    {
        if (!await db.Kho.AnyAsync(k => k.MaKho == maKho && k.TrangThai == TrangThaiHoatDong.HoatDong, ct))
            return $"Kho {maKho} không tồn tại hoặc đã ngừng hoạt động.";
        if (!await db.NguoiDung.AnyAsync(n => n.MaND == maNguoiLap, ct)) return $"Không tìm thấy người lập {maNguoiLap}.";

        var maBT = dong.Select(d => d.MaBT).Distinct().ToList();
        if (await db.BienThe.CountAsync(b => maBT.Contains(b.MaBT), ct) != maBT.Count) return "Có mặt hàng không tồn tại.";
        if (!canLoVaViTri) return null;

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

    private static PhieuNhap TaoPhieu(int maKho, IReadOnlyList<DongPhieu> dong, int maNguoiLap, string loai, string trangThai, int? maNCC, string dienGiai, int? maPK) => new()
    {
        SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành NK + mã ngay sau khi có MaPN
        LoaiPhieu = loai, TrangThai = trangThai, ThoiGian = DateTime.UtcNow, MaKho = maKho, MaNCC = maNCC, MaPK = maPK,
        MaNguoiLap = maNguoiLap, DienGiai = dienGiai,
        ChiTiet = dong.Select(d => new ChiTietPhieuNhap { MaBT = d.MaBT, SLDuKien = d.SoLuong }).ToList(),
    };

    /// <summary>
    /// Lưu phiếu (lấy MaPN rồi đặt số phiếu NK + mã) và chạy <paramref name="tiep"/> trong cùng giao dịch. Thất bại thì hoàn tác và gỡ phiếu khỏi bộ theo dõi.
    /// Nếu nơi gọi đã mở giao dịch thì chỉ tham gia, việc hoàn tác khi thất bại thuộc về nơi gọi.
    /// </summary>
    private async Task<KetQua<PhieuRef>> TrongGiaoDich(PhieuNhap phieu, Func<Task<KetQua<PhieuRef>>> tiep, CancellationToken ct)
    {
        var tuMo = db.Database.CurrentTransaction is null;
        await using var giaoDich = tuMo ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            db.PhieuNhap.Add(phieu);
            await db.SaveChangesAsync(ct);                       // lấy được MaPN
            phieu.SoPhieu = $"NK{phieu.MaPN:D6}";
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

    private void GoKhoiTheoDoi(PhieuNhap phieu)
    {
        foreach (var d in phieu.ChiTiet) db.Entry(d).State = EntityState.Detached;
        db.Entry(phieu).State = EntityState.Detached;
    }
}
