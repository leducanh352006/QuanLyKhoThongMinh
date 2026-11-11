using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>
/// Quy tắc gợi ý G2 "xếp hàng nhập vào ô nào" (việc T1-3, Bảng 1.3, Bảng 1.4, Bảng 2.3 dòng 3), thuần túy, không cần CSDL nên kiểm thử trực tiếp.
/// Ràng buộc cứng kiểm bằng code: q × t(v) ≤ V(p) - Vđ(p) và q × k(v) ≤ K(p) - Kđ(p), kể cả hàng đã xếp vào cùng ô ở các dòng trước của chính gợi ý này.
/// Luật làm mốc: điểm S = 0,5 × gần khu tiếp nhận + 0,3 × cạnh hàng cùng loại + 0,2 × độ lấp đầy phù hợp; ô điểm cao nhất được chọn trước.
/// </summary>
public static class QuyTacG2
{
    public const double TrongSoGan = 0.5, TrongSoCungHang = 0.3, TrongSoLapDay = 0.2;

    /// <summary>Một ô lưu trữ dùng được: thể tích và khối lượng còn nhận thêm (đã trừ hàng đang có và hàng chờ xếp), điểm gần cửa nhận, điểm cạnh hàng cùng loại (0 đến 1) và nhãn kích thước lòng ô.</summary>
    public sealed record OUngVien(int MaViTri, string TenViTri, decimal TheTichCon, decimal KhoiCon, double Gan, double CungHang, string? KichThuocO = null);

    /// <summary>Phần hàng xếp vào một ô và độ lấp đầy của ô sau khi xếp (0 đến 1).</summary>
    public sealed record PhanXep(OUngVien O, int SoLuong, double LapDay)
    {
        public double Diem => TrongSoGan * O.Gan + TrongSoCungHang * O.CungHang + TrongSoLapDay * LapDay;
    }

    /// <summary>Số đơn vị tối đa nhận thêm được, theo cả thể tích lẫn khối lượng.</summary>
    public static int ToiDa(decimal theTichCon, decimal khoiCon, decimal theTich1, decimal khoi1) =>
        Math.Min(Gioi(theTichCon, theTich1), Gioi(khoiCon, khoi1));

    private static int Gioi(decimal conLai, decimal motDonVi)
    {
        if (motDonVi <= 0) return int.MaxValue;
        var n = Math.Floor(Math.Max(conLai, 0m) / motDonVi);
        return n >= int.MaxValue ? int.MaxValue : (int)n;
    }

    /// <summary>Độ lấp đầy phù hợp: phần thể tích hoặc khối lượng còn lại của ô mà số hàng này chiếm (chiều nào chật hơn), tối đa 1.</summary>
    public static double LapDay(int soLuong, decimal theTichCon, decimal khoiCon, decimal theTich1, decimal khoi1)
    {
        var a = theTichCon > 0 ? (double)(soLuong * theTich1 / theTichCon) : 1.0;
        var b = khoiCon > 0 ? (double)(soLuong * khoi1 / khoiCon) : 1.0;
        return Math.Min(1.0, Math.Max(a, b));
    }

