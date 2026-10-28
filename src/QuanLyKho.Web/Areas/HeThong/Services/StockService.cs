using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Bản thật của <see cref="IStockService"/> (việc T4-4): TonKho, BienDongTon qua EF Core trên SQL Server, thay
/// <see cref="Contracts.Stubs.StockServiceStub"/> mà không đổi interface.
/// <para>Mỗi lần đổi tồn là một giao dịch (tham gia giao dịch của nơi gọi nếu đã mở sẵn trên cùng KhoDbContext).
/// Chống tranh chấp bằng khóa lạc quan trên <see cref="TonKho.PhienBan"/> (rowversion): câu UPDATE của EF luôn có
/// điều kiện "còn đúng phiên bản vừa đọc", nên hai thao tác đổi cùng một dòng tồn không thể ghi đè nhau; thao tác
/// thua thử lại tối đa <see cref="SoLanThuLai"/> lần với số liệu mới nhất trước khi báo lỗi.</para>
/// <para>Chống ghi hai lần khi bị gọi lại: trước khi ghi, kiểm tra sổ cái đã có đúng dòng (cùng chứng từ, loại biến
/// động, lô, ô, số lượng) chưa; nếu có rồi thì coi như đã làm, trả về thành công mà không ghi lại.</para>
/// </summary>
public sealed class StockService(KhoDbContext db) : IStockService
{
    private const int SoLanThuLai = 5;

    public Task<KetQua> GiuCho(DongTon dong, CancellationToken ct = default) =>
        ThuLai(async () =>
        {
            var loi = await KiemTraDong(dong, ct, chanKhoa: true);
            if (loi is not null) return loi;

            var ton = await LayHoacTaoTon(dong.MaLo, dong.MaViTri, ct);
            var khaDung = ton.SoLuongTon - ton.SoLuongGiu;
            if (dong.SoLuong > khaDung) return ThieuTon(dong.SoLuong, khaDung);

            ton.SoLuongGiu += dong.SoLuong;
            await db.SaveChangesAsync(ct);
            return KetQua.Ok();
        }, ct);

    public Task<KetQua> TraCho(DongTon dong, CancellationToken ct = default) =>
        ThuLai(async () =>
        {
            var loi = await KiemTraDong(dong, ct);
            if (loi is not null) return loi;

            var ton = await LayHoacTaoTon(dong.MaLo, dong.MaViTri, ct);
            if (dong.SoLuong > ton.SoLuongGiu)
                return KetQua.Loi($"Không thể trả chỗ: cần trả {dong.SoLuong}, đang giữ {ton.SoLuongGiu}.", MaLoiDichVu.DuLieuKhongHopLe);

            ton.SoLuongGiu -= dong.SoLuong;
            await db.SaveChangesAsync(ct);
            return KetQua.Ok();
        }, ct);

    public Task<KetQua> GhiTang(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) =>
        ThuLai(async () =>
        {
            var loi = await KiemTraDong(dong, ct, chanKhoa: chungTu.Loai != LoaiChungTu.KK);
            if (loi is not null) return loi;
            if (await DaGhi(dong.MaLo, dong.MaViTri, dong.SoLuong, loai, chungTu, ct)) return KetQua.Ok();

            var ton = await LayHoacTaoTon(dong.MaLo, dong.MaViTri, ct);
            ton.SoLuongTon += dong.SoLuong;
            GhiSoCai(dong.MaLo, dong.MaViTri, dong.SoLuong, loai, chungTu, maND);
            await db.SaveChangesAsync(ct);
            return KetQua.Ok();
        }, ct);

