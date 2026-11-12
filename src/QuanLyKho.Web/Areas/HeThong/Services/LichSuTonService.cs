using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Điều kiện lọc màn hình lịch sử biến động tồn (việc T4-6): theo lô, ô, loại biến động, loại chứng từ và thời gian.
/// Mọi điều kiện đều tùy chọn; thời gian là UTC, tính cả hai đầu.
/// </summary>
public sealed record LichSuTonLoc(
    int? MaLo = null, int? MaViTri = null, string? LoaiBienDong = null, LoaiChungTu? LoaiCT = null, int? MaCT = null,
    DateTime? TuUtc = null, DateTime? DenUtc = null, int Trang = 1, int CoTrang = LichSuTonLoc.CoTrangMacDinh)
{
    public const int CoTrangMacDinh = 50;
    public const int CoTrangToiDa = 200;
}

/// <summary>Một dòng sổ cái kèm mã vạch lô và tên ô (chưa có tên người thực hiện).</summary>
public sealed record LichSuTonThanh(
    long MaBD, DateTime ThoiGianUtc, int MaLo, string MaVachLo, int MaViTri, string TenViTri,
    int SoLuong, string LoaiBienDong, string LoaiCT, int MaCT, int MaND);

/// <summary>Một dòng lịch sử để hiển thị. SoLuong dương là tăng, âm là giảm.</summary>
public sealed record LichSuTonDong(
    long MaBD, DateTime ThoiGianUtc, int MaLo, string MaVachLo, int MaViTri, string TenViTri,
    int SoLuong, string LoaiBienDong, string LoaiCT, int MaCT, int MaND, string NguoiThucHien);

public sealed record LichSuTonTrang(IReadOnlyList<LichSuTonDong> Dong, int Tong, int Trang, int CoTrang)
{
    public int TongTrang => CoTrang <= 0 ? 0 : (Tong + CoTrang - 1) / CoTrang;
}

/// <summary>Một dòng xuất thô (chưa gom ngày): biến thể và kho lấy qua lô và ô của dòng sổ cái.</summary>
public sealed record XuatThanh(DateTime ThoiGianUtc, int MaBT, int MaKho, int SoLuong);

/// <summary>Kết quả đối chiếu sổ cái với TonKho: <see cref="Khop"/> khi tổng sổ cái bằng tồn ở mọi (lô, ô).</summary>
public sealed record KetQuaDoiChieu(IReadOnlyList<ChenhLechSoCai> Chenh, DateTime ThoiGianUtc)
{
    public bool Khop => Chenh.Count == 0;
}

/// <summary>
/// Cổng đọc BienDongTon (việc T4-6), cùng kiểu với <see cref="INhatKyStore"/>: bản EF Core (SQL Server) và bản trong bộ nhớ
/// (chỉ Development khi chưa có chuỗi kết nối, và kiểm thử). Chỉ đọc: sổ cái chỉ được ghi qua <see cref="IStockService"/>.
/// </summary>
public interface ILichSuTonStore
{
    /// <summary>Mới trước cũ sau; <c>Tong</c> là số dòng khớp bộ lọc (chưa phân trang).</summary>
    Task<(IReadOnlyList<LichSuTonThanh> Dong, int Tong)> Tim(LichSuTonLoc loc, CancellationToken ct = default);

    /// <summary>Các dòng sổ cái loại Xuất kho trong khoảng UTC (tính cả hai đầu), kèm biến thể và kho; lọc thêm theo kho, biến thể nếu có.</summary>
    Task<IReadOnlyList<XuatThanh>> LayXuat(DateTime tuUtc, DateTime denUtc, int? maKho, int? maBT, CancellationToken ct = default);
}

public sealed class EfLichSuTonStore(KhoDbContext db) : ILichSuTonStore
{
    public async Task<(IReadOnlyList<LichSuTonThanh> Dong, int Tong)> Tim(LichSuTonLoc loc, CancellationToken ct = default)
    {
        var q = db.BienDongTon.AsNoTracking().AsQueryable();
        if (loc.MaLo is int lo) q = q.Where(b => b.MaLo == lo);
        if (loc.MaViTri is int vt) q = q.Where(b => b.MaViTri == vt);
        if (!string.IsNullOrWhiteSpace(loc.LoaiBienDong)) { var l = loc.LoaiBienDong.Trim(); q = q.Where(b => b.LoaiBienDong == l); }
        if (loc.LoaiCT is LoaiChungTu ctLoai) { var s = ctLoai.ToCsdl(); q = q.Where(b => b.LoaiCT == s); }
        if (loc.MaCT is int ma) q = q.Where(b => b.MaCT == ma);
        if (loc.TuUtc is DateTime tu) q = q.Where(b => b.ThoiGianUtc >= tu);
        if (loc.DenUtc is DateTime den) q = q.Where(b => b.ThoiGianUtc <= den);

        var tong = await q.CountAsync(ct);
        var (bo, lay) = LichSuTonService.PhanTrang(loc);
        var dong = await (from b in q
                          join l in db.LoHang.AsNoTracking() on b.MaLo equals l.MaLo
                          join v in db.ViTri.AsNoTracking() on b.MaViTri equals v.MaViTri
                          orderby b.MaBD descending
                          select new LichSuTonThanh(b.MaBD, b.ThoiGianUtc, b.MaLo, l.MaVachLo, b.MaViTri, v.TenViTri,
                              b.SoLuong, b.LoaiBienDong, b.LoaiCT, b.MaCT, b.MaND))
            .Skip(bo).Take(lay).ToListAsync(ct);
        return (dong, tong);
    }

