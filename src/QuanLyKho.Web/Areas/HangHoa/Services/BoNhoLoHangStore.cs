using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Kho dữ liệu LoHang trong bộ nhớ, nạp sẵn từ <see cref="SeedDataSet"/>.
/// Dùng khi chưa cấu hình SQL Server ở môi trường Development và trong kiểm thử. Dữ liệu mất khi tắt ứng dụng.
/// </summary>
public sealed class BoNhoLoHangStore : ILoHangStore
{
    private readonly object _khoa = new();
    private readonly List<LoHang> _lo = new();
    private int _maLo;

    public BoNhoLoHangStore()
    {
        foreach (var l in SeedDataSet.LoHang)
        {
            _lo.Add(new LoHang { MaLo = l.MaLo, MaVachLo = l.MaVachLo, MaBT = l.MaBT, HanDung = l.HanDung });
            _maLo = Math.Max(_maLo, l.MaLo);
        }
    }

    public Task<IReadOnlyList<LoHang>> LayTheoBienThe(int maBT, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<LoHang>>(_lo.Where(l => l.MaBT == maBT).OrderBy(l => l.MaLo).ToList());
    }

    public Task<LoHang?> Tim(int maLo, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_lo.FirstOrDefault(l => l.MaLo == maLo));
    }

    public Task<LoHang?> TimTheoMaVach(string maVachLo, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_lo.FirstOrDefault(l => string.Equals(l.MaVachLo, maVachLo, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<LoHang> Them(LoHang lo, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_lo.Any(l => string.Equals(l.MaVachLo, lo.MaVachLo, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Mã vạch lô đã tồn tại.");
            lo.MaLo = ++_maLo;
            _lo.Add(lo);
            return Task.FromResult(lo);
        }
    }
}
