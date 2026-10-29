using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Cổng truy cập dữ liệu GoiYAI, ChiTietGoiY (việc T4-8), cùng kiểu <see cref="IKhoViTriStore"/>: bản EF Core (SQL Server) và bản trong bộ nhớ
/// (Development chưa có chuỗi kết nối, và kiểm thử). Gợi ý chỉ thêm và đổi trạng thái một lần từ Chờ duyệt, không bao giờ xóa.
/// </summary>
public interface IGoiYStore
{
    /// <summary>Lưu gợi ý Chờ duyệt cùng các dòng gốc của AI trong một giao dịch. Thất bại (biến thể, lô, ô, kho, phiếu nhập không tồn tại) thì không lưu gì.</summary>
    Task<KetQua<int>> Them(LoaiGoiY loai, ThamChieuGoiY thamChieu, IReadOnlyList<ChiTietGoiYDto> chiTiet, DateTime luc, CancellationToken ct = default);

    /// <summary>Gợi ý kèm các dòng có hiệu lực (dòng sửa nếu có, ngược lại dòng gốc).</summary>
    Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default);

    Task<IReadOnlyList<GoiYDto>> DanhSach(TrangThaiGoiY? trangThai, LoaiGoiY? loai, int toiDa, CancellationToken ct = default);

    /// <summary>Các dòng gốc của AI, không bị ảnh hưởng bởi "Sửa rồi duyệt" (để so sánh).</summary>
    Task<IReadOnlyList<ChiTietGoiYDto>> LayChiTietGoc(int maGoiY, CancellationToken ct = default);

    /// <summary>
    /// Chạy <paramref name="viec"/> trong một giao dịch: thành công thì giữ mọi thay đổi, thất bại (hoặc ném lỗi) thì hoàn tác hết.
    /// Tham gia giao dịch của nơi gọi nếu đã mở sẵn (dùng điểm lưu). Nhờ vậy thao tác của nhóm khác (handler) chạy cùng giao dịch với việc đổi trạng thái.
    /// </summary>
    Task<KetQua> ChayTrongGiaoDich(Func<Task<KetQua>> viec, CancellationToken ct = default);

    /// <summary>
    /// Đổi trạng thái từ Chờ duyệt sang <paramref name="moi"/> bằng một lệnh có điều kiện: đúng một người thắng khi hai người cùng xử lý.
    /// Trả false nếu gợi ý không còn Chờ duyệt (hoặc không tồn tại).
    /// </summary>
    Task<bool> NhanXuLy(int maGoiY, TrangThaiGoiY moi, int maNguoiDuyet, DateTime luc, CancellationToken ct = default);

    /// <summary>Ghi tham chiếu phiếu do handler tạo và (nếu có) các dòng người duyệt sửa; dòng gốc giữ nguyên.</summary>
    Task<KetQua> CapNhatKetQua(int maGoiY, ThamChieuGoiY thamChieu, IReadOnlyList<ChiTietGoiYDto>? chiTietDaSua, CancellationToken ct = default);
}

public sealed class EfGoiYStore(KhoDbContext db) : IGoiYStore
{
    private const string LoiThamChieu = "Không lưu được vì dữ liệu tham chiếu không hợp lệ (biến thể, lô, ô, kho hoặc phiếu không tồn tại).";

    public async Task<KetQua<int>> Them(LoaiGoiY loai, ThamChieuGoiY tc, IReadOnlyList<ChiTietGoiYDto> chiTiet, DateTime luc, CancellationToken ct = default)
    {
        var ma = 0;
        var kq = await ChayTrongGiaoDich(async () =>
        {
            var g = new GoiYAI
            {
                LoaiGoiY = loai.ToCsdl(), ThoiGianTao = luc, TrangThai = TrangThaiGoiY.ChoDuyet.ToCsdl(),
                MaPX = tc.MaPX, MaPN = tc.MaPN, MaPC = tc.MaPC,
            };
            db.GoiYAI.Add(g);
            await db.SaveChangesAsync(ct);
            db.ChiTietGoiY.AddRange(chiTiet.Select(c => TaoDong(g.MaGoiY, c, laBanSua: false)));
            await db.SaveChangesAsync(ct);
            ma = g.MaGoiY;
            return KetQua.Ok();
        }, ct);
        return kq.ThanhCong ? KetQua<int>.Ok(ma) : KetQua<int>.Loi(kq.ThongBao ?? LoiThamChieu, kq.MaLoi);
    }