    public async Task<IReadOnlyList<XuatThanh>> LayXuat(DateTime tuUtc, DateTime denUtc, int? maKho, int? maBT, CancellationToken ct = default)
    {
        var xuat = LoaiBienDongGiaTri.XuatKho;
        var q = from b in db.BienDongTon.AsNoTracking()
                where b.LoaiBienDong == xuat && b.ThoiGianUtc >= tuUtc && b.ThoiGianUtc <= denUtc
                join l in db.LoHang.AsNoTracking() on b.MaLo equals l.MaLo
                join v in db.ViTri.AsNoTracking() on b.MaViTri equals v.MaViTri
                select new { b.ThoiGianUtc, l.MaBT, v.MaKho, b.SoLuong };
        if (maKho is int kho) q = q.Where(x => x.MaKho == kho);
        if (maBT is int bt) q = q.Where(x => x.MaBT == bt);

        var ds = await q.ToListAsync(ct);
        return ds.Select(x => new XuatThanh(x.ThoiGianUtc, x.MaBT, x.MaKho, x.SoLuong)).ToList();
    }
}

/// <summary>
/// Bản trong bộ nhớ của <see cref="ILichSuTonStore"/>: đọc sổ cái của <see cref="StockServiceStub"/> (không cần SQL Server).
/// Mã vạch lô, tên ô, biến thể và kho lấy từ <see cref="SeedDataSet"/>. Mất dữ liệu khi tắt ứng dụng.
/// </summary>
public sealed class BoNhoLichSuTonStore(StockServiceStub stock) : ILichSuTonStore
{
    public Task<(IReadOnlyList<LichSuTonThanh> Dong, int Tong)> Tim(LichSuTonLoc loc, CancellationToken ct = default)
    {
        var q = stock.SoCai.AsEnumerable();
        if (loc.MaLo is int lo) q = q.Where(b => b.MaLo == lo);
        if (loc.MaViTri is int vt) q = q.Where(b => b.MaViTri == vt);
        if (!string.IsNullOrWhiteSpace(loc.LoaiBienDong)) { var l = loc.LoaiBienDong.Trim(); q = q.Where(b => b.LoaiBienDong.ToCsdl() == l); }
        if (loc.LoaiCT is LoaiChungTu ctLoai) q = q.Where(b => b.LoaiCT == ctLoai);
        if (loc.MaCT is int ma) q = q.Where(b => b.MaCT == ma);
        if (loc.TuUtc is DateTime tu) q = q.Where(b => b.ThoiGianUtc >= tu);
        if (loc.DenUtc is DateTime den) q = q.Where(b => b.ThoiGianUtc <= den);

        var ds = q.OrderByDescending(b => b.MaBD).ToList();
        var (bo, lay) = LichSuTonService.PhanTrang(loc);
        IReadOnlyList<LichSuTonThanh> dong = ds.Skip(bo).Take(lay).Select(b => new LichSuTonThanh(
            b.MaBD, b.ThoiGianUtc, b.MaLo, SeedDataSet.TimLo(b.MaLo)?.MaVachLo ?? "", b.MaViTri, SeedDataSet.TimViTri(b.MaViTri)?.TenViTri ?? "",
            b.SoLuong, b.LoaiBienDong.ToCsdl(), b.LoaiCT.ToCsdl(), b.MaCT, b.MaND)).ToList();
        return Task.FromResult((dong, ds.Count));
    }

    public Task<IReadOnlyList<XuatThanh>> LayXuat(DateTime tuUtc, DateTime denUtc, int? maKho, int? maBT, CancellationToken ct = default)
    {
        var ds = new List<XuatThanh>();
        foreach (var b in stock.SoCai.Where(b => b.LoaiBienDong == LoaiBienDong.XuatKho && b.ThoiGianUtc >= tuUtc && b.ThoiGianUtc <= denUtc))
        {
            var lo = SeedDataSet.TimLo(b.MaLo);
            var vt = SeedDataSet.TimViTri(b.MaViTri);
            if (lo is null || vt is null) continue;
            if (maKho is int kho && vt.MaKho != kho) continue;
            if (maBT is int bt && lo.MaBT != bt) continue;
            ds.Add(new XuatThanh(b.ThoiGianUtc, lo.MaBT, vt.MaKho, b.SoLuong));
        }
        return Task.FromResult<IReadOnlyList<XuatThanh>>(ds);
    }
}

