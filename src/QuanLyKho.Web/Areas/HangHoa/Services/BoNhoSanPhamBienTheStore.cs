using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Kho dữ liệu SanPham, BienThe trong bộ nhớ, nạp sẵn từ <see cref="SeedDataSet"/>.
/// Dùng khi chưa cấu hình SQL Server ở môi trường Development và trong kiểm thử. Dữ liệu mất khi tắt ứng dụng.
/// </summary>
public sealed class BoNhoSanPhamBienTheStore : ISanPhamBienTheStore
{
    private readonly object _khoa = new();
    private readonly List<SanPham> _sanPham = new();
    private readonly List<BienThe> _bienThe = new();
    private int _maSP;
    private int _maBT;

    public BoNhoSanPhamBienTheStore()
    {
        foreach (var s in SeedDataSet.SanPham)
        {
            _sanPham.Add(new SanPham { MaSP = s.MaSP, TenSP = s.TenSP, MaDM = s.MaDM, DonViTinh = s.DonViTinh, CoHanDung = s.CoHanDung });
            _maSP = Math.Max(_maSP, s.MaSP);
        }

        foreach (var b in SeedDataSet.BienThe)
        {
            _bienThe.Add(new BienThe { MaBT = b.MaBT, MaSP = b.MaSP, MaVach = b.MaVach, ThuocTinh = b.ThuocTinh, Dai = b.Dai, Rong = b.Rong, Cao = b.Cao, KhoiLuong = b.KhoiLuong });
            _maBT = Math.Max(_maBT, b.MaBT);
        }
    }

    public Task<IReadOnlyList<SanPham>> LaySanPham(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<SanPham>>(_sanPham.OrderBy(s => s.TenSP).ToList());
    }

    public Task<SanPham?> TimSanPham(int maSP, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_sanPham.FirstOrDefault(s => s.MaSP == maSP));
    }

    public Task<SanPham> ThemSanPham(SanPham sp, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            sp.MaSP = ++_maSP;
            _sanPham.Add(sp);
            return Task.FromResult(sp);
        }
    }

    public Task<IReadOnlyList<BienThe>> LayBienTheTheoSanPham(int maSP, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<BienThe>>(_bienThe.Where(b => b.MaSP == maSP).OrderBy(b => b.ThuocTinh).ToList());
    }

    public Task<BienThe?> TimBienThe(int maBT, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_bienThe.FirstOrDefault(b => b.MaBT == maBT));
    }

    public Task<BienThe> ThemBienThe(BienThe bt, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_bienThe.Any(b => string.Equals(b.MaVach, bt.MaVach, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Mã vạch đã tồn tại.");
            bt.MaBT = ++_maBT;
            _bienThe.Add(bt);
            return Task.FromResult(bt);
        }
    }

    public Task LuuThayDoi(CancellationToken ct = default) => Task.CompletedTask;
}
