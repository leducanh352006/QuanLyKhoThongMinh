using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Điều kiện lọc màn hình nhật ký (báo cáo F10: lọc theo người, loại thao tác, thời gian). Mọi điều kiện đều tùy chọn; thời gian là UTC, tính cả hai đầu.</summary>
public sealed record NhatKyLoc(
    int? MaND = null, string? HanhDong = null, LoaiChungTu? LoaiCT = null, int? MaCT = null,
    DateTime? TuUtc = null, DateTime? DenUtc = null, int Trang = 1, int CoTrang = NhatKyLoc.CoTrangMacDinh)
{
    public const int CoTrangMacDinh = 50;
    public const int CoTrangToiDa = 200;
}

/// <summary>
/// Cổng truy cập dữ liệu NhatKyThaoTac (việc T4-7), cùng kiểu với <see cref="IKhoViTriStore"/>: bản EF Core (SQL Server) và bản trong bộ nhớ
/// (chỉ Development khi chưa có chuỗi kết nối, và kiểm thử). Nhật ký chỉ thêm, không có hàm sửa hay xóa.
/// </summary>
public interface INhatKyStore
{
    Task Them(NhatKyThaoTac dong, CancellationToken ct = default);

    /// <summary>Lịch sử một chứng từ, cũ trước mới sau.</summary>
    Task<IReadOnlyList<NhatKyThaoTac>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default);

    /// <summary>Mới trước cũ sau; <paramref name="Tong"/> là số dòng khớp bộ lọc (chưa phân trang).</summary>
    Task<(IReadOnlyList<NhatKyThaoTac> Dong, int Tong)> Tim(NhatKyLoc loc, CancellationToken ct = default);
}

public sealed class EfNhatKyStore(KhoDbContext db) : INhatKyStore
{
    public async Task Them(NhatKyThaoTac dong, CancellationToken ct = default)
    {
        db.NhatKyThaoTac.Add(dong);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NhatKyThaoTac>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default) =>
        await db.NhatKyThaoTac.AsNoTracking().Where(n => n.LoaiCT == loaiCT && n.MaCT == maCT).OrderBy(n => n.MaNK).ToListAsync(ct);

    public async Task<(IReadOnlyList<NhatKyThaoTac> Dong, int Tong)> Tim(NhatKyLoc loc, CancellationToken ct = default)
    {
        var q = db.NhatKyThaoTac.AsNoTracking().AsQueryable();
        if (loc.MaND is int nd) q = q.Where(n => n.MaND == nd);
        if (!string.IsNullOrWhiteSpace(loc.HanhDong)) { var hd = loc.HanhDong.Trim(); q = q.Where(n => n.HanhDong.Contains(hd)); }
        if (loc.LoaiCT is LoaiChungTu l) { var s = l.ToCsdl(); q = q.Where(n => n.LoaiCT == s); }
        if (loc.MaCT is int ma) q = q.Where(n => n.MaCT == ma);
        if (loc.TuUtc is DateTime tu) q = q.Where(n => n.ThoiGian >= tu);
        if (loc.DenUtc is DateTime den) q = q.Where(n => n.ThoiGian <= den);

        var tong = await q.CountAsync(ct);
        var (bo, lay) = NhatKyService.PhanTrang(loc);
        var dong = await q.OrderByDescending(n => n.MaNK).Skip(bo).Take(lay).ToListAsync(ct);
        return (dong, tong);
    }
}

/// <summary>Bản trong bộ nhớ của <see cref="INhatKyStore"/>. Mất dữ liệu khi tắt ứng dụng.</summary>
public sealed class BoNhoNhatKyStore : INhatKyStore
{
    private readonly object _khoa = new();
    private readonly List<NhatKyThaoTac> _dong = new();
    private long _maNK;

    public Task Them(NhatKyThaoTac dong, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            dong.MaNK = ++_maNK;
            _dong.Add(dong);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NhatKyThaoTac>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            IReadOnlyList<NhatKyThaoTac> kq = _dong.Where(n => n.LoaiCT == loaiCT && n.MaCT == maCT).OrderBy(n => n.MaNK).ToList();
            return Task.FromResult(kq);
        }
    }

    public Task<(IReadOnlyList<NhatKyThaoTac> Dong, int Tong)> Tim(NhatKyLoc loc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var q = _dong.AsEnumerable();
            if (loc.MaND is int nd) q = q.Where(n => n.MaND == nd);
            if (!string.IsNullOrWhiteSpace(loc.HanhDong)) { var hd = loc.HanhDong.Trim(); q = q.Where(n => n.HanhDong.Contains(hd, StringComparison.OrdinalIgnoreCase)); }
            if (loc.LoaiCT is LoaiChungTu l) { var s = l.ToCsdl(); q = q.Where(n => n.LoaiCT == s); }
            if (loc.MaCT is int ma) q = q.Where(n => n.MaCT == ma);
            if (loc.TuUtc is DateTime tu) q = q.Where(n => n.ThoiGian >= tu);
            if (loc.DenUtc is DateTime den) q = q.Where(n => n.ThoiGian <= den);

            var danhSach = q.OrderByDescending(n => n.MaNK).ToList();
            var (bo, lay) = NhatKyService.PhanTrang(loc);
            return Task.FromResult<(IReadOnlyList<NhatKyThaoTac>, int)>((danhSach.Skip(bo).Take(lay).ToList(), danhSach.Count));
        }
    }
}