/// <summary>Tra cứu lịch sử biến động tồn và đối chiếu sổ cái cho màn hình của Quản trị viên, Quản lý kho (việc T4-6).</summary>
public interface ILichSuTonQuanLy
{
    Task<LichSuTonTrang> Tim(LichSuTonLoc loc, CancellationToken ct = default);

    /// <summary>Đối chiếu TonKho với tồn tính lại từ sổ cái. Dùng lại <see cref="IStockService.DoiChieuSoCai"/> của T4-4, không tự tính lần nữa.</summary>
    Task<KetQuaDoiChieu> DoiChieu(CancellationToken ct = default);
}

/// <summary>
/// Việc T4-6: lịch sử biến động tồn. Đọc sổ cái BienDongTon mà <see cref="IStockService"/> (T4-4) ghi; không bao giờ ghi hay sửa sổ cái.
/// Đồng thời cung cấp <see cref="ILichSuTonService"/> (hiện chưa có nơi dùng sau khi bỏ G3, G4).
/// </summary>
public sealed class LichSuTonService(ILichSuTonStore store, IStockService stock, IHeThongStore nguoiDung, TimeProvider gio)
    : ILichSuTonQuanLy, ILichSuTonService
{
    public async Task<LichSuTonTrang> Tim(LichSuTonLoc loc, CancellationToken ct = default)
    {
        var trang = Math.Max(loc.Trang, 1);
        var coTrang = Math.Clamp(loc.CoTrang, 1, LichSuTonLoc.CoTrangToiDa);
        loc = loc with { Trang = trang, CoTrang = coTrang };

        var (dong, tong) = await store.Tim(loc, ct);
        var ten = (await nguoiDung.LayNguoiDung(ct)).ToDictionary(n => n.MaND, n => n.HoTen);
        var kq = dong.Select(d => new LichSuTonDong(
            d.MaBD, d.ThoiGianUtc, d.MaLo, d.MaVachLo, d.MaViTri, d.TenViTri, d.SoLuong, d.LoaiBienDong, d.LoaiCT, d.MaCT, d.MaND,
            ten.TryGetValue(d.MaND, out var t) ? t : $"Người dùng {d.MaND}")).ToList();
        return new LichSuTonTrang(kq, tong, trang, coTrang);
    }

    public async Task<KetQuaDoiChieu> DoiChieu(CancellationToken ct = default) =>
        new(await stock.DoiChieuSoCai(ct), gio.GetUtcNow().UtcDateTime);

    public async Task<IReadOnlyList<XuatTheoNgayDto>> LichSuXuatTheoNgay(
        DateOnly tuNgay, DateOnly denNgay, int? maKho = null, int? maBT = null, CancellationToken ct = default)
    {
        if (tuNgay > denNgay) return Array.Empty<XuatTheoNgayDto>();

        var muiGio = gio.LocalTimeZone;
        var tuUtc = DauNgayUtc(tuNgay, muiGio);
        var denUtc = DauNgayUtc(denNgay.AddDays(1), muiGio).AddTicks(-1);
        return TongHopXuatTheoNgay(await store.LayXuat(tuUtc, denUtc, maKho, maBT, ct), muiGio);
    }

    /// <summary>
    /// Gom các dòng xuất theo (ngày giờ máy chủ, biến thể, kho). Dòng sổ cái xuất mang dấu âm nên lượng xuất là phần đối của tổng;
    /// nhóm có tổng không âm (không có xuất ròng) bị bỏ để kết quả luôn dương.
    /// </summary>
    public static IReadOnlyList<XuatTheoNgayDto> TongHopXuatTheoNgay(IEnumerable<XuatThanh> dong, TimeZoneInfo muiGio) =>
        dong.GroupBy(x => (Ngay: NgayCuc(x.ThoiGianUtc, muiGio), x.MaBT, x.MaKho))
            .Select(g => new XuatTheoNgayDto(g.Key.Ngay, g.Key.MaBT, g.Key.MaKho, -g.Sum(x => x.SoLuong)))
            .Where(x => x.SoLuong > 0)
            .OrderBy(x => x.Ngay).ThenBy(x => x.MaKho).ThenBy(x => x.MaBT)
            .ToList();

    /// <summary>Số dòng bỏ qua và số dòng lấy, đã chặn giá trị sai (trang &lt; 1, cỡ trang quá lớn).</summary>
    public static (int Bo, int Lay) PhanTrang(LichSuTonLoc loc)
    {
        var trang = Math.Max(loc.Trang, 1);
        var co = Math.Clamp(loc.CoTrang, 1, LichSuTonLoc.CoTrangToiDa);
        return ((trang - 1) * co, co);
    }

    private static DateOnly NgayCuc(DateTime utc, TimeZoneInfo muiGio) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), muiGio));

    private static DateTime DauNgayUtc(DateOnly ngay, TimeZoneInfo muiGio) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), muiGio);
}
