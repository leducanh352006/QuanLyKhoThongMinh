using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Bản thật của <see cref="IHeThongStore"/> trên SQL Server qua EF Core (bảng VaiTro, PhanQuyen, NguoiDung). Đăng ký theo từng yêu cầu (scoped).</summary>
public sealed class EfHeThongStore(KhoDbContext db) : IHeThongStore
{
    public async Task<IReadOnlyList<VaiTro>> LayVaiTro(CancellationToken ct = default)
        => await db.VaiTro.AsNoTracking().OrderBy(v => v.MaVaiTro).ToListAsync(ct);

    public async Task<IReadOnlyList<NguoiDung>> LayNguoiDung(CancellationToken ct = default)
        => await db.NguoiDung.AsNoTracking().OrderBy(n => n.MaND).ToListAsync(ct);

    public Task<NguoiDung?> TimNguoiDung(int maND, CancellationToken ct = default)
        => db.NguoiDung.FirstOrDefaultAsync(n => n.MaND == maND, ct);

    // Collation mặc định của SQL Server không phân biệt hoa thường nên so sánh bằng == là đủ.
    public Task<NguoiDung?> TimNguoiDungTheoTen(string tenDangNhap, CancellationToken ct = default)
        => db.NguoiDung.FirstOrDefaultAsync(n => n.TenDangNhap == tenDangNhap, ct);

    public async Task<NguoiDung> ThemNguoiDung(NguoiDung nguoiDung, CancellationToken ct = default)
    {
        db.NguoiDung.Add(nguoiDung);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(nguoiDung).State = EntityState.Detached;
            throw new XungDotDuLieuException("Tên đăng nhập đã tồn tại.", ex);
        }
        return nguoiDung;
    }

    public Task LuuThayDoi(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<int> DemQuanTriVienHoatDong(int? botMaND, CancellationToken ct = default)
        => (from n in db.NguoiDung
            join v in db.VaiTro on n.MaVaiTro equals v.MaVaiTro
            where v.TenVaiTro == VaiTroTen.QuanTriVien
                  && n.TrangThai == TrangThaiNguoiDung.HoatDong
                  && (botMaND == null || n.MaND != botMaND)
            select n).CountAsync(ct);

    public async Task<IReadOnlyList<PhanQuyen>> LayPhanQuyen(CancellationToken ct = default)
        => await db.PhanQuyen.AsNoTracking().ToListAsync(ct);

    public async Task ThayPhanQuyenCuaVaiTro(int maVaiTro, IReadOnlyList<PhanQuyen> dong, CancellationToken ct = default)
    {
        var hienCo = await db.PhanQuyen.Where(p => p.MaVaiTro == maVaiTro).ToListAsync(ct);
        foreach (var d in dong)
        {
            var cu = hienCo.FirstOrDefault(p => p.LoaiChungTu == d.LoaiChungTu);
            if (cu is null)
            {
                db.PhanQuyen.Add(new PhanQuyen
                {
                    MaVaiTro = maVaiTro, LoaiChungTu = d.LoaiChungTu,
                    CoXem = d.CoXem, CoThem = d.CoThem, CoSua = d.CoSua, CoXoa = d.CoXoa, CoDuyet = d.CoDuyet,
                });
            }
            else
            {
                cu.CoXem = d.CoXem; cu.CoThem = d.CoThem; cu.CoSua = d.CoSua; cu.CoXoa = d.CoXoa; cu.CoDuyet = d.CoDuyet;
            }
        }
        await db.SaveChangesAsync(ct);   // SaveChanges chạy trong một giao dịch ngầm nên các dòng của vai trò đổi cùng lúc
    }
}
