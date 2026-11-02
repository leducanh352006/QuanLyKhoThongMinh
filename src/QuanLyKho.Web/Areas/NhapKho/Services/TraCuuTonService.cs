using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>Bộ lọc tra cứu vị trí và tồn. Chuỗi rỗng hoặc null là không lọc theo mục đó; chuỗi được tìm theo kiểu "có chứa", không phân biệt hoa thường.</summary>
public sealed record BoLocTra(int? MaKho = null, string? BienThe = null, string? Lo = null, string? O = null);

/// <summary>Một dòng tra cứu: một lô đang nằm ở một ô lưu trữ, kèm đường dẫn từ kho đến ô.</summary>
public sealed record DongViTriTon(
    int MaKho, string TenKho, int MaViTri, string DuongDan, string? MaVachO,
    int MaBT, string TenHang, int MaLo, string MaVachLo, DateOnly? HanDung, int SoLuongTon, int SoLuongGiu)
{
    public int KhaDung => SoLuongTon - SoLuongGiu;
}

/// <summary>Kết quả tra cứu; <see cref="BiCat"/> là true khi còn nhiều hơn <see cref="TraCuuTonService.ToiDa"/> dòng nên chỉ trả phần đầu.</summary>
public sealed record KetQuaTra(IReadOnlyList<DongViTriTon> Dong, bool BiCat);

/// <summary>Một dòng báo cáo tổng hợp tồn: một biến thể trong một kho. Luôn có DauKy + Nhap - Xuat = CuoiKy.</summary>
public sealed record DongBaoCao(int MaKho, string TenKho, int MaBT, string TenHang, int DauKy, int Nhap, int Xuat, int CuoiKy);

/// <summary>
/// Tra cứu vị trí và tồn, báo cáo tổng hợp tồn (việc T1-5). Chỉ đọc TonKho, BienDongTon, ViTri; không ghi gì nên không đi qua IStockService.
/// </summary>
public sealed class TraCuuTonService(KhoDbContext db)
{
    /// <summary>Số dòng tối đa mỗi lần tra, để trang không quá nặng.</summary>
    public const int ToiDa = 500;

    /// <summary>
    /// Hàng đang nằm ở ô lưu trữ (hàng đã sắp xếp), còn tồn lớn hơn 0. Hàng còn ở ô Tiếp nhận chưa sắp xếp nên không hiện.
    /// Tồn nằm ở ô, cấp thấp nhất; các cấp trên (kệ, dãy...) chỉ xuất hiện trong đường dẫn.
    /// </summary>
    public async Task<KetQuaTra> Tra(BoLocTra loc, CancellationToken ct = default)
    {
        var q = from t in db.TonKho.AsNoTracking()
                join v in db.ViTri on t.MaViTri equals v.MaViTri
                join k in db.Kho on v.MaKho equals k.MaKho
                join l in db.LoHang on t.MaLo equals l.MaLo
                join b in db.BienThe on l.MaBT equals b.MaBT
                join s in db.SanPham on b.MaSP equals s.MaSP
                where t.SoLuongTon > 0 && v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru
                select new { t, v, k, l, b, s };

        if (loc.MaKho is int maKho) q = q.Where(x => x.v.MaKho == maKho);
        if (Co(loc.BienThe, out var bt)) q = q.Where(x => x.s.TenSP.Contains(bt) || x.b.ThuocTinh.Contains(bt) || x.b.MaVach.Contains(bt));
        if (Co(loc.Lo, out var lo)) q = q.Where(x => x.l.MaVachLo.Contains(lo));
        if (Co(loc.O, out var o)) q = q.Where(x => x.v.TenViTri.Contains(o) || (x.v.MaVachO != null && x.v.MaVachO.Contains(o)));

        var dong = await q.OrderBy(x => x.k.MaKho).ThenBy(x => x.v.MaViTri).ThenBy(x => x.l.MaLo).Take(ToiDa + 1)
            .Select(x => new
            {
                x.v.MaKho, x.k.TenKho, x.v.MaViTri, x.v.MaVachO, x.b.MaBT, x.s.TenSP, x.b.ThuocTinh,
                x.l.MaLo, x.l.MaVachLo, x.l.HanDung, x.t.SoLuongTon, x.t.SoLuongGiu,
            }).ToListAsync(ct);

        var biCat = dong.Count > ToiDa;
        if (biCat) dong.RemoveAt(dong.Count - 1);

        // Cây vị trí của các kho có mặt trong kết quả, để dựng đường dẫn từ kho đến ô.
        var maKhoCo = dong.Select(d => d.MaKho).Distinct().ToList();
        var cay = await db.ViTri.AsNoTracking().Where(v => maKhoCo.Contains(v.MaKho)).ToDictionaryAsync(v => v.MaViTri, ct);

        return new KetQuaTra(dong.Select(d => new DongViTriTon(
            d.MaKho, d.TenKho, d.MaViTri, DuongDan(d.TenKho, d.MaViTri, cay), d.MaVachO, d.MaBT, d.TenSP + " " + d.ThuocTinh,
            d.MaLo, d.MaVachLo, d.HanDung, d.SoLuongTon, d.SoLuongGiu)).ToList(), biCat);
    }

