using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Bản thật của <see cref="IPhieuKiemStore"/> trên SQL Server qua EF Core.</summary>
public sealed class EfPhieuKiemStore(KhoDbContext db) : IPhieuKiemStore
{
    public Task<bool> KhoHoatDong(int maKho, CancellationToken ct = default)
        => db.Kho.AnyAsync(k => k.MaKho == maKho && k.TrangThai == TrangThaiHoatDong.HoatDong, ct);

    public async Task<IReadOnlyList<int>> LayOLuuTru(int maKho, CancellationToken ct = default)
        => await db.ViTri.AsNoTracking()
            .Where(v => v.MaKho == maKho && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru)
            .OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync(ct);

    public Task<int> DemNguoiHoatDong(IReadOnlyCollection<int> maND, CancellationToken ct = default)
        => db.NguoiDung.CountAsync(n => maND.Contains(n.MaND) && n.TrangThai == TrangThaiNguoiDung.HoatDong, ct);

    public Task<int> DemBienThe(IReadOnlyCollection<int> maBT, CancellationToken ct = default)
        => db.BienThe.CountAsync(b => maBT.Contains(b.MaBT), ct);

    public async Task<IReadOnlyDictionary<int, int>> LayBienTheCuaLo(IReadOnlyCollection<int> maLo, CancellationToken ct = default)
        => await db.LoHang.AsNoTracking().Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => l.MaBT, ct);

    public async Task Luu(PhieuKiem phieu, CancellationToken ct = default)
    {
        // Nơi gọi đã mở giao dịch thì tham gia giao dịch đó, không mở lồng.
        await using var giaoDich = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            // Số phiếu lấy theo MaPK (IDENTITY) nên phải lưu hai lần trong cùng giao dịch: lần đầu với số tạm duy nhất để có MaPK,
            // lần hai đổi thành KK + mã. Không dùng SEQUENCE của SQL Server vì sẽ phải thêm đối tượng dùng chung ngoài bảng của
            // Area này; cách này vẫn bảo đảm số phiếu duy nhất (chỉ mục UNIQUE trên SoPhieu) và cùng kiểu với phiếu nhập, phiếu xuất.
            phieu.SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12];
            db.PhieuKiem.Add(phieu);
            await db.SaveChangesAsync(ct);
            phieu.SoPhieu = $"KK{phieu.MaPK:D6}";
            await db.SaveChangesAsync(ct);
            if (giaoDich is not null) await giaoDich.CommitAsync(ct);
        }
        catch
        {
            if (giaoDich is not null) await giaoDich.RollbackAsync(CancellationToken.None);
            // Gỡ phiếu hỏng khỏi bộ theo dõi: ILockService dùng chung KhoDbContext, nếu để lại thì lần SaveChanges kế tiếp
            // (mở lại ô) sẽ cố ghi lại phiếu này và lỗi tiếp.
            foreach (var d in phieu.ChiTiet) db.Entry(d).State = EntityState.Detached;
            foreach (var t in phieu.ThamGia) db.Entry(t).State = EntityState.Detached;
            db.Entry(phieu).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<IReadOnlyList<PhieuKiemDong>> DanhSach(int toiDa, CancellationToken ct = default)
        => await (
            from p in db.PhieuKiem.AsNoTracking()
            join k in db.Kho on p.MaKho equals k.MaKho
            join n in db.NguoiDung on p.MaNguoiLap equals n.MaND
            orderby p.MaPK descending
            select new PhieuKiemDong(p.MaPK, p.SoPhieu, p.LoaiKiemKe, p.ThoiGianBatDau, p.MaKho, k.TenKho, n.HoTen,
                p.ChiTiet.Count, p.ThamGia.Count, p.TrangThai)
        ).Take(toiDa).ToListAsync(ct);

    public async Task<PhieuKiemXem?> Xem(int maPK, CancellationToken ct = default)
    {
        var p = await (
            from x in db.PhieuKiem.AsNoTracking()
            join k in db.Kho on x.MaKho equals k.MaKho
            join n in db.NguoiDung on x.MaNguoiLap equals n.MaND
            where x.MaPK == maPK
            select new { Phieu = x, k.TenKho, NguoiLap = n.HoTen }
        ).FirstOrDefaultAsync(ct);
        if (p is null) return null;

        var thamGia = await (
            from t in db.ThamGiaKiem.AsNoTracking()
            join n in db.NguoiDung on t.MaND equals n.MaND
            join v in db.VaiTro on n.MaVaiTro equals v.MaVaiTro
            where t.MaPK == maPK
            orderby n.HoTen
            select new ThamGiaXem(t.MaND, n.HoTen, v.TenVaiTro, t.GhiChu)
        ).ToListAsync(ct);

        var chiTiet = await (
            from d in db.ChiTietPhieuKiem.AsNoTracking()
            join b in db.BienThe on d.MaBT equals b.MaBT
            join s in db.SanPham on b.MaSP equals s.MaSP
            join l in db.LoHang on d.MaLo equals l.MaLo
            join v in db.ViTri on d.MaViTri equals v.MaViTri
            where d.MaPK == maPK
            orderby v.TenViTri, s.TenSP, l.MaVachLo
            select new DongHeThongXem(s.TenSP + " (" + b.ThuocTinh + ")", l.MaVachLo, v.TenViTri, d.SoLuongHeThong)
        ).ToListAsync(ct);

        var x0 = p.Phieu;
        return new PhieuKiemXem(x0.MaPK, x0.SoPhieu, x0.MaKho, p.TenKho, x0.LoaiKiemKe, x0.ThamChieu, x0.DienGiai,
            x0.ThoiGianBatDau, p.NguoiLap, x0.TrangThai, thamGia, chiTiet);
    }

    public async Task<DanhMucChonPhieuKiem> LayDanhMucChon(CancellationToken ct = default)
    {
        var kho = await db.Kho.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong)
            .OrderBy(k => k.TenKho).Select(k => new KeyValuePair<int, string>(k.MaKho, k.TenKho)).ToListAsync(ct);
        var bienThe = await db.BienThe.AsNoTracking()
            .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" })
            .OrderBy(x => x.Ten).Select(x => new KeyValuePair<int, string>(x.MaBT, x.Ten)).ToListAsync(ct);
        var nguoiDung = await db.NguoiDung.AsNoTracking().Where(n => n.TrangThai == TrangThaiNguoiDung.HoatDong)
            .OrderBy(n => n.HoTen).Select(n => new KeyValuePair<int, string>(n.MaND, n.HoTen)).ToListAsync(ct);
        return new DanhMucChonPhieuKiem(kho, bienThe, nguoiDung);
    }
}
