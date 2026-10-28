using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Bản thật của <see cref="ILoHangStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfLoHangStore(KhoDbContext db) : ILoHangStore
{
    public async Task<IReadOnlyList<LoHang>> LayTheoBienThe(int maBT, CancellationToken ct = default)
        => await db.LoHang.AsNoTracking().Where(l => l.MaBT == maBT).OrderBy(l => l.MaLo).ToListAsync(ct);

    public Task<LoHang?> Tim(int maLo, CancellationToken ct = default)
        => db.LoHang.FirstOrDefaultAsync(l => l.MaLo == maLo, ct);

    public Task<LoHang?> TimTheoMaVach(string maVachLo, CancellationToken ct = default)
        => db.LoHang.FirstOrDefaultAsync(l => l.MaVachLo == maVachLo, ct);

    public async Task<LoHang> Them(LoHang lo, CancellationToken ct = default)
    {
        db.LoHang.Add(lo);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(lo).State = EntityState.Detached;
            throw new XungDotDuLieuException("Mã vạch lô đã tồn tại.", ex);
        }
        return lo;
    }
}