    /// <summary>
    /// Báo cáo tổng hợp tồn từng kho, từng biến thể, tính từ sổ cái BienDongTon trong khoảng [<paramref name="tuUtc"/>, <paramref name="denUtc"/>).
    /// Đầu kỳ là tồn trước <paramref name="tuUtc"/>, cuối kỳ là tồn trước <paramref name="denUtc"/>. Nhập là các dòng sổ cái dương, xuất là các dòng âm
    /// (nhận hàng, xuất kho, chuyển đi, nhận chuyển, hoàn tác). Đổi ô chỉ đưa hàng từ ô này sang ô khác nên không tính là nhập hay xuất của kho.
    /// </summary>
    public async Task<IReadOnlyList<DongBaoCao>> BaoCao(DateTime tuUtc, DateTime denUtc, int? maKho = null, CancellationToken ct = default)
    {
        var so = from b in db.BienDongTon.AsNoTracking()
                 where b.ThoiGianUtc < denUtc
                 join v in db.ViTri on b.MaViTri equals v.MaViTri
                 join l in db.LoHang on b.MaLo equals l.MaLo
                 select new { v.MaKho, l.MaBT, b.SoLuong, b.ThoiGianUtc, b.LoaiBienDong };
        if (maKho is int mk) so = so.Where(x => x.MaKho == mk);

        const string doiO = LoaiBienDongGiaTri.DoiO;
        var nhom = await so.GroupBy(x => new { x.MaKho, x.MaBT }).Select(g => new
        {
            g.Key.MaKho,
            g.Key.MaBT,
            DauKy = g.Sum(x => x.ThoiGianUtc < tuUtc ? x.SoLuong : 0),
            Nhap = g.Sum(x => x.ThoiGianUtc >= tuUtc && x.LoaiBienDong != doiO && x.SoLuong > 0 ? x.SoLuong : 0),
            Xuat = g.Sum(x => x.ThoiGianUtc >= tuUtc && x.LoaiBienDong != doiO && x.SoLuong < 0 ? -x.SoLuong : 0),
            DoiO = g.Sum(x => x.ThoiGianUtc >= tuUtc && x.LoaiBienDong == doiO ? x.SoLuong : 0),
            CuoiKy = g.Sum(x => x.SoLuong),
        }).ToListAsync(ct);

        var tenKho = await db.Kho.AsNoTracking().ToDictionaryAsync(k => k.MaKho, k => k.TenKho, ct);
        var maBT = nhom.Select(n => n.MaBT).Distinct().ToList();
        var tenHang = await db.BienThe.AsNoTracking().Where(b => maBT.Contains(b.MaBT))
            .Join(db.SanPham, b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " " + b.ThuocTinh })
            .ToDictionaryAsync(x => x.MaBT, x => x.Ten, ct);

        return nhom.Select(n =>
            {
                var (nhap, xuat) = GopDoiO(n.Nhap, n.Xuat, n.DoiO);
                return new DongBaoCao(n.MaKho, tenKho.GetValueOrDefault(n.MaKho, "Kho " + n.MaKho), n.MaBT, tenHang.GetValueOrDefault(n.MaBT, "Biến thể " + n.MaBT),
                    n.DauKy, nhap, xuat, n.CuoiKy);
            })
            .Where(d => d.DauKy != 0 || d.Nhap != 0 || d.Xuat != 0 || d.CuoiKy != 0)
            .OrderBy(d => d.TenKho).ThenBy(d => d.TenHang).ToList();
    }

    /// <summary>
    /// Đổi ô trong cùng một kho cộng lại bằng 0. Nếu có lệch (đổi ô giữa hai kho) thì phần lệch tính là nhập hoặc xuất của kho,
    /// để đầu kỳ + nhập - xuất luôn bằng cuối kỳ.
    /// </summary>
    public static (int Nhap, int Xuat) GopDoiO(int nhap, int xuat, int doiO) =>
        doiO > 0 ? (nhap + doiO, xuat) : (nhap, xuat - doiO);

    /// <summary>Đường dẫn từ kho đến ô, ví dụ "Kho tổng › Dãy A › Kệ A1 › Ô A1-01". Cha không có trong cây hoặc vòng lặp thì dừng, không báo lỗi.</summary>
    public static string DuongDan(string tenKho, int maViTri, IReadOnlyDictionary<int, ViTri> cay)
    {
        var ten = new List<string>();
        var daDi = new HashSet<int>();
        var ma = (int?)maViTri;
        while (ma is int m && cay.TryGetValue(m, out var v) && daDi.Add(m))
        {
            ten.Add(v.TenViTri);
            ma = v.MaViTriCha;
        }
        ten.Add(tenKho);
        ten.Reverse();
        return string.Join(" › ", ten);
    }

    private static bool Co(string? s, out string giaTri)
    {
        giaTri = s?.Trim() ?? "";
        return giaTri.Length > 0;
    }
}
