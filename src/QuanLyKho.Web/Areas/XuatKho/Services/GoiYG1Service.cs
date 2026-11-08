using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Tính phân bổ FEFO/FIFO và dựng đề xuất gợi ý G1 "lấy hàng từ lô, ô nào" (việc T2-2, issue #22) cho phiếu xuất loại Khác,
/// Hủy hàng. Tách khỏi PhieuXuatController để kiểm thử trực tiếp (xem GoiYG1ServiceTests) và dùng chung ở hai nơi: TaoGoiY
/// (dựng đề xuất cho khung gợi ý AI) và Duyet (đường làm tay trực tiếp, không qua gợi ý, mục 1.2.4).
/// </summary>
public interface IGoiYG1Service
{
    /// <summary>
    /// Phân bổ (MaCTX, MaLo, MaViTri, SoLuong) cho từng dòng phiếu; trả lỗi nếu một dòng không đủ tồn khả dụng hợp lệ.
    /// <paramref name="loaiTru"/> (nếu có) loại hẳn các cặp (MaLo, MaViTri) đó khỏi ứng viên — dùng khi gợi ý lại phần thiếu để
    /// không đề xuất lại đúng lô/ô vừa lấy thiếu (FEFO/FIFO thường chọn lại đúng lô/ô đó vì khả dụng vừa được trả lại).
    /// </summary>
    Task<(List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> Phan, string? Loi)> PhanBo(
        PhieuXuat p, CancellationToken ct = default, ISet<(int MaLo, int MaViTri)>? loaiTru = null);

    /// <summary>Chuyển phân bổ của <see cref="PhanBo"/> thành các dòng đề xuất (ChiTietGoiYDto) kèm lý do, sắp theo thứ tự đi lấy.</summary>
    Task<(List<ChiTietGoiYDto> ChiTiet, string? Loi)> TaoDeXuat(PhieuXuat p, CancellationToken ct = default);
}

public sealed class GoiYG1Service(TimeProvider gio, KhoDbContext? db = null) : IGoiYG1Service
{
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa tính được gợi ý G1.");

    /// <summary>
    /// Phân bổ lô/ô cho loại Khác, Hủy hàng: FEFO/FIFO trên ô LƯU TRỮ đang hoạt động, không khóa kiểm kê, của kho xuất — lô đã hết
    /// hạn bị loại hẳn (không gợi ý), còn lại lô hết hạn sớm trước, không có hạn thì lô mã nhỏ trước (xấp xỉ "nhập trước", đúng
    /// Bảng 1.4). Dùng chung cho cả việc tạo gợi ý G1 lẫn Duyệt tay trực tiếp.
    /// </summary>
    public async Task<(List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> Phan, string? Loi)> PhanBo(
        PhieuXuat p, CancellationToken ct = default, ISet<(int MaLo, int MaViTri)>? loaiTru = null)
    {
        var homNay = DateOnly.FromDateTime(gio.GetUtcNow().UtcDateTime);
        var phan = new List<(int, int, int, int)>();
        var daDung = new Dictionary<(int MaLo, int MaViTri), int>();

        foreach (var d in p.ChiTiet)
        {
            var canLay = d.SoLuong;
            var ungVien = (await (
                from t in Db.TonKho
                join l in Db.LoHang on t.MaLo equals l.MaLo
                join v in Db.ViTri on t.MaViTri equals v.MaViTri
                where v.MaKho == p.MaKho && v.LoaiViTri == LoaiViTriGiaTri.LuuTru && v.TrangThai == TrangThaiHoatDong.HoatDong
                      && !v.DangKhoa && (l.HanDung == null || l.HanDung >= homNay) && l.MaBT == d.MaBT
                orderby l.HanDung ?? DateOnly.MaxValue, l.MaLo
                select new { l.MaLo, t.MaViTri, KhaDung = t.SoLuongTon - t.SoLuongGiu }
            ).ToListAsync(ct)).Where(u => loaiTru is null || !loaiTru.Contains((u.MaLo, u.MaViTri))).ToList();

            foreach (var u in ungVien)
            {
                if (canLay <= 0) break;
                var daTru = daDung.GetValueOrDefault((u.MaLo, u.MaViTri));
                var conLai = u.KhaDung - daTru;
                if (conLai <= 0) continue;
                var lay = Math.Min(canLay, conLai);
                phan.Add((d.MaCTX, u.MaLo, u.MaViTri, lay));
                daDung[(u.MaLo, u.MaViTri)] = daTru + lay;
                canLay -= lay;
            }
            if (canLay > 0)
                return (phan, $"{await TenBienThe(d.MaBT, ct)}: không đủ tồn khả dụng (còn hạn dùng, chưa khóa, đang hoạt động) để giữ chỗ (còn thiếu {canLay}).");
        }
        return (phan, null);
    }

    /// <summary>
    /// Chuyển phân bổ của <see cref="PhanBo"/> thành các dòng đề xuất của gợi ý G1, kèm lý do có số liệu căn cứ (hạn dùng của lô,
    /// mã vạch ô) theo yêu cầu của khung gợi ý AI (QuyTacGoiY.KiemTraChiTiet). Không giữ chỗ tồn ở đây; chỉ thật sự giữ chỗ khi
    /// quản lý kho duyệt gợi ý (HandlerG1.ThucHien). Các dòng được sắp theo thứ tự đi lấy trong kho (mục BE của issue #22) —
    /// xấp xỉ bằng đường dẫn vị trí (tên các cấp cha nối tới ô) vì dự án chưa có dữ liệu tuyến đi thật trong kho.
    /// </summary>
    public async Task<(List<ChiTietGoiYDto> ChiTiet, string? Loi)> TaoDeXuat(PhieuXuat p, CancellationToken ct = default)
    {
        var (phan, loi) = await PhanBo(p, ct);
        if (loi is not null) return (new(), loi);
        if (phan.Count == 0) return (new(), null);

        var maLo = phan.Select(x => x.MaLo).Distinct().ToList();
        var maViTri = phan.Select(x => x.MaViTri).Distinct().ToList();
        var lo = await Db.LoHang.Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => new { l.MaVachLo, l.HanDung }, ct);
        var viTri = await Db.ViTri.Where(v => maViTri.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri, v => v, ct);
        var duongDan = await DuongDiAsync(p.MaKho, maViTri, ct);
        var bienThe = p.ChiTiet.ToDictionary(d => d.MaCTX, d => d.MaBT);

        var sapXep = phan.OrderBy(x => duongDan.GetValueOrDefault(x.MaViTri, "")).ThenBy(x => x.MaLo).ToList();

        var chiTiet = sapXep.Select((x, i) =>
        {
            var l = lo[x.MaLo];
            var tenO = viTri[x.MaViTri].MaVachO ?? $"#{x.MaViTri}";
            var lyDo = l.HanDung is DateOnly h
                ? $"Thứ tự lấy {i + 1}: hết hạn trước lấy trước (FEFO) — lô {l.MaVachLo} hạn {h:dd/MM/yyyy}, sớm hơn các lô còn hàng khác " +
                  $"của mặt hàng; lấy {x.SoLuong} tại ô {tenO}, không vượt tồn khả dụng còn lại ở ô đó."
                : $"Thứ tự lấy {i + 1}: lô {l.MaVachLo} không có hạn dùng, lấy theo nhập trước (xấp xỉ bằng mã lô nhỏ hơn trước); " +
                  $"lấy {x.SoLuong} tại ô {tenO}, không vượt tồn khả dụng còn lại ở ô đó.";
            return new ChiTietGoiYDto(bienThe[x.MaCTX], x.SoLuong, lyDo, x.MaLo, x.MaViTri);
        }).ToList();
        return (chiTiet, null);
    }

    /// <summary>Đường dẫn vị trí (tên các cấp cha nối tới từng ô, trong cùng kho) để sắp "thứ tự đi lấy" theo đường đi trong kho.</summary>
    private async Task<Dictionary<int, string>> DuongDiAsync(int maKho, List<int> oCanDuongDan, CancellationToken ct)
    {
        var tatCa = await Db.ViTri.AsNoTracking().Where(v => v.MaKho == maKho).ToListAsync(ct);
        var theoMa = tatCa.ToDictionary(v => v.MaViTri);
        string DuongDan(ViTri v) => v.MaViTriCha is int cha && theoMa.TryGetValue(cha, out var c) ? DuongDan(c) + "/" + v.TenViTri : v.TenViTri;
        return oCanDuongDan.Where(theoMa.ContainsKey).ToDictionary(ma => ma, ma => DuongDan(theoMa[ma]));
    }

    private async Task<string> TenBienThe(int maBT, CancellationToken ct)
        => await Db.BienThe.Where(b => b.MaBT == maBT).Join(Db.SanPham, b => b.MaSP, s => s.MaSP,
            (b, s) => s.TenSP + " (" + b.ThuocTinh + ")").FirstOrDefaultAsync(ct) ?? $"biến thể #{maBT}";
}