    /// <summary>
    /// Chia <paramref name="soLuong"/> hàng vào các ô, ô điểm cao trước, mỗi ô nhận tối đa theo sức chứa. Trả null nếu các ô không đủ chỗ.
    /// <paramref name="daDung"/>: thể tích và khối lượng mỗi ô đã bị các dòng trước của cùng gợi ý chiếm.
    /// Hai phần của cùng một dòng không có cùng số lượng (kể cả với <paramref name="daCo"/>, các lệnh xếp còn hiệu lực của dòng đó),
    /// vì StockService.ChuyenO coi hai lần chuyển trùng số lượng là gọi lặp (xem SapXepService.LapLenh).
    /// </summary>
    public static List<PhanXep>? PhanBo(
        int soLuong, decimal theTich1, decimal khoi1, IReadOnlyList<OUngVien> ungVien,
        IReadOnlyDictionary<int, (decimal TheTich, decimal Khoi)> daDung, IReadOnlyCollection<int>? daCo = null)
    {
        daCo ??= Array.Empty<int>();
        var ketQua = new List<PhanXep>();
        var daChon = new HashSet<int>();
        var conLai = soLuong;
        while (conLai > 0)
        {
            PhanXep? tot = null;
            foreach (var o in ungVien)
            {
                if (daChon.Contains(o.MaViTri)) continue;
                var (theTichCon, khoiCon) = Con(o, daDung);
                var toiDa = ToiDa(theTichCon, khoiCon, theTich1, khoi1);
                if (toiDa <= 0) continue;
                var lay = Math.Min(conLai, toiDa);
                var ung = new PhanXep(o, lay, LapDay(lay, theTichCon, khoiCon, theTich1, khoi1));
                var hon = tot is null || Math.Round(ung.Diem, 6) > Math.Round(tot.Diem, 6)
                          || (Math.Round(ung.Diem, 6) == Math.Round(tot.Diem, 6) && o.MaViTri < tot.O.MaViTri);
                if (hon) tot = ung;
            }
            if (tot is null) return null;
            ketQua.Add(tot);
            daChon.Add(tot.O.MaViTri);
            conLai -= tot.SoLuong;
        }

        // Trùng số lượng (ví dụ 5 + 5): chuyển 1 đơn vị từ ô đầu sang ô cuối nếu ô cuối còn chỗ (4 + 6); không được thì bỏ gợi ý dòng này, thủ kho xếp tay.
        if (!KhacNhau(ketQua, daCo) && ketQua.Count >= 2)
        {
            var dau = ketQua[0];
            var cuoi = ketQua[^1];
            var (theTichCon, khoiCon) = Con(cuoi.O, daDung);
            if (dau.SoLuong > 1 && ToiDa(theTichCon, khoiCon, theTich1, khoi1) >= cuoi.SoLuong + 1)
            {
                var (theTichDau, khoiDau) = Con(dau.O, daDung);
                ketQua[0] = dau with { SoLuong = dau.SoLuong - 1, LapDay = LapDay(dau.SoLuong - 1, theTichDau, khoiDau, theTich1, khoi1) };
                ketQua[^1] = cuoi with { SoLuong = cuoi.SoLuong + 1, LapDay = LapDay(cuoi.SoLuong + 1, theTichCon, khoiCon, theTich1, khoi1) };
            }
        }
        return KhacNhau(ketQua, daCo) ? ketQua : null;
    }

    private static (decimal TheTichCon, decimal KhoiCon) Con(OUngVien o, IReadOnlyDictionary<int, (decimal TheTich, decimal Khoi)> daDung)
    {
        var (theTich, khoi) = daDung.GetValueOrDefault(o.MaViTri);
        return (o.TheTichCon - theTich, o.KhoiCon - khoi);
    }

    private static bool KhacNhau(List<PhanXep> ds, IReadOnlyCollection<int> daCo) => ds.Select(x => x.SoLuong).Concat(daCo).GroupBy(x => x).All(g => g.Count() == 1);
}

