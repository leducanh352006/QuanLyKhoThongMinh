using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Cấu hình lưu tệp đính kèm: thư mục gốc (ngoài wwwroot, ngoài git) và dung lượng tối đa một tệp.</summary>
public sealed record TepLuuTruCauHinh(string ThuMuc, long ToiDaByte = QuyTacTep.ToiDaByteMacDinh);

/// <summary>Quy tắc tên tệp, loại tệp, dung lượng và đường dẫn an toàn; không cần CSDL hay ổ đĩa nên kiểm thử trực tiếp.</summary>
public static class QuyTacTep
{
    public const long ToiDaByteMacDinh = 10L * 1024 * 1024;
    public const int DoDaiToiDaTenTep = 200;

    public static readonly IReadOnlyList<string> DuoiChoPhep = new[]
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".csv", ".doc", ".docx", ".xls", ".xlsx",
    };

    /// <summary>Tên tệp để lưu và hiển thị: bỏ đường dẫn, ký tự cấm, cắt độ dài. Trả null nếu rỗng hoặc đuôi không được phép.</summary>
    public static string? ChuanHoaTen(string? tenGoc)
    {
        if (string.IsNullOrWhiteSpace(tenGoc)) return null;
        var ten = tenGoc.Replace('\\', '/');
        ten = ten[(ten.LastIndexOf('/') + 1)..];
        foreach (var c in Path.GetInvalidFileNameChars()) ten = ten.Replace(c, '_');
        ten = ten.Trim().Trim('.');
        var duoi = Path.GetExtension(ten).ToLowerInvariant();
        if (ten.Length == 0 || !DuoiChoPhep.Contains(duoi)) return null;
        if (ten.Length > DoDaiToiDaTenTep) ten = Path.GetFileNameWithoutExtension(ten)[..(DoDaiToiDaTenTep - duoi.Length)] + duoi;
        return ten;
    }

    /// <summary>Đường dẫn tương đối để lưu: <c>LoaiCT/MaCT/guid.đuôi</c>; tên lưu trên đĩa không dùng tên người dùng nên không thể trùng hay thoát thư mục.</summary>
    public static string DuongDanLuu(string loaiCT, int maCT, string tenDaChuanHoa) =>
        $"{loaiCT}/{maCT}/{Guid.NewGuid():N}{Path.GetExtension(tenDaChuanHoa).ToLowerInvariant()}";

    /// <summary>Ghép thư mục gốc với đường dẫn tương đối và bảo đảm kết quả nằm trong thư mục gốc; null nếu thoát ra ngoài.</summary>
    public static string? DuongDanDayDu(string thuMucGoc, string duongDan)
    {
        var goc = Path.GetFullPath(thuMucGoc).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var day = Path.GetFullPath(Path.Combine(goc, duongDan));
        return day.StartsWith(goc, StringComparison.OrdinalIgnoreCase) ? day : null;
    }

    private static readonly FileExtensionContentTypeProvider LoaiNoiDung = new();

    public static string LoaiNoiDungCua(string tenTep) =>
        LoaiNoiDung.TryGetContentType(tenTep, out var t) ? t : "application/octet-stream";
}

/// <summary>Một tệp đính kèm để hiển thị.</summary>
public sealed record TepDto(int MaTep, string LoaiCT, int MaCT, string TenTep, DateTime ThoiGianUtc, int MaND, string NguoiTai);

/// <summary>Tệp đang mở để tải về; người nhận phải đóng <see cref="Luong"/>.</summary>
public sealed record TepMo(Stream Luong, string TenTep, string LoaiNoiDung);

/// <summary>Cổng truy cập dữ liệu TepDinhKem (EF Core hoặc bộ nhớ). Chỉ thêm, không sửa, không xóa.</summary>
public interface ITepDinhKemStore
{
    Task Them(TepDinhKem tep, CancellationToken ct = default);

    Task<TepDinhKem?> Tim(int maTep, CancellationToken ct = default);

