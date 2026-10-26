using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>Bản giả của <see cref="IHangHoaService"/>: lô mới giữ trong bộ nhớ, bắt đầu từ lô mẫu.</summary>
public sealed class HangHoaServiceStub : IHangHoaService
{
    private readonly object _khoa = new();
    private readonly List<LoHangDto> _lo = SeedDataSet.LoHang.Select(l => new LoHangDto(l.MaLo, l.MaVachLo, l.MaBT, l.HanDung)).ToList();
    private int _maLoCuoi = SeedDataSet.LoHang.Max(l => l.MaLo);

    public Task<KetQua<LoHangDto>> TaoLo(int maBT, DateOnly? hanDung, string? maVachLo = null, CancellationToken ct = default)
    {
        var bt = SeedDataSet.TimBienThe(maBT);
        if (bt is null)
            return Task.FromResult(KetQua<LoHangDto>.Loi($"Không tìm thấy biến thể {maBT}.", MaLoiDichVu.KhongTimThay));

        var sp = SeedDataSet.SanPham.First(s => s.MaSP == bt.MaSP);
        if (sp.CoHanDung && hanDung is null)
            return Task.FromResult(KetQua<LoHangDto>.Loi($"Sản phẩm '{sp.TenSP}' quản lý theo hạn dùng: lô phải có hạn dùng.", MaLoiDichVu.DuLieuKhongHopLe));

        lock (_khoa)
        {
            var ma = maVachLo?.Trim();
            if (string.IsNullOrEmpty(ma))
                ma = $"LO-{DateTime.UtcNow:yyMM}-{_maLoCuoi + 1:D3}";
            if (_lo.Any(l => string.Equals(l.MaVachLo, ma, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult(KetQua<LoHangDto>.Loi($"Mã lô '{ma}' đã tồn tại; mã lô phải duy nhất.", MaLoiDichVu.DuLieuKhongHopLe));

            var lo = new LoHangDto(++_maLoCuoi, ma, maBT, hanDung);
            _lo.Add(lo);
            return Task.FromResult(KetQua<LoHangDto>.Ok(lo));
        }
    }
}