/// <summary>
/// Tạo gợi ý G2 cho một phiếu nhập Đã kiểm tra: với mỗi dòng hàng đạt chưa lập lệnh xếp, đề xuất ô (hoặc nhiều ô nếu vượt sức chứa) cùng lý do có điểm S và số liệu,
/// lưu qua <see cref="IGoiYService"/> ở trạng thái Chờ duyệt. Chỉ ô lưu trữ đang hoạt động, không khóa kiểm kê, còn chỗ theo <see cref="ICapacityService"/> mới được gợi ý.
/// </summary>
public sealed class GoiYG2Service(KhoDbContext db, ICapacityService sucChua, IGoiYService goiY, IAuditService audit)
{
    public async Task<KetQua<int>> TaoGoiY(int maPN, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == maPN, ct);
        if (p is null) return KetQua<int>.Loi("Không tìm thấy phiếu nhập.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuNhap.DaKiemTra)
            return KetQua<int>.Loi($"Chỉ gợi ý cho phiếu Đã kiểm tra, phiếu {p.SoPhieu} đang {p.TrangThai}.", MaLoiDichVu.SaiTrangThai);
        if ((await goiY.DanhSachChoDuyet(LoaiGoiY.G2, ct)).Any(g => g.ThamChieu.MaPN == maPN))
            return KetQua<int>.Loi("Phiếu này đã có gợi ý G2 đang chờ duyệt. Hãy duyệt hoặc từ chối gợi ý đó trước.", MaLoiDichVu.SaiTrangThai);

        // Dòng hàng đạt còn chưa lập lệnh xếp.
        var maCTN = p.ChiTiet.Select(d => d.MaCTN).ToList();
        var lenhCu = await db.SapXep.AsNoTracking().Where(s => maCTN.Contains(s.MaCTN) && s.TrangThai != SapXep.DaHuy).ToListAsync(ct);
        var daLap = lenhCu.GroupBy(s => s.MaCTN).ToDictionary(g => g.Key, g => g.Sum(s => s.SoLuong));
        var dong = p.ChiTiet.Where(d => d.MaLo != null && (d.SLDat ?? 0) - daLap.GetValueOrDefault(d.MaCTN) > 0).OrderBy(d => d.MaCTN).ToList();
        if (dong.Count == 0) return KetQua<int>.Loi("Phiếu không còn hàng đạt nào chưa lập lệnh xếp.", MaLoiDichVu.SaiTrangThai);

        // Ô lưu trữ dùng được và khoảng cách tới khu tiếp nhận (xấp xỉ bằng số bước đi trên cây vị trí, vì chưa có dữ liệu tuyến đi thật).
        var viTri = await db.ViTri.AsNoTracking().Where(v => v.MaKho == p.MaKho).ToListAsync(ct);
        var theoMa = viTri.ToDictionary(v => v.MaViTri);
        var oXep = viTri.Where(v => v.LaO && v.LoaiViTri == LoaiViTriGiaTri.LuuTru && v.DangHoatDong && !v.DangKhoa).OrderBy(v => v.MaViTri).ToList();
        if (oXep.Count == 0) return KetQua<int>.Loi("Kho chưa có ô lưu trữ nào dùng được để gợi ý. Hãy xếp tay.", MaLoiDichVu.DuLieuKhongHopLe);

        List<int> Duong(ViTri v)
        {
            var ds = new List<int>();
            for (ViTri? x = v; x is not null && ds.Count < 20; x = x.MaViTriCha is int cha ? theoMa.GetValueOrDefault(cha) : null) ds.Insert(0, x.MaViTri);
            return ds;
        }
        var cua = viTri.Where(v => v.LoaiViTri == LoaiViTriGiaTri.TiepNhan).OrderBy(v => v.MaViTri).FirstOrDefault();
        var duongCua = cua is null ? null : Duong(cua);
        double Gan(ViTri o)
        {
            if (duongCua is null) return 0.5;
            var duong = Duong(o);
            var chung = 0;
            while (chung < duong.Count && chung < duongCua.Count && duong[chung] == duongCua[chung]) chung++;
            return 1.0 / (1 + (duong.Count - chung) + (duongCua.Count - chung));
        }

        // Hàng đang nằm trong các ô (để tính điểm cạnh hàng cùng loại) và kích thước, khối lượng của các biến thể cần xếp.
        var maO = oXep.Select(o => o.MaViTri).ToList();
        var trongO = await (from t in db.TonKho.AsNoTracking()
                            where maO.Contains(t.MaViTri) && t.SoLuongTon > 0
                            join l in db.LoHang.AsNoTracking() on t.MaLo equals l.MaLo
                            join b in db.BienThe.AsNoTracking() on l.MaBT equals b.MaBT
                            join s in db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
                            select new { t.MaViTri, b.MaBT, b.MaSP, s.MaDM }).Distinct().ToListAsync(ct);
        var maBT = dong.Select(d => d.MaBT).Distinct().ToList();
        var bienThe = await (from b in db.BienThe.AsNoTracking()
                             where maBT.Contains(b.MaBT)
                             join s in db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
                             select new { b.MaBT, b.MaSP, s.MaDM, s.TenSP, b.ThuocTinh, b.Dai, b.Rong, b.Cao, b.KhoiLuong }).ToDictionaryAsync(x => x.MaBT, ct);

        var daDung = new Dictionary<int, (decimal TheTich, decimal Khoi)>();
        var sucChuaCache = new Dictionary<(int MaViTri, int MaBT), KetQuaSucChua>();
        var chiTiet = new List<ChiTietGoiYDto>();
        var khongXep = new List<string>();
        foreach (var d in dong)
        {
            var bt = bienThe[d.MaBT];
            var theTich1 = bt.Dai * bt.Rong * bt.Cao;
            var canXep = d.SLDat!.Value - daLap.GetValueOrDefault(d.MaCTN);
            var ten = (bt.TenSP + " " + bt.ThuocTinh).Trim();

            var ktHang = $"{Canh(bt.Dai)}×{Canh(bt.Rong)}×{Canh(bt.Cao)} cm";
            var ungVien = new List<QuyTacG2.OUngVien>();
            foreach (var o in oXep)
            {
                // Hỏi ICapacityService (đã kiểm tra lọt lòng 3D, hàng đang có và hàng chờ xếp) để lấy thể tích, khối lượng còn lại của ô; lưu đệm theo cặp (Ô, Biến thể).
                if (!sucChuaCache.TryGetValue((o.MaViTri, d.MaBT), out var sc))
                {
                    sc = await sucChua.KiemTra(o.MaViTri, d.MaBT, 1, ct);
                    sucChuaCache[(o.MaViTri, d.MaBT)] = sc;
                }
                if ((sc.SoLuongToiDaThem ?? 0) <= 0) continue;
                var cungHang = trongO.Where(x => x.MaViTri == o.MaViTri)
                    .Select(x => x.MaBT == d.MaBT ? 1.0 : x.MaSP == bt.MaSP ? 0.7 : x.MaDM == bt.MaDM ? 0.4 : 0.0)
                    .DefaultIfEmpty(0.0).Max();
                var ktO = o.Dai is not null ? $"{Canh(o.Dai.Value)}×{Canh(o.Rong ?? 0)}×{Canh(o.Cao ?? 0)} cm" : null;
                ungVien.Add(new QuyTacG2.OUngVien(o.MaViTri, o.TenViTri, sc.TheTichConLaiCm3, sc.KhoiLuongConLaiKg, Gan(o), cungHang, ktO));
            }

            var phan = QuyTacG2.PhanBo(canXep, theTich1, bt.KhoiLuong, ungVien, daDung, lenhCu.Where(s => s.MaCTN == d.MaCTN).Select(s => s.SoLuong).ToList());
            if (phan is null) { khongXep.Add($"{ten} (cần xếp {canXep}): các ô lưu trữ không vừa kích thước 3D hoặc không đủ chỗ"); continue; }
            foreach (var x in phan)
            {
                var (theTichDung, khoiDung) = daDung.GetValueOrDefault(x.O.MaViTri);
                var lyDo = TaoLyDoGoiY(x, ten, ktHang, x.O.TheTichCon - theTichDung, x.O.KhoiCon - khoiDung);
                daDung[x.O.MaViTri] = (theTichDung + x.SoLuong * theTich1, khoiDung + x.SoLuong * bt.KhoiLuong);
                chiTiet.Add(new ChiTietGoiYDto(d.MaBT, x.SoLuong, lyDo, d.MaLo, x.O.MaViTri));
            }
        }

        if (chiTiet.Count == 0) return KetQua<int>.Loi("Không có gợi ý nào: " + string.Join("; ", khongXep) + ". Hãy xếp tay.", MaLoiDichVu.VuotSucChua);
        var kq = await goiY.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: maPN), chiTiet, ct);
        if (!kq.ThanhCong) return kq;

        await audit.Ghi(maND, "Tạo gợi ý G2", LoaiChungTu.NK, maPN, $"{p.SoPhieu}: {chiTiet.Count} dòng đề xuất", ct);
        var luu = khongXep.Count == 0 ? "" : " Chưa gợi ý được: " + string.Join("; ", khongXep) + " (xếp tay các dòng này).";
        return KetQua<int>.Ok(kq.GiaTri, $"Đã tạo gợi ý G2 cho phiếu {p.SoPhieu}: {chiTiet.Count} dòng đề xuất, chờ duyệt.{luu}");
    }

    private static string So(double v) => v.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Canh(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    public static string TaoLyDoGoiY(
        QuyTacG2.PhanXep x,
        string ten,
        string? kichThuocBT = null,
        decimal? theTichConThuc = null,
        decimal? khoiConThuc = null)
    {
        var theTich = theTichConThuc ?? x.O.TheTichCon;
        var khoi = khoiConThuc ?? x.O.KhoiCon;
        var moTaKt = kichThuocBT is not null && x.O.KichThuocO is not null
            ? $" Kích thước hàng ({kichThuocBT}) vừa lòng ô ({x.O.KichThuocO})."
            : "";
        return $"Điểm S = {So(x.Diem)} (Heuristic đa tiêu chí): gần khu tiếp nhận ({So(x.O.Gan)}), cạnh hàng cùng loại ({So(x.O.CungHang)}), độ lấp đầy phù hợp ({So(x.LapDay)})." +
               moTaKt +
               $" Xếp {x.SoLuong} {ten} vào ô {x.O.TenViTri}; trước khi xếp ô còn {Math.Round(theTich)} cm³ và {Math.Round(khoi, 2)} kg, không vượt thể tích và khối lượng.";
    }
}