    Task<IReadOnlyList<TepDinhKem>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default);
}

public sealed class EfTepDinhKemStore(KhoDbContext db) : ITepDinhKemStore
{
    public async Task Them(TepDinhKem tep, CancellationToken ct = default)
    {
        db.TepDinhKem.Add(tep);
        await db.SaveChangesAsync(ct);
    }

    public Task<TepDinhKem?> Tim(int maTep, CancellationToken ct = default) =>
        db.TepDinhKem.AsNoTracking().FirstOrDefaultAsync(t => t.MaTep == maTep, ct);

    public async Task<IReadOnlyList<TepDinhKem>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default) =>
        await db.TepDinhKem.AsNoTracking().Where(t => t.LoaiCT == loaiCT && t.MaCT == maCT).OrderBy(t => t.MaTep).ToListAsync(ct);
}

public sealed class BoNhoTepDinhKemStore : ITepDinhKemStore
{
    private readonly object _khoa = new();
    private readonly List<TepDinhKem> _tep = new();
    private int _ma;

    public Task Them(TepDinhKem tep, CancellationToken ct = default)
    {
        lock (_khoa) { tep.MaTep = ++_ma; _tep.Add(tep); }
        return Task.CompletedTask;
    }

    public Task<TepDinhKem?> Tim(int maTep, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_tep.FirstOrDefault(t => t.MaTep == maTep));
    }

    public Task<IReadOnlyList<TepDinhKem>> LayTheoChungTu(string loaiCT, int maCT, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            IReadOnlyList<TepDinhKem> kq = _tep.Where(t => t.LoaiCT == loaiCT && t.MaCT == maCT).OrderBy(t => t.MaTep).ToList();
            return Task.FromResult(kq);
        }
    }
}

/// <summary>Quản lý tệp đính kèm theo chứng từ (việc T4-7). Dùng chung cho mọi phiếu qua thành phần <c>_DinhKem</c>.</summary>
public interface ITepDinhKemQuanLy
{
    Task<IReadOnlyList<TepDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default);

    /// <summary>Lưu tệp, ghi dòng TepDinhKem và nhật ký "Đính kèm tệp". Từ chối tệp rỗng, quá dung lượng hoặc đuôi không cho phép.</summary>
    Task<KetQua<TepDto>> Them(LoaiChungTu loaiCT, int maCT, string tenTep, Stream noiDung, int maND, CancellationToken ct = default);

    /// <summary>Mở tệp để xem lại; thất bại nếu không có bản ghi hoặc tệp trên đĩa đã mất.</summary>
    Task<KetQua<TepMo>> Mo(int maTep, CancellationToken ct = default);

    /// <summary>Chứng từ mà tệp thuộc về (để kiểm quyền Xem trước khi cho tải); null nếu không có.</summary>
    Task<(LoaiChungTu Loai, int MaCT)?> ChungTuCuaTep(int maTep, CancellationToken ct = default);
}

