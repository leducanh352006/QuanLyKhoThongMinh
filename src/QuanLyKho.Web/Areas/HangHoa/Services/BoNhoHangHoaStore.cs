using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Kho dữ liệu NhaCungCap, DanhMuc trong bộ nhớ, nạp sẵn từ <see cref="SeedDataSet"/>.
/// Dùng khi chưa cấu hình SQL Server ở môi trường Development và trong kiểm thử. Dữ liệu mất khi tắt ứng dụng.
/// </summary>
public sealed class BoNhoHangHoaStore : IHangHoaStore
{
    private readonly object _khoa = new();
    private readonly List<NhaCungCap> _nhaCungCap = new();
    private readonly List<DanhMuc> _danhMuc = new();
    private int _maNCC;
    private int _maDM;

    public BoNhoHangHoaStore()
    {
        foreach (var n in SeedDataSet.NhaCungCap)
        {
            _nhaCungCap.Add(new NhaCungCap { MaNCC = n.MaNCC, TenNCC = n.TenNCC, SDT = n.SDT, DiaChi = n.DiaChi, TrangThai = TrangThaiNhaCungCap.HoatDong });
            _maNCC = Math.Max(_maNCC, n.MaNCC);
        }

        foreach (var d in SeedDataSet.DanhMuc)
        {
            _danhMuc.Add(new DanhMuc { MaDM = d.MaDM, TenDM = d.TenDM });
            _maDM = Math.Max(_maDM, d.MaDM);
        }
    }

    public Task<IReadOnlyList<NhaCungCap>> LayNhaCungCap(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<NhaCungCap>>(_nhaCungCap.OrderBy(n => n.TenNCC).ToList());
    }

    public Task<NhaCungCap?> TimNhaCungCap(int maNCC, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_nhaCungCap.FirstOrDefault(n => n.MaNCC == maNCC));
    }

    public Task<NhaCungCap> ThemNhaCungCap(NhaCungCap ncc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_nhaCungCap.Any(n => string.Equals(n.TenNCC, ncc.TenNCC, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Tên nhà cung cấp đã tồn tại.");
            ncc.MaNCC = ++_maNCC;
            _nhaCungCap.Add(ncc);
            return Task.FromResult(ncc);
        }
    }

    public Task<IReadOnlyList<DanhMuc>> LayDanhMuc(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<DanhMuc>>(_danhMuc.OrderBy(d => d.TenDM).ToList());
    }

    public Task<DanhMuc?> TimDanhMuc(int maDM, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_danhMuc.FirstOrDefault(d => d.MaDM == maDM));
    }

    public Task<DanhMuc> ThemDanhMuc(DanhMuc danhMuc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_danhMuc.Any(d => string.Equals(d.TenDM, danhMuc.TenDM, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Tên danh mục đã tồn tại.");
            danhMuc.MaDM = ++_maDM;
            _danhMuc.Add(danhMuc);
            return Task.FromResult(danhMuc);
        }
    }

    public Task XoaDanhMuc(DanhMuc danhMuc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (SeedDataSet.SanPham.Any(s => s.MaDM == danhMuc.MaDM))
                throw new XungDotDuLieuException("Danh mục còn sản phẩm tham chiếu, không xóa được.");
            _danhMuc.RemoveAll(d => d.MaDM == danhMuc.MaDM);
            return Task.CompletedTask;
        }
    }

    public Task LuuThayDoi(CancellationToken ct = default) => Task.CompletedTask;
}
