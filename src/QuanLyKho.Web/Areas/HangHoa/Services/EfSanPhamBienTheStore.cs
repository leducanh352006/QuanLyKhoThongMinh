using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Bản thật của <see cref="ISanPhamBienTheStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfSanPhamBienTheStore(KhoDbContext db) : ISanPhamBienTheStore
{
    public async Task<IReadOnlyList<SanPham>> LaySanPham(CancellationToken ct = default)
        => await db.SanPham.AsNoTracking().OrderBy(s => s.TenSP).ToListAsync(ct);

    public Task<SanPham?> TimSanPham(int maSP, CancellationToken ct = default)
        => db.SanPham.FirstOrDefaultAsync(s => s.MaSP == maSP, ct);

    public async Task<SanPham> ThemSanPham(SanPham sp, CancellationToken ct = default)
    {
        db.SanPham.Add(sp);
        await db.SaveChangesAsync(ct);
        return sp;
    }

    public async Task<IReadOnlyList<BienThe>> LayBienTheTheoSanPham(int maSP, CancellationToken ct = default)
        => await db.BienThe.AsNoTracking().Where(b => b.MaSP == maSP).OrderBy(b => b.ThuocTinh).ToListAsync(ct);

    public Task<BienThe?> TimBienThe(int maBT, CancellationToken ct = default)
        => db.BienThe.FirstOrDefaultAsync(b => b.MaBT == maBT, ct);

    public async Task<BienThe> ThemBienThe(BienThe bt, CancellationToken ct = default)
    {
        db.BienThe.Add(bt);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(bt).State = EntityState.Detached;
            throw new XungDotDuLieuException("Mã vạch đã tồn tại.", ex);
        }
        return bt;
    }

    public Task LuuThayDoi(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
