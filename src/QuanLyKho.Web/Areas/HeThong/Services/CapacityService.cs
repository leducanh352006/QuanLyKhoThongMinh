using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Một dòng hàng đã được duyệt xếp vào ô nhưng chưa quét xác nhận (đã chiếm chỗ, chưa có trong TonKho).</summary>
public sealed record DongChoXep(int MaBT, int SoLuong);

/// <summary>
/// Nguồn hàng "đã duyệt xếp vào ô nhưng chưa quét xác nhận" (phần thứ hai của Vđ, Kđ trong Bảng 2.3 dòng 3, 4).
/// Dữ liệu này thuộc luồng xếp hàng G2 của Nhóm 2 (NhapKho). Khi nhóm đó có bản thật, đăng ký
/// <c>services.AddScoped&lt;INguonHangChoXep, ...&gt;()</c> trong module Area của mình (đăng ký sau cùng thắng); không cần sửa HeThong.
/// </summary>
public interface INguonHangChoXep
{
    Task<IReadOnlyList<DongChoXep>> LayTheoO(int maViTri, CancellationToken ct = default);
}

/// <summary>Bản mặc định: chưa có hàng chờ xếp nào (giả định cho tới khi Nhóm 2 cung cấp nguồn thật).</summary>
public sealed class KhongCoHangChoXep : INguonHangChoXep
{
    public Task<IReadOnlyList<DongChoXep>> LayTheoO(int maViTri, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DongChoXep>>(Array.Empty<DongChoXep>());
}

/// <summary>
/// Bản thật của <see cref="ICapacityService"/> (việc T4-5), thay <see cref="Contracts.Stubs.CapacityServiceStub"/> mà không đổi interface.
/// Vđ, Kđ = hàng đang nằm trong ô (TonKho) + hàng đã duyệt xếp vào ô nhưng chưa quét (<see cref="INguonHangChoXep"/>).
/// Phép tính nằm ở <see cref="QuyTacSucChua"/>.
/// </summary>
public sealed class CapacityService(KhoDbContext db, INguonHangChoXep choXep) : ICapacityService
{
    public async Task<KetQuaSucChua> KiemTra(int maViTri, int maBT, int soLuong, CancellationToken ct = default)
    {
        var o = await db.ViTri.AsNoTracking().FirstOrDefaultAsync(v => v.MaViTri == maViTri, ct);
        if (o is null)
            return new KetQuaSucChua(false, 0, 0, 0, $"Không tìm thấy vị trí {maViTri}.", MaLoiDichVu.KhongTimThay);

        var bt = await db.BienThe.AsNoTracking().FirstOrDefaultAsync(b => b.MaBT == maBT, ct);
        if (bt is null)
            return new KetQuaSucChua(false, 0, 0, 0, $"Không tìm thấy biến thể {maBT}.", MaLoiDichVu.KhongTimThay);

        decimal theTichDaDung = 0, khoiDaDung = 0;

        var tonTrongO = await (
            from t in db.TonKho.AsNoTracking()
            where t.MaViTri == maViTri && t.SoLuongTon > 0
            join l in db.LoHang.AsNoTracking() on t.MaLo equals l.MaLo
            join b in db.BienThe.AsNoTracking() on l.MaBT equals b.MaBT
            select new { t.SoLuongTon, b.Dai, b.Rong, b.Cao, b.KhoiLuong }).ToListAsync(ct);
        foreach (var x in tonTrongO)
        {
            theTichDaDung += x.SoLuongTon * x.Dai * x.Rong * x.Cao;
            khoiDaDung += x.SoLuongTon * x.KhoiLuong;
        }

        var dongCho = await choXep.LayTheoO(maViTri, ct);
        if (dongCho.Count > 0)
        {
            var maBTCho = dongCho.Select(d => d.MaBT).Distinct().ToList();
            var btCho = await db.BienThe.AsNoTracking().Where(b => maBTCho.Contains(b.MaBT)).ToDictionaryAsync(b => b.MaBT, ct);
            foreach (var d in dongCho)
            {
                if (!btCho.TryGetValue(d.MaBT, out var b)) continue;
                theTichDaDung += d.SoLuong * b.TheTichCm3;
                khoiDaDung += d.SoLuong * b.KhoiLuong;
            }
        }

        return QuyTacSucChua.Tinh(o, bt.TheTichCm3, bt.KhoiLuong, soLuong, theTichDaDung, khoiDaDung);
    }
}