    public async Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default)
    {
        var g = await db.GoiYAI.AsNoTracking().FirstOrDefaultAsync(x => x.MaGoiY == maGoiY, ct);
        if (g is null) return null;
        var dong = await db.ChiTietGoiY.AsNoTracking().Where(c => c.MaGoiY == maGoiY).OrderBy(c => c.MaCTGY).ToListAsync(ct);
        return ToDto(g, dong);
    }

    public async Task<IReadOnlyList<GoiYDto>> DanhSach(TrangThaiGoiY? trangThai, LoaiGoiY? loai, int toiDa, CancellationToken ct = default)
    {
        var q = db.GoiYAI.AsNoTracking().AsQueryable();
        if (trangThai is TrangThaiGoiY tt) { var s = tt.ToCsdl(); q = q.Where(g => g.TrangThai == s); }
        if (loai is LoaiGoiY l) { var s = l.ToCsdl(); q = q.Where(g => g.LoaiGoiY == s); }
        // Đang chờ thì cũ trước (làm theo thứ tự tạo); còn lại mới trước (xem lại việc vừa làm).
        var gy = trangThai == TrangThaiGoiY.ChoDuyet
            ? await q.OrderBy(g => g.MaGoiY).Take(toiDa).ToListAsync(ct)
            : await q.OrderByDescending(g => g.MaGoiY).Take(toiDa).ToListAsync(ct);
        if (gy.Count == 0) return Array.Empty<GoiYDto>();

        var maGoiY = gy.Select(g => g.MaGoiY).ToList();
        var dong = (await db.ChiTietGoiY.AsNoTracking().Where(c => maGoiY.Contains(c.MaGoiY)).OrderBy(c => c.MaCTGY).ToListAsync(ct))
            .ToLookup(c => c.MaGoiY);
        return gy.Select(g => ToDto(g, dong[g.MaGoiY].ToList())).ToList();
    }

    public async Task<IReadOnlyList<ChiTietGoiYDto>> LayChiTietGoc(int maGoiY, CancellationToken ct = default) =>
        (await db.ChiTietGoiY.AsNoTracking().Where(c => c.MaGoiY == maGoiY && !c.LaBanSua).OrderBy(c => c.MaCTGY).ToListAsync(ct))
            .Select(ToChiTietDto).ToList();

    public async Task<KetQua> ChayTrongGiaoDich(Func<Task<KetQua>> viec, CancellationToken ct = default)
    {
        var ganSan = db.Database.CurrentTransaction;
        var giaoDichMoi = ganSan is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var diemLui = "gy" + Guid.NewGuid().ToString("N")[..16];   // SQL Server giới hạn tên điểm lưu 32 ký tự
        if (ganSan is not null) await ganSan.CreateSavepointAsync(diemLui, ct);
        try
        {
            KetQua kq;
            try
            {
                kq = await viec();
            }
            catch (DbUpdateException)
            {
                kq = KetQua.Loi(LoiThamChieu, MaLoiDichVu.DuLieuKhongHopLe);
            }

            if (kq.ThanhCong)
            {
                if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);
                return kq;
            }

            await HoanTac(ganSan, giaoDichMoi, diemLui);
            return kq;
        }
        catch
        {
            await HoanTac(ganSan, giaoDichMoi, diemLui);
            throw;
        }
        finally
        {
            giaoDichMoi?.Dispose();
        }
    }

    private async Task HoanTac(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? ganSan,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? giaoDichMoi, string diemLui)
    {
        if (giaoDichMoi is not null) await giaoDichMoi.RollbackAsync(CancellationToken.None);
        else await ganSan!.RollbackToSavepointAsync(diemLui, CancellationToken.None);
        db.ChangeTracker.Clear();   // bỏ mọi đối tượng đã theo dõi nhưng không còn khớp CSDL sau khi hoàn tác
    }

    public async Task<bool> NhanXuLy(int maGoiY, TrangThaiGoiY moi, int maNguoiDuyet, DateTime luc, CancellationToken ct = default)
    {
        var cho = TrangThaiGoiY.ChoDuyet.ToCsdl();
        var trangThaiMoi = moi.ToCsdl();
        int? nguoi = maNguoiDuyet;
        DateTime? thoiGian = luc;
        var soDong = await db.GoiYAI.Where(g => g.MaGoiY == maGoiY && g.TrangThai == cho)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.TrangThai, trangThaiMoi)
                .SetProperty(g => g.MaNguoiDuyet, nguoi)
                .SetProperty(g => g.ThoiGianDuyet, thoiGian), ct);
        return soDong == 1;
    }

    public async Task<KetQua> CapNhatKetQua(int maGoiY, ThamChieuGoiY tc, IReadOnlyList<ChiTietGoiYDto>? chiTietDaSua, CancellationToken ct = default)
    {
        await db.GoiYAI.Where(g => g.MaGoiY == maGoiY).ExecuteUpdateAsync(s => s
            .SetProperty(g => g.MaPX, tc.MaPX)
            .SetProperty(g => g.MaPN, tc.MaPN)
            .SetProperty(g => g.MaPC, tc.MaPC), ct);
        if (chiTietDaSua is not null)
        {
            db.ChiTietGoiY.AddRange(chiTietDaSua.Select(c => TaoDong(maGoiY, c, laBanSua: true)));
            await db.SaveChangesAsync(ct);
        }
        return KetQua.Ok();
    }

    private static ChiTietGoiY TaoDong(int maGoiY, ChiTietGoiYDto c, bool laBanSua) => new()
    {
        MaGoiY = maGoiY, MaBT = c.MaBT, MaLo = c.MaLo, MaViTri = c.MaViTri, MaKhoNguon = c.MaKhoNguon, MaKhoDich = c.MaKhoDich,
        SoLuong = c.SoLuong, LyDo = c.LyDo.Trim(), LaBanSua = laBanSua,
    };

    private static ChiTietGoiYDto ToChiTietDto(ChiTietGoiY c) => new(c.MaBT, c.SoLuong, c.LyDo, c.MaLo, c.MaViTri, c.MaKhoNguon, c.MaKhoDich);

    private static GoiYDto ToDto(GoiYAI g, IReadOnlyList<ChiTietGoiY> dong)
    {
        var coHieuLuc = dong.Any(c => c.LaBanSua) ? dong.Where(c => c.LaBanSua) : dong.Where(c => !c.LaBanSua);
        return new GoiYDto(
            g.MaGoiY, GiaTriCsdl.ParseLoaiGoiY(g.LoaiGoiY), GiaTriCsdl.ParseTrangThaiGoiY(g.TrangThai),
            DateTime.SpecifyKind(g.ThoiGianTao, DateTimeKind.Utc), g.MaNguoiDuyet,
            g.ThoiGianDuyet is DateTime d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null,
            new ThamChieuGoiY(g.MaPX, g.MaPN, g.MaPC), coHieuLuc.Select(ToChiTietDto).ToList());
    }
}

