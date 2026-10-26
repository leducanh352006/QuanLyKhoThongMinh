using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Contracts.Stubs;

internal static class KiemTraDongPhieu
{
    /// <summary>Kiểm tra chung: có dòng, số lượng dương, kho tồn tại; yêu cầu MaLo và MaViTri nếu là phiếu điều chỉnh do kiểm kê.</summary>
    public static string? Kiem(int maKho, IReadOnlyList<DongPhieu> dong, bool canLoVaViTri)
    {
        if (SeedDataSet.Kho.All(k => k.MaKho != maKho)) return $"Không tìm thấy kho {maKho}.";
        if (dong.Count == 0) return "Phiếu phải có ít nhất một dòng hàng.";
        foreach (var d in dong)
        {
            if (d.SoLuong <= 0) return "Số lượng phải lớn hơn 0.";
            if (SeedDataSet.TimBienThe(d.MaBT) is null) return $"Không tìm thấy biến thể {d.MaBT}.";
            if (canLoVaViTri && (d.MaLo is null || d.MaViTri is null))
                return "Phiếu điều chỉnh do kiểm kê: mỗi dòng phải có lô và ô.";
        }
        return null;
    }
}

/// <summary>Bản giả của <see cref="INhapKhoService"/>: chỉ kiểm tra đầu vào và trả số phiếu giả (tiền tố NK).</summary>
public sealed class NhapKhoServiceStub : INhapKhoService
{
    private int _ma;

    public Task<KetQua<PhieuRef>> TaoPhieuNhapThua(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default)
    {
        var loi = KiemTraDongPhieu.Kiem(maKho, dong, canLoVaViTri: true);
        return Task.FromResult(loi is null ? KetQua<PhieuRef>.Ok(TaoRef()) : KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));
    }

    public Task<KetQua<PhieuRef>> TaoPhieuNhapNhap(int maKho, IReadOnlyList<DongPhieu> dong, int maNguoiLap, int? maGoiY = null, CancellationToken ct = default)
    {
        var loi = KiemTraDongPhieu.Kiem(maKho, dong, canLoVaViTri: false);
        return Task.FromResult(loi is null ? KetQua<PhieuRef>.Ok(TaoRef()) : KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));
    }

    private PhieuRef TaoRef()
    {
        var ma = Interlocked.Increment(ref _ma);
        return new PhieuRef(ma, $"NK{ma:D6}");
    }
}

/// <summary>Bản giả của <see cref="IXuatKhoService"/>: chỉ kiểm tra đầu vào và trả số phiếu giả (tiền tố XK).</summary>
public sealed class XuatKhoServiceStub : IXuatKhoService
{
    private int _ma;

    public Task<KetQua<PhieuRef>> TaoPhieuXuatThieu(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default)
    {
        var loi = KiemTraDongPhieu.Kiem(maKho, dong, canLoVaViTri: true);
        if (loi is not null) return Task.FromResult(KetQua<PhieuRef>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));
        var ma = Interlocked.Increment(ref _ma);
        return Task.FromResult(KetQua<PhieuRef>.Ok(new PhieuRef(ma, $"XK{ma:D6}")));
    }
}