    public Task<KetQua> GhiGiam(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, bool tuGiuCho = false, CancellationToken ct = default) =>
        ThuLai(async () =>
        {
            var loi = await KiemTraDong(dong, ct, chanKhoa: chungTu.Loai != LoaiChungTu.KK);
            if (loi is not null) return loi;
            if (await DaGhi(dong.MaLo, dong.MaViTri, -dong.SoLuong, loai, chungTu, ct)) return KetQua.Ok();

            var ton = await LayHoacTaoTon(dong.MaLo, dong.MaViTri, ct);
            if (tuGiuCho)
            {
                if (dong.SoLuong > ton.SoLuongGiu)
                    return KetQua.Loi($"Số lượng đang giữ chỗ không đủ: cần {dong.SoLuong}, đang giữ {ton.SoLuongGiu}.", MaLoiDichVu.VuotTon);
                ton.SoLuongTon -= dong.SoLuong;
                ton.SoLuongGiu -= dong.SoLuong;
            }
            else
            {
                var khaDung = ton.SoLuongTon - ton.SoLuongGiu;
                if (dong.SoLuong > khaDung) return ThieuTon(dong.SoLuong, khaDung);
                ton.SoLuongTon -= dong.SoLuong;
            }
            GhiSoCai(dong.MaLo, dong.MaViTri, -dong.SoLuong, loai, chungTu, maND);
            await db.SaveChangesAsync(ct);
            return KetQua.Ok();
        }, ct);

    public Task<KetQua> ChuyenO(int maLo, int maViTriNguon, int maViTriDich, int soLuong, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) =>
        ThuLai(async () =>
        {
            if (maViTriNguon == maViTriDich)
                return KetQua.Loi("Ô nguồn và ô đích phải khác nhau.", MaLoiDichVu.DuLieuKhongHopLe);

            var chanKhoa = chungTu.Loai != LoaiChungTu.KK;
            var loi = await KiemTraDong(new DongTon(maLo, maViTriNguon, soLuong), ct, chanKhoa)
                      ?? await KiemTraDong(new DongTon(maLo, maViTriDich, soLuong), ct, chanKhoa);
            if (loi is not null) return loi;
            if (await DaGhi(maLo, maViTriNguon, -soLuong, LoaiBienDong.DoiO, chungTu, ct)) return KetQua.Ok();

            var tonNguon = await LayHoacTaoTon(maLo, maViTriNguon, ct);
            var khaDung = tonNguon.SoLuongTon - tonNguon.SoLuongGiu;
            if (soLuong > khaDung) return ThieuTon(soLuong, khaDung);

            var tonDich = await LayHoacTaoTon(maLo, maViTriDich, ct);
            tonNguon.SoLuongTon -= soLuong;
            tonDich.SoLuongTon += soLuong;
            GhiSoCai(maLo, maViTriNguon, -soLuong, LoaiBienDong.DoiO, chungTu, maND);
            GhiSoCai(maLo, maViTriDich, soLuong, LoaiBienDong.DoiO, chungTu, maND);
            await db.SaveChangesAsync(ct);
            return KetQua.Ok();
        }, ct);

    public async Task<TonKhoDto?> LayTon(int maLo, int maViTri, CancellationToken ct = default)
    {
        var t = await db.TonKho.AsNoTracking().FirstOrDefaultAsync(x => x.MaLo == maLo && x.MaViTri == maViTri, ct);
        return t is null ? null : new TonKhoDto(t.MaLo, t.MaViTri, t.SoLuongTon, t.SoLuongGiu);
    }