/// <summary>Bản trong bộ nhớ của <see cref="IGoiYStore"/>. Mất dữ liệu khi tắt ứng dụng; không kiểm khóa ngoại (không có CSDL).</summary>
public sealed class BoNhoGoiYStore : IGoiYStore
{
    private sealed record Muc(GoiYDto Dto, IReadOnlyList<ChiTietGoiYDto> Goc);

    private readonly object _khoa = new();
    private readonly SemaphoreSlim _giaoDich = new(1, 1);
    private Dictionary<int, Muc> _goiY = new();
    private int _ma;

    public Task<KetQua<int>> Them(LoaiGoiY loai, ThamChieuGoiY tc, IReadOnlyList<ChiTietGoiYDto> chiTiet, DateTime luc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var ma = ++_ma;
            var dong = chiTiet.Select(c => c with { LyDo = c.LyDo.Trim() }).ToList();
            _goiY[ma] = new Muc(new GoiYDto(ma, loai, TrangThaiGoiY.ChoDuyet, luc, null, null, tc, dong), dong);
            return Task.FromResult(KetQua<int>.Ok(ma));
        }
    }

    public Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_goiY.TryGetValue(maGoiY, out var m) ? m.Dto : null);
    }

    public Task<IReadOnlyList<GoiYDto>> DanhSach(TrangThaiGoiY? trangThai, LoaiGoiY? loai, int toiDa, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var q = _goiY.Values.Select(m => m.Dto)
                .Where(g => (trangThai is null || g.TrangThai == trangThai) && (loai is null || g.Loai == loai));
            IReadOnlyList<GoiYDto> kq = (trangThai == TrangThaiGoiY.ChoDuyet ? q.OrderBy(g => g.MaGoiY) : q.OrderByDescending(g => g.MaGoiY))
                .Take(toiDa).ToList();
            return Task.FromResult(kq);
        }
    }

    public Task<IReadOnlyList<ChiTietGoiYDto>> LayChiTietGoc(int maGoiY, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_goiY.TryGetValue(maGoiY, out var m) ? m.Goc : Array.Empty<ChiTietGoiYDto>());
    }

    public async Task<KetQua> ChayTrongGiaoDich(Func<Task<KetQua>> viec, CancellationToken ct = default)
    {
        await _giaoDich.WaitAsync(ct);   // một giao dịch một lúc, thay cho khóa dòng của CSDL
        Dictionary<int, Muc> truoc;
        lock (_khoa) truoc = new Dictionary<int, Muc>(_goiY);
        try
        {
            var kq = await viec();
            if (!kq.ThanhCong) lock (_khoa) _goiY = truoc;
            return kq;
        }
        catch
        {
            lock (_khoa) _goiY = truoc;
            throw;
        }
        finally
        {
            _giaoDich.Release();
        }
    }

    public Task<bool> NhanXuLy(int maGoiY, TrangThaiGoiY moi, int maNguoiDuyet, DateTime luc, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (!_goiY.TryGetValue(maGoiY, out var m) || m.Dto.TrangThai != TrangThaiGoiY.ChoDuyet) return Task.FromResult(false);
            _goiY[maGoiY] = m with { Dto = m.Dto with { TrangThai = moi, MaNguoiDuyet = maNguoiDuyet, ThoiGianDuyetUtc = luc } };
            return Task.FromResult(true);
        }
    }

    public Task<KetQua> CapNhatKetQua(int maGoiY, ThamChieuGoiY tc, IReadOnlyList<ChiTietGoiYDto>? chiTietDaSua, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var m = _goiY[maGoiY];
            var dong = chiTietDaSua?.Select(c => c with { LyDo = c.LyDo.Trim() }).ToList() ?? m.Dto.ChiTiet;
            _goiY[maGoiY] = m with { Dto = m.Dto with { ThamChieu = tc, ChiTiet = dong } };
            return Task.FromResult(KetQua.Ok());
        }
    }
}