/// <summary>
/// Bộ xử lý gợi ý G2 cho khung gợi ý AI dùng chung (T4-8): khi thủ kho duyệt (hoặc sửa rồi duyệt), lập lệnh sắp xếp tới đúng các ô đã chọn bằng
/// <see cref="SapXepService.LapLenh"/>, nên mọi ràng buộc (phiếu Đã kiểm tra, ô lưu trữ cùng kho, ô không khóa, sức chứa) được kiểm lại, kể cả khi gợi ý đã cũ
/// hoặc người duyệt chọn ô khác (lỗi nêu rõ thiếu thể tích hay khối lượng). Không chạm hàng thật: thủ kho vẫn quét mã ô và mã lô để xác nhận từng lệnh.
/// Một dòng lỗi thì khung hoàn tác cả lần duyệt, gợi ý vẫn Chờ duyệt và thủ kho lập lệnh tay được.
/// </summary>
public sealed class HandlerG2(IStockService ton, ICapacityService sucChua, IAuditService audit, KhoDbContext? db = null) : IGoiYHandler
{
    public LoaiGoiY Loai => LoaiGoiY.G2;

    public async Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default)
    {
        if (db is null)
            return KetQua<ThamChieuGoiY>.Loi("Chưa cấu hình ConnectionStrings:KhoDb nên chưa duyệt được gợi ý G2.", MaLoiDichVu.ChuaCoXuLy);
        if (goiY.ThamChieu.MaPN is not int maPN)
            return KetQua<ThamChieuGoiY>.Loi("Gợi ý G2 phải gắn phiếu nhập.", MaLoiDichVu.DuLieuKhongHopLe);

        var p = await db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == maPN, ct);
        if (p is null) return KetQua<ThamChieuGoiY>.Loi("Không còn thấy phiếu nhập của gợi ý.", MaLoiDichVu.KhongTimThay);

        var maCTN = p.ChiTiet.Select(d => d.MaCTN).ToList();
        var daLap = (await db.SapXep.AsNoTracking().Where(s => maCTN.Contains(s.MaCTN) && s.TrangThai != SapXep.DaHuy).ToListAsync(ct))
            .GroupBy(s => s.MaCTN).ToDictionary(g => g.Key, g => g.Sum(s => s.SoLuong));
        var conLai = p.ChiTiet.ToDictionary(d => d.MaCTN, d => (d.SLDat ?? 0) - daLap.GetValueOrDefault(d.MaCTN));   // hàng đạt còn chưa lập lệnh của từng dòng

        var sx = new SapXepService(db, ton, sucChua, audit);
        foreach (var c in goiY.ChiTiet)
        {
            if (c.MaViTri is not int maO)
                return KetQua<ThamChieuGoiY>.Loi($"Mỗi dòng gợi ý G2 phải có ô (biến thể #{c.MaBT}).", MaLoiDichVu.DuLieuKhongHopLe);
            var d = p.ChiTiet.OrderBy(x => x.MaCTN)
                .FirstOrDefault(x => x.MaBT == c.MaBT && (c.MaLo is null || x.MaLo == c.MaLo) && conLai[x.MaCTN] >= c.SoLuong);
            if (d is null)
                return KetQua<ThamChieuGoiY>.Loi($"Gợi ý có dòng biến thể #{c.MaBT} số lượng {c.SoLuong} không khớp hàng đạt còn chưa xếp của phiếu; phiếu có thể đã đổi sau khi tạo gợi ý.", MaLoiDichVu.DuLieuKhongHopLe);

            var kq = await sx.LapLenh(d.MaCTN, maO, c.SoLuong, false, maNguoiDuyet, ct);   // không tự chia: đúng ô đã duyệt hoặc báo lỗi
            if (!kq.ThanhCong) return KetQua<ThamChieuGoiY>.Loi(kq.ThongBao ?? "Không lập được lệnh sắp xếp.", kq.MaLoi);
            conLai[d.MaCTN] -= c.SoLuong;
        }
        return KetQua<ThamChieuGoiY>.Ok(new ThamChieuGoiY(MaPN: maPN));
    }
}