/// <summary>
/// Bản thật của <see cref="IAuditService"/> (việc T4-7), thay <see cref="Contracts.Stubs.AuditServiceStub"/> mà không đổi interface.
/// Ghi vào cùng <c>KhoDbContext</c> của nơi gọi nên tham gia giao dịch của nơi gọi: nghiệp vụ hoàn tác thì dòng nhật ký cũng hoàn tác.
/// </summary>
public sealed class AuditService(INhatKyStore store, TimeProvider gio) : IAuditService
{
    public const int DoDaiToiDaHanhDong = 100;
    public const int DoDaiToiDaChiTiet = 1000;

    public async Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
    {
        if (loaiCT.HasValue != maCT.HasValue)
            throw new ArgumentException("LoaiCT và MaCT phải cùng có giá trị hoặc cùng null (Bảng NhatKyThaoTac).");
        if (string.IsNullOrWhiteSpace(hanhDong))
            throw new ArgumentException("Hành động không được để trống.", nameof(hanhDong));
        hanhDong = hanhDong.Trim();
        if (hanhDong.Length > DoDaiToiDaHanhDong)
            throw new ArgumentException($"Hành động tối đa {DoDaiToiDaHanhDong} ký tự.", nameof(hanhDong));
        if (chiTiet is { Length: > DoDaiToiDaChiTiet }) chiTiet = chiTiet[..DoDaiToiDaChiTiet];   // nhật ký không được làm hỏng nghiệp vụ chỉ vì chi tiết dài

        await store.Them(new NhatKyThaoTac
        {
            MaND = maND, ThoiGian = gio.GetUtcNow().UtcDateTime, HanhDong = hanhDong,
            LoaiCT = loaiCT?.ToCsdl(), MaCT = maCT, ChiTiet = chiTiet,
        }, ct);
    }

    public async Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default) =>
        (await store.LayTheoChungTu(loaiCT.ToCsdl(), maCT, ct))
            .Select(n => new NhatKyDto(n.MaNK, n.MaND, n.ThoiGian, n.HanhDong, loaiCT, n.MaCT, n.ChiTiet)).ToList();
}

/// <summary>Một dòng nhật ký để hiển thị: kèm tên người thực hiện.</summary>
public sealed record NhatKyDong(
    long MaNK, DateTime ThoiGianUtc, int MaND, string NguoiThucHien, string HanhDong, string? LoaiCT, int? MaCT, string? ChiTiet);

public sealed record NhatKyTrang(IReadOnlyList<NhatKyDong> Dong, int Tong, int Trang, int CoTrang)
{
    public int TongTrang => CoTrang <= 0 ? 0 : (Tong + CoTrang - 1) / CoTrang;
}

/// <summary>Tra cứu nhật ký cho màn hình của Quản trị viên (báo cáo F10).</summary>
public interface INhatKyQuanLy
{
    Task<NhatKyTrang> Tim(NhatKyLoc loc, CancellationToken ct = default);
}

public sealed class NhatKyService(INhatKyStore store, IHeThongStore nguoiDung) : INhatKyQuanLy
{
    public async Task<NhatKyTrang> Tim(NhatKyLoc loc, CancellationToken ct = default)
    {
        var trang = Math.Max(loc.Trang, 1);
        var coTrang = Math.Clamp(loc.CoTrang, 1, NhatKyLoc.CoTrangToiDa);
        loc = loc with { Trang = trang, CoTrang = coTrang };

        var (dong, tong) = await store.Tim(loc, ct);
        var ten = (await nguoiDung.LayNguoiDung(ct)).ToDictionary(n => n.MaND, n => n.HoTen);
        var kq = dong.Select(n => new NhatKyDong(
            n.MaNK, n.ThoiGian, n.MaND, ten.TryGetValue(n.MaND, out var t) ? t : $"Người dùng {n.MaND}",
            n.HanhDong, n.LoaiCT, n.MaCT, n.ChiTiet)).ToList();
        return new NhatKyTrang(kq, tong, trang, coTrang);
    }

    /// <summary>Số dòng bỏ qua và số dòng lấy, đã chặn giá trị sai (trang &lt; 1, cỡ trang quá lớn).</summary>
    public static (int Bo, int Lay) PhanTrang(NhatKyLoc loc)
    {
        var trang = Math.Max(loc.Trang, 1);
        var co = Math.Clamp(loc.CoTrang, 1, NhatKyLoc.CoTrangToiDa);
        return ((trang - 1) * co, co);
    }
}
