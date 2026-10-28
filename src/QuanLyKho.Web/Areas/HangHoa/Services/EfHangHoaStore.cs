using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Bản thật của <see cref="IHangHoaStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfHangHoaStore(KhoDbContext db) : IHangHoaStore
{
    public async Task<IReadOnlyList<NhaCungCap>> LayNhaCungCap(CancellationToken ct = default)
        => await db.NhaCungCap.AsNoTracking().OrderBy(n => n.TenNCC).ToListAsync(ct);

    public Task<NhaCungCap?> TimNhaCungCap(int maNCC, CancellationToken ct = default)
        => db.NhaCungCap.FirstOrDefaultAsync(n => n.MaNCC == maNCC, ct);

    public async Task<NhaCungCap> ThemNhaCungCap(NhaCungCap ncc, CancellationToken ct = default)
    {
        db.NhaCungCap.Add(ncc);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(ncc).State = EntityState.Detached;
            throw new XungDotDuLieuException("Tên nhà cung cấp đã tồn tại.", ex);
        }
        return ncc;
    }

    public async Task<IReadOnlyList<DanhMuc>> LayDanhMuc(CancellationToken ct = default)
        => await db.DanhMuc.AsNoTracking().OrderBy(d => d.TenDM).ToListAsync(ct);

    public Task<DanhMuc?> TimDanhMuc(int maDM, CancellationToken ct = default)
        => db.DanhMuc.FirstOrDefaultAsync(d => d.MaDM == maDM, ct);

    public async Task<DanhMuc> ThemDanhMuc(DanhMuc danhMuc, CancellationToken ct = default)
    {
        db.DanhMuc.Add(danhMuc);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(danhMuc).State = EntityState.Detached;
            throw new XungDotDuLieuException("Tên danh mục đã tồn tại.", ex);
        }
        return danhMuc;
    }

    public async Task XoaDanhMuc(DanhMuc danhMuc, CancellationToken ct = default)
    {
        db.DanhMuc.Remove(danhMuc);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(danhMuc).State = EntityState.Unchanged;
            throw new XungDotDuLieuException("Danh mục còn sản phẩm tham chiếu, không xóa được.", ex);
        }
    }

    public Task LuuThayDoi(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
