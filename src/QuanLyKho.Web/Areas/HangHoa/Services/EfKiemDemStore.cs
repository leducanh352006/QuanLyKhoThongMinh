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
    {
        var dong = await (
            from d in db.DongKiem.AsNoTracking()
            join v in db.ViTri on d.MaViTri equals v.MaViTri
            join l in db.LoHang on d.MaLo equals l.MaLo
            join b in db.BienThe on l.MaBT equals b.MaBT
            join s in db.SanPham on b.MaSP equals s.MaSP
            join n in db.NguoiDung on d.MaNguoiDem equals n.MaND into nd
            from n in nd.DefaultIfEmpty()
            where d.MaPK == maPK
            orderby v.TenViTri, s.TenSP, l.MaVachLo
            select new
            {
                d.MaDK, d.MaViTri, TenO = v.TenViTri, v.MaVachO, d.MaLo, l.MaVachLo, b.MaBT,
                TenHang = s.TenSP + " (" + b.ThuocTinh + ")", MaVachHang = b.MaVach,
                d.SLHeThong, d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, NguoiDem = n.HoTen,
                d.NguyenNhan, d.MaViTriDung, d.MaBTDung,
            }
        ).ToListAsync(ct);

        // Tên ô đúng và hàng đúng (chỉ có ở dòng đã phân loại sai vị trí, sai mã hàng): lấy riêng để tránh nhiều phép nối ngoài.
        var maODung = dong.Where(d => d.MaViTriDung is not null).Select(d => d.MaViTriDung!.Value).Distinct().ToList();
        var tenODung = maODung.Count == 0 ? new Dictionary<int, string>()
            : await db.ViTri.AsNoTracking().Where(v => maODung.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri, v => v.TenViTri, ct);
        var maBTDung = dong.Where(d => d.MaBTDung is not null).Select(d => d.MaBTDung!.Value).Distinct().ToList();
        var tenHangDung = maBTDung.Count == 0 ? new Dictionary<int, string>()
            : await db.BienThe.AsNoTracking().Where(b => maBTDung.Contains(b.MaBT))
                .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
                .ToDictionaryAsync(x => x.MaBT, x => x.Ten, ct);

        return dong.Select(d => new DongKiemXem(
            d.MaDK, d.MaViTri, d.TenO, d.MaVachO, d.MaLo, d.MaVachLo, d.MaBT, d.TenHang, d.MaVachHang,
            d.SLHeThong, d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, d.NguoiDem,
            d.NguyenNhan, d.MaViTriDung, d.MaViTriDung is int o ? tenODung.GetValueOrDefault(o) : null,
            d.MaBTDung, d.MaBTDung is int bt ? tenHangDung.GetValueOrDefault(bt) : null)).ToList();
    }

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

    public Task<ViTriChon?> TimViTri(int maViTri, CancellationToken ct = default)
        => db.ViTri.AsNoTracking().Where(v => v.MaViTri == maViTri)
            .Select(v => new ViTriChon(v.MaViTri, v.TenViTri, v.MaKho, v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru,
                v.TrangThai == TrangThaiHoatDong.HoatDong, v.DangKhoa, v.Dai, v.Rong, v.Cao)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ViTriChon>> LayOLuuTruHoatDong(int maKho, CancellationToken ct = default)
        => await db.ViTri.AsNoTracking()
            .Where(v => v.MaKho == maKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru && v.TrangThai == TrangThaiHoatDong.HoatDong)
            .OrderBy(v => v.TenViTri)
            .Select(v => new ViTriChon(v.MaViTri, v.TenViTri, v.MaKho, true, true, v.DangKhoa, v.Dai, v.Rong, v.Cao)).ToListAsync(ct);

    public Task<string?> TenBienThe(int maBT, CancellationToken ct = default)
        => db.BienThe.AsNoTracking().Where(b => b.MaBT == maBT)
            .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => s.TenSP + " (" + b.ThuocTinh + ")").FirstOrDefaultAsync(ct);

    public Task<KichThuocBienThe?> KichThuoc(int maBT, CancellationToken ct = default)
        => db.BienThe.AsNoTracking().Where(b => b.MaBT == maBT)
            .Select(b => new KichThuocBienThe(b.Dai, b.Rong, b.Cao)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<KeyValuePair<int, string>>> LayBienTheChon(CancellationToken ct = default)
        => await db.BienThe.AsNoTracking()
            .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .OrderBy(x => x.Ten).Select(x => new KeyValuePair<int, string>(x.MaBT, x.Ten)).ToListAsync(ct);

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
