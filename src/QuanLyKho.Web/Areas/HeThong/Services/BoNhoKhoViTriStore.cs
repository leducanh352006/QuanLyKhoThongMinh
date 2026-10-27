using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Kho dữ liệu Kho, ViTri trong bộ nhớ, nạp sẵn từ <see cref="SeedDataSet"/> (2 kho, cây vị trí mẫu).
/// Dùng khi chưa cấu hình SQL Server ở môi trường Development và trong kiểm thử. Dữ liệu mất khi tắt ứng dụng.
/// </summary>
public sealed class BoNhoKhoViTriStore : IKhoViTriStore
{
    private readonly object _khoa = new();
    private readonly List<Kho> _kho = new();
    private readonly List<ViTri> _viTri = new();
    private int _maKho;
    private int _maViTri;

    public BoNhoKhoViTriStore()
    {
        foreach (var k in SeedDataSet.Kho)
        {
            _kho.Add(new Kho
            {
                MaKho = k.MaKho, TenKho = k.TenKho, LoaiKho = k.LoaiKho, DiaChi = k.DiaChi,
                MaQuanLy = k.MaQuanLy, ThoiGianCho = k.ThoiGianCho, TrangThai = TrangThaiHoatDong.HoatDong,
            });
            _maKho = Math.Max(_maKho, k.MaKho);
        }

        foreach (var v in SeedDataSet.ViTri)
        {
            _viTri.Add(new ViTri
            {
                MaViTri = v.MaViTri, TenViTri = v.TenViTri, MaKho = v.MaKho, MaViTriCha = v.MaViTriCha, CapViTri = v.CapViTri,
                LoaiViTri = v.LoaiViTri, MaVachO = v.MaVachO, Dai = v.Dai, Rong = v.Rong, Cao = v.Cao,
                KhoiLuongToiDa = v.KhoiLuongToiDa, DangKhoa = false, TrangThai = TrangThaiHoatDong.HoatDong,
            });
            _maViTri = Math.Max(_maViTri, v.MaViTri);
        }
    }

    public Task<IReadOnlyList<Kho>> LayKho(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<Kho>>(_kho.OrderBy(k => k.TenKho).ToList());
    }

    public Task<Kho?> TimKho(int maKho, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_kho.FirstOrDefault(k => k.MaKho == maKho));
    }

    public Task<Kho> ThemKho(Kho kho, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_kho.Any(k => string.Equals(k.TenKho, kho.TenKho, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Tên kho đã tồn tại.");
            kho.MaKho = ++_maKho;
            _kho.Add(kho);
            return Task.FromResult(kho);
        }
    }

    public Task<IReadOnlyList<ViTri>> LayViTriTheoKho(int maKho, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<ViTri>>(_viTri.Where(v => v.MaKho == maKho).OrderBy(v => v.TenViTri).ToList());
    }

    public Task<ViTri?> TimViTri(int maViTri, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_viTri.FirstOrDefault(v => v.MaViTri == maViTri));
    }

    public Task<ViTri> ThemViTri(ViTri viTri, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_viTri.Any(v => v.MaKho == viTri.MaKho && v.MaViTriCha == viTri.MaViTriCha
                    && string.Equals(v.TenViTri, viTri.TenViTri, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Tên vị trí đã tồn tại trong cùng cấp cha.");
            if (viTri.MaVachO is not null && _viTri.Any(v => string.Equals(v.MaVachO, viTri.MaVachO, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Mã vạch ô đã dùng.");
            viTri.MaViTri = ++_maViTri;
            _viTri.Add(viTri);
            return Task.FromResult(viTri);
        }
    }

    public Task<int> DemOTiepNhanHoatDong(int maKho, int? botMaViTri, CancellationToken ct = default)
    {
        lock (_khoa)
            return Task.FromResult(_viTri.Count(v => v.MaKho == maKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan
                && v.DangHoatDong && v.MaViTri != botMaViTri));
    }

    public Task<int> DemConHoatDong(int maViTriCha, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_viTri.Count(v => v.MaViTriCha == maViTriCha && v.DangHoatDong));
    }

    public Task LuuThayDoi(CancellationToken ct = default) => Task.CompletedTask;

    public Task XoaViTri(ViTri viTri, CancellationToken ct = default)
    {
        lock (_khoa) { _viTri.RemoveAll(v => v.MaViTri == viTri.MaViTri); }
        return Task.CompletedTask;
    }
}
