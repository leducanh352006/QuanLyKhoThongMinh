using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Một lựa chọn trong ô chọn: mã nội bộ gửi lên máy chủ, tên người dùng đọc được.</summary>
public sealed record LuaChon(int Ma, string Ten, int? MaCha = null);

/// <summary>
/// Tên dễ đọc thay cho mã số nội bộ (biến thể, lô, ô, kho, người dùng) và danh sách cho ô chọn, dùng ở Gợi ý AI,
/// Lịch sử tồn, Đối chiếu sổ cái. Dữ liệu danh mục nhỏ nên nạp một lần mỗi yêu cầu. Chưa có CSDL thì mọi danh sách rỗng
/// và các hàm tên trả lại "#mã" như trước.
/// </summary>
public sealed class TenHienThiService(KhoDbContext? db = null)
{
    private Dictionary<int, string> _bienThe = new(), _kho = new(), _nguoiDung = new();
    private List<LuaChon> _lo = new(), _o = new();
    private bool _daNap;

    public bool CoDuLieu => _daNap && db is not null;

    public async Task Nap(CancellationToken ct = default)
    {
        if (_daNap) return;
        _daNap = true;
        if (db is null) return;

        _bienThe = await db.BienThe.AsNoTracking()
            .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, s.TenSP, b.ThuocTinh })
            .ToDictionaryAsync(x => x.MaBT, x => string.IsNullOrWhiteSpace(x.ThuocTinh) ? x.TenSP : $"{x.TenSP} ({x.ThuocTinh})", ct);
        _kho = await db.Kho.AsNoTracking().ToDictionaryAsync(k => k.MaKho, k => k.TenKho, ct);
        _nguoiDung = await db.NguoiDung.AsNoTracking().ToDictionaryAsync(n => n.MaND, n => n.HoTen, ct);
        _lo = (await db.LoHang.AsNoTracking().OrderBy(l => l.MaVachLo).Select(l => new { l.MaLo, l.MaBT, l.MaVachLo, l.HanDung }).ToListAsync(ct))
            .Select(l => new LuaChon(l.MaLo, l.HanDung is DateOnly h ? $"{l.MaVachLo} · hạn {h:dd/MM/yyyy}" : l.MaVachLo, l.MaBT)).ToList();
        _o = (await db.ViTri.AsNoTracking().Where(v => v.CapViTri == CapViTriGiaTri.O).Select(v => new { v.MaViTri, v.MaKho, v.TenViTri }).ToListAsync(ct))
            .Select(v => new LuaChon(v.MaViTri, $"{_kho.GetValueOrDefault(v.MaKho, "Kho #" + v.MaKho)} ▷ {v.TenViTri}", v.MaKho))
            .OrderBy(v => v.Ten).ToList();
    }

    public string BienThe(int ma) => _bienThe.GetValueOrDefault(ma, $"Biến thể #{ma}");
    public string Kho(int ma) => _kho.GetValueOrDefault(ma, $"Kho #{ma}");
    public string NguoiDung(int ma) => _nguoiDung.GetValueOrDefault(ma, $"người dùng #{ma}");
    public string Lo(int ma) => _lo.FirstOrDefault(l => l.Ma == ma)?.Ten.Split(" · ")[0] ?? $"lô #{ma}";
    public string O(int ma) => _o.FirstOrDefault(o => o.Ma == ma)?.Ten ?? $"ô #{ma}";

    /// <summary>Lô của một biến thể (null: mọi lô).</summary>
    public IReadOnlyList<LuaChon> DanhSachLo(int? maBT = null) => maBT is int bt ? _lo.Where(l => l.MaCha == bt).ToList() : _lo;
    public IReadOnlyList<LuaChon> DanhSachO() => _o;
    public IReadOnlyList<LuaChon> DanhSachKho() => _kho.Select(k => new LuaChon(k.Key, k.Value)).OrderBy(k => k.Ten).ToList();
}
