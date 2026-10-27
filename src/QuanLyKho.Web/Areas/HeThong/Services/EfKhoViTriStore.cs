using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Bản thật của <see cref="IKhoViTriStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfKhoViTriStore(KhoDbContext db) : IKhoViTriStore
{
    public async Task<IReadOnlyList<Kho>> LayKho(CancellationToken ct = default)
        => await db.Kho.AsNoTracking().OrderBy(k => k.TenKho).ToListAsync(ct);

    public Task<Kho?> TimKho(int maKho, CancellationToken ct = default)
        => db.Kho.FirstOrDefaultAsync(k => k.MaKho == maKho, ct);

    public async Task<Kho> ThemKho(Kho kho, CancellationToken ct = default)
    {
        db.Kho.Add(kho);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(kho).State = EntityState.Detached;
            throw new XungDotDuLieuException("Tên kho đã tồn tại.", ex);
        }
        return kho;
    }

    public async Task<IReadOnlyList<ViTri>> LayViTriTheoKho(int maKho, CancellationToken ct = default)
        => await db.ViTri.AsNoTracking().Where(v => v.MaKho == maKho).OrderBy(v => v.TenViTri).ToListAsync(ct);

    public Task<ViTri?> TimViTri(int maViTri, CancellationToken ct = default)
        => db.ViTri.FirstOrDefaultAsync(v => v.MaViTri == maViTri, ct);

    public async Task<ViTri> ThemViTri(ViTri viTri, CancellationToken ct = default)
    {
        db.ViTri.Add(viTri);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(viTri).State = EntityState.Detached;
            throw new XungDotDuLieuException("Tên vị trí đã tồn tại trong cùng cấp cha, hoặc mã vạch ô đã dùng.", ex);
        }
        return viTri;
    }

    public Task<int> DemOTiepNhanHoatDong(int maKho, int? botMaViTri, CancellationToken ct = default)
        => db.ViTri.CountAsync(v => v.MaKho == maKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan
            && v.TrangThai == TrangThaiHoatDong.HoatDong && (botMaViTri == null || v.MaViTri != botMaViTri), ct);

    public Task<int> DemConHoatDong(int maViTriCha, CancellationToken ct = default)
        => db.ViTri.CountAsync(v => v.MaViTriCha == maViTriCha && v.TrangThai == TrangThaiHoatDong.HoatDong, ct);

    public Task LuuThayDoi(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task XoaViTri(ViTri viTri, CancellationToken ct = default)
    {
        db.ViTri.Remove(viTri);
        await db.SaveChangesAsync(ct);
    }
}
