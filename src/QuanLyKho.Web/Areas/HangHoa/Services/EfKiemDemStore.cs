using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Bản thật của <see cref="IKiemDemStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfKiemDemStore(KhoDbContext db) : IKiemDemStore
{
    public Task<PhieuKiemTomTat?> LayPhieu(int maPK, CancellationToken ct = default)
        => (from p in db.PhieuKiem.AsNoTracking()
            join k in db.Kho on p.MaKho equals k.MaKho
            where p.MaPK == maPK
            select new PhieuKiemTomTat(p.MaPK, p.SoPhieu, p.MaKho, k.TenKho, p.LoaiKiemKe, p.TrangThai)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DongKiemXem>> LayDong(int maPK, CancellationToken ct = default)
        => await (
            from d in db.DongKiem.AsNoTracking()
            join v in db.ViTri on d.MaViTri equals v.MaViTri
            join l in db.LoHang on d.MaLo equals l.MaLo
            join b in db.BienThe on l.MaBT equals b.MaBT
            join s in db.SanPham on b.MaSP equals s.MaSP
            join n in db.NguoiDung on d.MaNguoiDem equals n.MaND into nd
            from n in nd.DefaultIfEmpty()
            where d.MaPK == maPK
            orderby v.TenViTri, s.TenSP, l.MaVachLo
            select new DongKiemXem(d.MaDK, d.MaViTri, v.TenViTri, v.MaVachO, d.MaLo, l.MaVachLo, b.MaBT,
                s.TenSP + " (" + b.ThuocTinh + ")", b.MaVach, d.SLHeThong, d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, n.HoTen)
        ).ToListAsync(ct);

    public Task<OQuet?> TimO(string maVachO, CancellationToken ct = default)
        => db.ViTri.AsNoTracking().Where(v => v.MaVachO == maVachO && v.CapViTri == CapViTriGiaTri.O)
            .Select(v => new OQuet(v.MaViTri, v.TenViTri, v.MaKho, v.LoaiViTri == LoaiViTriGiaTri.LuuTru)).FirstOrDefaultAsync(ct);

    public Task<LoQuet?> TimLo(string maVachLo, CancellationToken ct = default)
        => db.LoHang.AsNoTracking().Where(l => l.MaVachLo == maVachLo)
            .Select(l => new LoQuet(l.MaLo, l.MaVachLo, l.MaBT)).FirstOrDefaultAsync(ct);

    public async Task<int?> TimBienThe(string maVach, CancellationToken ct = default)
        => await db.BienThe.AsNoTracking().Where(b => b.MaVach == maVach).Select(b => (int?)b.MaBT).FirstOrDefaultAsync(ct);

    public Task<DongKiem?> TimDong(int maPK, int maDK, CancellationToken ct = default)
        => db.DongKiem.FirstOrDefaultAsync(d => d.MaPK == maPK && d.MaDK == maDK, ct);

    public void Them(DongKiem dong) => db.DongKiem.Add(dong);

    public void Xoa(DongKiem dong) => db.DongKiem.Remove(dong);

    public async Task LuuThayDoi(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.ChangeTracker.Clear();
            throw new InvalidOperationException("Không ghi được dòng kiểm (có thể người khác vừa ghi cùng ô và lô).", ex);
        }
    }
}