    public async Task<IReadOnlyList<TonKhoDto>> LayTonTheoViTri(int maViTri, CancellationToken ct = default) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaViTri == maViTri && t.SoLuongTon > 0)
            .OrderBy(t => t.MaLo)
            .Select(t => new TonKhoDto(t.MaLo, t.MaViTri, t.SoLuongTon, t.SoLuongGiu))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ChenhLechSoCai>> DoiChieuSoCai(CancellationToken ct = default)
    {
        var tonHienTai = await db.TonKho.AsNoTracking().ToListAsync(ct);
        var theoSoCai = await db.BienDongTon.AsNoTracking()
            .GroupBy(b => new { b.MaLo, b.MaViTri })
            .Select(g => new { g.Key.MaLo, g.Key.MaViTri, Tong = g.Sum(b => b.SoLuong) })
            .ToListAsync(ct);

        var kq = new List<ChenhLechSoCai>();
        foreach (var t in tonHienTai)
        {
            var tong = theoSoCai.FirstOrDefault(s => s.MaLo == t.MaLo && s.MaViTri == t.MaViTri)?.Tong ?? 0;
            if (t.SoLuongTon != tong) kq.Add(new ChenhLechSoCai(t.MaLo, t.MaViTri, t.SoLuongTon, tong));
        }
        return kq;
    }

    // ---- nội bộ ----

    /// <summary>Lấy dòng TonKho đang theo dõi (tracked); tạo dòng 0/0 nếu lô này chưa từng có tồn ở ô này.</summary>
    private async Task<TonKho> LayHoacTaoTon(int maLo, int maViTri, CancellationToken ct)
    {
        var ton = await db.TonKho.FirstOrDefaultAsync(t => t.MaLo == maLo && t.MaViTri == maViTri, ct);
        if (ton is not null) return ton;

        ton = new TonKho { MaLo = maLo, MaViTri = maViTri, SoLuongTon = 0, SoLuongGiu = 0 };
        db.TonKho.Add(ton);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Có thể một giao dịch khác vừa tạo đúng dòng này trước (đụng khóa chính); đọc lại để xác nhận.
            // Nếu đọc lại vẫn không thấy, lỗi là thật (ví dụ MaLo không tồn tại trong LoHang) nên phải báo tiếp, không nuốt lỗi.
            db.Entry(ton).State = EntityState.Detached;
            var lai = await db.TonKho.FirstOrDefaultAsync(t => t.MaLo == maLo && t.MaViTri == maViTri, ct);
            if (lai is null) throw;
            ton = lai;
        }
        return ton;
    }

    private void GhiSoCai(int maLo, int maViTri, int soLuong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND) =>
        db.BienDongTon.Add(new BienDongTon
        {
            MaLo = maLo, MaViTri = maViTri, SoLuong = soLuong,
            LoaiBienDong = loai.ToCsdl(), LoaiCT = chungTu.Loai.ToCsdl(), MaCT = chungTu.MaCT,
            MaND = maND, ThoiGianUtc = DateTime.UtcNow,
        });

    private Task<bool> DaGhi(int maLo, int maViTri, int soLuong, LoaiBienDong loai, ThamChieuChungTu chungTu, CancellationToken ct) =>
        db.BienDongTon.AnyAsync(b => b.MaLo == maLo && b.MaViTri == maViTri && b.SoLuong == soLuong
            && b.LoaiBienDong == loai.ToCsdl() && b.LoaiCT == chungTu.Loai.ToCsdl() && b.MaCT == chungTu.MaCT, ct);

    /// <summary><paramref name="chanKhoa"/> = true: từ chối ô đang khóa kiểm kê (ILockService).</summary>
    private async Task<KetQua?> KiemTraDong(DongTon d, CancellationToken ct, bool chanKhoa = false)
    {
        if (d.SoLuong <= 0)
            return KetQua.Loi("Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);

        var vt = await db.ViTri.AsNoTracking().FirstOrDefaultAsync(v => v.MaViTri == d.MaViTri, ct);
        if (vt is null)
            return KetQua.Loi($"Không tìm thấy vị trí {d.MaViTri}.", MaLoiDichVu.KhongTimThay);
        if (!vt.LaO)
            return KetQua.Loi($"Hàng chỉ được để ở ô, '{vt.TenViTri}' là cấp {vt.CapViTri}.", MaLoiDichVu.DuLieuKhongHopLe);
        if (chanKhoa && vt.DangKhoa)
            return KetQua.Loi($"Ô '{vt.TenViTri}' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        return null;
    }

    private static KetQua ThieuTon(int can, int con) =>
        KetQua.Loi($"Không đủ tồn khả dụng: cần {can}, còn {con}.", MaLoiDichVu.VuotTon);

    /// <summary>
    /// Chạy <paramref name="viec"/> trong một giao dịch (tham gia giao dịch của nơi gọi nếu đã mở sẵn trên cùng db).
    /// Thử lại khi một thao tác khác vừa đổi đúng dòng TonKho đang xử lý (DbUpdateConcurrencyException trên PhienBan).
    /// </summary>
    private async Task<KetQua> ThuLai(Func<Task<KetQua>> viec, CancellationToken ct)
    {
        for (var lan = 1; ; lan++)
        {
            var ganSan = db.Database.CurrentTransaction;
            var giaoDichMoi = ganSan is null ? await db.Database.BeginTransactionAsync(ct) : null;
            try
            {
                var kq = await viec();
                if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);
                return kq;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(ct);
                foreach (var e in db.ChangeTracker.Entries().ToList()) e.State = EntityState.Detached;
                if (lan >= SoLanThuLai) throw;
            }
            finally
            {
                giaoDichMoi?.Dispose();
            }
        }
    }
}