public sealed class TepDinhKemService(
    ITepDinhKemStore store, IHeThongStore nguoiDung, IAuditService audit, TepLuuTruCauHinh cauHinh, TimeProvider gio) : ITepDinhKemQuanLy
{
    public async Task<IReadOnlyList<TepDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
    {
        var tep = await store.LayTheoChungTu(loaiCT.ToCsdl(), maCT, ct);
        var ten = (await nguoiDung.LayNguoiDung(ct)).ToDictionary(n => n.MaND, n => n.HoTen);
        return tep.Select(t => Dto(t, ten.GetValueOrDefault(t.MaND) ?? $"Người dùng {t.MaND}")).ToList();
    }

    public async Task<KetQua<TepDto>> Them(LoaiChungTu loaiCT, int maCT, string tenTep, Stream noiDung, int maND, CancellationToken ct = default)
    {
        if (maCT <= 0) return KetQua<TepDto>.Loi("Chứng từ không hợp lệ.", MaLoiDichVu.DuLieuKhongHopLe);
        var ten = QuyTacTep.ChuanHoaTen(tenTep);
        if (ten is null)
            return KetQua<TepDto>.Loi($"Tên tệp không hợp lệ hoặc loại tệp không được phép (chỉ nhận: {string.Join(", ", QuyTacTep.DuoiChoPhep)}).", MaLoiDichVu.DuLieuKhongHopLe);
        var nguoi = await nguoiDung.TimNguoiDung(maND, ct);
        if (nguoi is null) return KetQua<TepDto>.Loi($"Không tìm thấy người dùng {maND}.", MaLoiDichVu.KhongTimThay);

        var duongDan = QuyTacTep.DuongDanLuu(loaiCT.ToCsdl(), maCT, ten);
        var dayDu = QuyTacTep.DuongDanDayDu(cauHinh.ThuMuc, duongDan)!;
        Directory.CreateDirectory(Path.GetDirectoryName(dayDu)!);

        long daGhi = 0;
        try
        {
            await using (var f = new FileStream(dayDu, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var dem = new byte[81920];
                int doc;
                while ((doc = await noiDung.ReadAsync(dem, ct)) > 0)
                {
                    daGhi += doc;
                    if (daGhi > cauHinh.ToiDaByte) break;
                    await f.WriteAsync(dem.AsMemory(0, doc), ct);
                }
            }

            if (daGhi == 0 || daGhi > cauHinh.ToiDaByte)
            {
                File.Delete(dayDu);
                return KetQua<TepDto>.Loi(daGhi == 0 ? "Tệp rỗng." : $"Tệp vượt quá {cauHinh.ToiDaByte / (1024 * 1024)} MB.", MaLoiDichVu.DuLieuKhongHopLe);
            }

            var tep = new TepDinhKem
            {
                LoaiCT = loaiCT.ToCsdl(), MaCT = maCT, TenTep = ten, DuongDan = duongDan, MaND = maND, ThoiGian = gio.GetUtcNow().UtcDateTime,
            };
            await store.Them(tep, ct);
            await audit.Ghi(maND, "Đính kèm tệp", loaiCT, maCT, ten, ct);
            return KetQua<TepDto>.Ok(Dto(tep, nguoi.HoTen), $"Đã đính kèm '{ten}'.");
        }
        catch
        {
            if (File.Exists(dayDu)) File.Delete(dayDu);   // không để lại tệp mồ côi khi ghi CSDL lỗi
            throw;
        }
    }

    public async Task<KetQua<TepMo>> Mo(int maTep, CancellationToken ct = default)
    {
        var tep = await store.Tim(maTep, ct);
        if (tep is null) return KetQua<TepMo>.Loi($"Không tìm thấy tệp {maTep}.", MaLoiDichVu.KhongTimThay);
        var dayDu = QuyTacTep.DuongDanDayDu(cauHinh.ThuMuc, tep.DuongDan);
        if (dayDu is null || !File.Exists(dayDu))
            return KetQua<TepMo>.Loi($"Tệp '{tep.TenTep}' không còn trong kho lưu trữ.", MaLoiDichVu.KhongTimThay);
        var luong = new FileStream(dayDu, FileMode.Open, FileAccess.Read, FileShare.Read);
        return KetQua<TepMo>.Ok(new TepMo(luong, tep.TenTep, QuyTacTep.LoaiNoiDungCua(tep.TenTep)));
    }

    public async Task<(LoaiChungTu Loai, int MaCT)?> ChungTuCuaTep(int maTep, CancellationToken ct = default)
    {
        var tep = await store.Tim(maTep, ct);
        return tep is not null && Enum.TryParse<LoaiChungTu>(tep.LoaiCT, out var loai) ? (loai, tep.MaCT) : null;
    }

    private static TepDto Dto(TepDinhKem t, string nguoiTai) => new(t.MaTep, t.LoaiCT, t.MaCT, t.TenTep, t.ThoiGian, t.MaND, nguoiTai);
}
