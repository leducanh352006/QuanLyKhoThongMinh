using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

public enum TrangThaiHienThiO
{
    Trong,
    ConCho,
    SapDay,
    DangKhoa,
    NgungHoatDong,
    TiepNhan
}

public enum LoaiKetQuaThuO
{
    DuCho,
    QuaKichThuoc3D,
    ThieuSucChua,
    BiChan
}

public sealed record BienTheThuDto(
    int MaBT,
    string TenHienThi,
    decimal Dai,
    decimal Rong,
    decimal Cao,
    decimal KhoiLuong)
{
    public string KichThuocText =>
        $"{DinhDang(Dai)}×{DinhDang(Rong)}×{DinhDang(Cao)} cm · {DinhDang(KhoiLuong)} kg";

    private static string DinhDang(decimal v) =>
        v.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
}

public sealed record KetQuaThuODto(
    LoaiKetQuaThuO Loai,
    int? SoLuongToiDaThem,
    string NhanNgan,
    string ChiTiet);

public sealed record OThongTinTrucQuanDto(
    ViTriCayDto Nut,
    int SoLuongTon,
    int SoLuongGiu,
    int SoLuongChoXep,
    decimal TheTichDaDungCm3,
    decimal TongTheTichCm3,
    int PhanTramTheTich,
    decimal KhoiLuongDaDungKg,
    decimal TongKhoiLuongKg,
    int PhanTramKhoiLuong,
    TrangThaiHienThiO TrangThaiHienThi,
    KetQuaThuODto? ThuKichThuoc)
{
    public int MucLapDayPhanTram => Math.Max(PhanTramTheTich, PhanTramKhoiLuong);
}

public sealed record KeTrucQuanDto(
    int? MaKe,
    string TenKe,
    string DuongDan,
    bool DangHoatDong,
    IReadOnlyList<OThongTinTrucQuanDto> DanhSachO);

public sealed record KhuVucTrucQuanDto(
    int? MaKhu,
    string TenKhu,
    string CapKhu,
    string DuongDan,
    bool DangHoatDong,
    IReadOnlyList<KeTrucQuanDto> DanhSachKe);

public sealed record ViTriTrangViewModel(
    int MaKho,
    string TenKho,
    string CheDoXem,
    IReadOnlyList<ViTriCayDto> CayGoc,
    IReadOnlyList<OThongTinTrucQuanDto> KhuTiepNhan,
    IReadOnlyList<KhuVucTrucQuanDto> KhuLuuTru,
    int TongOLuuTru,
    int OTrong,
    int OConCho,
    int OSapDay,
    int OKhoaHoacNgung,
    IReadOnlyList<BienTheThuDto> DanhSachBienThe,
    BienTheThuDto? BienTheDangThu,
    int? SoLuongDangThu,
    int SoODuChoThu,
    int TongSucChuaThemDuocThu);

public interface IViTriTrucQuanService
{
    Task<ViTriTrangViewModel> XayDung(
        int maKho,
        string tenKho,
        int? maBTThu,
        int? soLuongThu,
        string? cheDoXem = null,
        CancellationToken ct = default);
}

/// <summary>
/// Tổng hợp dữ liệu Sơ đồ Vị trí kho trực quan (Khu/Dãy → Kệ → Lưới Ô, % thể tích, % khối lượng và kiểm tra nhanh vừa ô 3D).
/// Hoạt động đồng nhất cả khi có SQL Server lẫn chế độ bộ nhớ mẫu Development.
/// </summary>
public sealed class ViTriTrucQuanService(
    IViTriQuanLy viTriQuanLy,
    IStockService stock,
    ICapacityService capacity,
    INguonHangChoXep choXep,
    KhoDbContext? db = null) : IViTriTrucQuanService
{
    private readonly ICapacityService _ = capacity;

    public async Task<ViTriTrangViewModel> XayDung(
        int maKho,
        string tenKho,
        int? maBTThu,
        int? soLuongThu,
        string? cheDoXem = null,
        CancellationToken ct = default)
    {
        var kqCay = await viTriQuanLy.LayCay(maKho, ct);
        var cay = kqCay.GiaTri ?? Array.Empty<ViTriCayDto>();
        var dsBienThe = await LayDanhSachBienThe(ct);
        var theoMaBT = dsBienThe.ToDictionary(x => x.MaBT);

        BienTheThuDto? btThu = maBTThu is int m && theoMaBT.TryGetValue(m, out var found) ? found : null;
        int? slThu = btThu is not null && (soLuongThu ?? 0) > 0 ? soLuongThu : null;

        // Thu thập toàn bộ nút Ô trong cây để nạp tồn & sức chứa một lượt.
        var tatCaNutO = new List<ViTriCayDto>();
        ThuThapO(cay, tatCaNutO);

        var suDungTheoO = await TinhSuDungTheoO(tatCaNutO, theoMaBT, ct);
        var thongTinTheoMa = new Dictionary<int, OThongTinTrucQuanDto>();
        foreach (var nut in tatCaNutO)
        {
            var sd = suDungTheoO.GetValueOrDefault(nut.MaViTri);
            thongTinTheoMa[nut.MaViTri] = TaoThongTinO(nut, sd, btThu, slThu);
        }

        var khuTiepNhan = tatCaNutO
            .Where(n => n.LoaiViTri == LoaiViTriGiaTri.TiepNhan)
            .Select(n => thongTinTheoMa[n.MaViTri])
            .ToList();

        var khuLuuTru = GomNhomKhuVaKe(cay, thongTinTheoMa);
        var tatCaOLuuTru = tatCaNutO
            .Where(n => n.LoaiViTri != LoaiViTriGiaTri.TiepNhan)
            .Select(n => thongTinTheoMa[n.MaViTri])
            .ToList();

        int oTrong = 0, oConCho = 0, oSapDay = 0, oKhoaHoacNgung = 0;
        int soODuChoThu = 0, tongThemThu = 0;

        foreach (var o in tatCaOLuuTru)
        {
            switch (o.TrangThaiHienThi)
            {
                case TrangThaiHienThiO.DangKhoa:
                case TrangThaiHienThiO.NgungHoatDong:
                    oKhoaHoacNgung++;
                    break;
                case TrangThaiHienThiO.SapDay:
                    oSapDay++;
                    break;
                case TrangThaiHienThiO.ConCho:
                    oConCho++;
                    break;
                default:
                    oTrong++;
                    break;
            }

            if (o.ThuKichThuoc is not null)
            {
                if (o.ThuKichThuoc.Loai == LoaiKetQuaThuO.DuCho)
                    soODuChoThu++;
                tongThemThu += Math.Max(0, o.ThuKichThuoc.SoLuongToiDaThem ?? 0);
            }
        }

        var cheDo = string.Equals(cheDoXem, "cay", StringComparison.OrdinalIgnoreCase) ? "cay" : "sodo";
        return new ViTriTrangViewModel(
            maKho,
            tenKho,
            cheDo,
            cay,
            khuTiepNhan,
            khuLuuTru,
            tatCaOLuuTru.Count,
            oTrong,
            oConCho,
            oSapDay,
            oKhoaHoacNgung,
            dsBienThe,
            btThu,
            slThu,
            soODuChoThu,
            tongThemThu);
    }

    private sealed record SuDungO(int Ton, int Giu, int ChoXep, decimal TheTichCm3, decimal KhoiKg);

    private async Task<IReadOnlyList<BienTheThuDto>> LayDanhSachBienThe(CancellationToken ct)
    {
        if (db is not null)
        {
            var rows = await (
                from b in db.BienThe.AsNoTracking()
                join s in db.SanPham.AsNoTracking() on b.MaSP equals s.MaSP
                orderby s.TenSP, b.MaBT
                select new { b.MaBT, s.TenSP, b.ThuocTinh, b.Dai, b.Rong, b.Cao, b.KhoiLuong })
                .ToListAsync(ct);

            if (rows.Count > 0)
            {
                return rows.Select(r => new BienTheThuDto(
                    r.MaBT,
                    string.IsNullOrWhiteSpace(r.ThuocTinh) ? r.TenSP : $"{r.TenSP} — {r.ThuocTinh}",
                    r.Dai, r.Rong, r.Cao, r.KhoiLuong)).ToList();
            }
        }

        var spSeed = SeedDataSet.SanPham.ToDictionary(s => s.MaSP, s => s.TenSP);
        return SeedDataSet.BienThe
            .Select(b => new BienTheThuDto(
                b.MaBT,
                $"{spSeed.GetValueOrDefault(b.MaSP, "Sản phẩm")} — {b.ThuocTinh}",
                b.Dai, b.Rong, b.Cao, b.KhoiLuong))
            .ToList();
    }

    private async Task<Dictionary<int, SuDungO>> TinhSuDungTheoO(
        IReadOnlyList<ViTriCayDto> dsO,
        IReadOnlyDictionary<int, BienTheThuDto> theoMaBT,
        CancellationToken ct)
    {
        var kq = new Dictionary<int, SuDungO>();
        if (dsO.Count == 0) return kq;

        var maOs = dsO.Select(o => o.MaViTri).ToList();
        var loSangBT = new Dictionary<int, int>();

        if (db is not null)
        {
            loSangBT = await db.LoHang.AsNoTracking()
                .ToDictionaryAsync(l => l.MaLo, l => l.MaBT, ct);
        }
        else
        {
            foreach (var l in SeedDataSet.LoHang)
                loSangBT[l.MaLo] = l.MaBT;
        }

        foreach (var o in dsO)
        {
            int ton = 0, giu = 0, slCho = 0;
            decimal vol = 0m, kg = 0m;

            var dongTons = await stock.LayTonTheoViTri(o.MaViTri, ct);
            foreach (var d in dongTons)
            {
                ton += d.SoLuongTon;
                giu += d.SoLuongGiu;
                if (loSangBT.TryGetValue(d.MaLo, out var maBT) && theoMaBT.TryGetValue(maBT, out var bt))
                {
                    vol += d.SoLuongTon * bt.Dai * bt.Rong * bt.Cao;
                    kg += d.SoLuongTon * bt.KhoiLuong;
                }
            }

            var dongChos = await choXep.LayTheoO(o.MaViTri, ct);
            foreach (var c in dongChos)
            {
                slCho += c.SoLuong;
                if (theoMaBT.TryGetValue(c.MaBT, out var bt))
                {
                    vol += c.SoLuong * bt.Dai * bt.Rong * bt.Cao;
                    kg += c.SoLuong * bt.KhoiLuong;
                }
            }

            kq[o.MaViTri] = new SuDungO(ton, giu, slCho, vol, kg);
        }

        return kq;
    }

    private static OThongTinTrucQuanDto TaoThongTinO(
        ViTriCayDto nut,
        SuDungO? sd,
        BienTheThuDto? btThu,
        int? slThu)
    {
        var ton = sd?.Ton ?? 0;
        var giu = sd?.Giu ?? 0;
        var cho = sd?.ChoXep ?? 0;
        var volDung = sd?.TheTichCm3 ?? 0m;
        var kgDung = sd?.KhoiKg ?? 0m;

        var tongVol = (nut.Dai ?? 0m) * (nut.Rong ?? 0m) * (nut.Cao ?? 0m);
        var tongKg = nut.KhoiLuongToiDa ?? 0m;

        var pctVol = TinhPhanTram(volDung, tongVol, ton + cho > 0);
        var pctKg = TinhPhanTram(kgDung, tongKg, ton + cho > 0);
        var maxPct = Math.Max(pctVol, pctKg);

        TrangThaiHienThiO trangThai;
        if (nut.LoaiViTri == LoaiViTriGiaTri.TiepNhan)
            trangThai = TrangThaiHienThiO.TiepNhan;
        else if (!nut.DangHoatDong)
            trangThai = TrangThaiHienThiO.NgungHoatDong;
        else if (nut.DangKhoa)
            trangThai = TrangThaiHienThiO.DangKhoa;
        else if (maxPct >= 80)
            trangThai = TrangThaiHienThiO.SapDay;
        else if (ton + cho > 0 || maxPct > 0)
            trangThai = TrangThaiHienThiO.ConCho;
        else
            trangThai = TrangThaiHienThiO.Trong;

        KetQuaThuODto? ketQuaThu = null;
        if (btThu is not null && slThu is int canXep && nut.LoaiViTri != LoaiViTriGiaTri.TiepNhan)
        {
            var entity = new ViTri
            {
                MaViTri = nut.MaViTri,
                MaKho = nut.MaKho,
                TenViTri = nut.TenViTri,
                CapViTri = nut.CapViTri,
                LoaiViTri = nut.LoaiViTri,
                MaVachO = nut.MaVachO,
                Dai = nut.Dai,
                Rong = nut.Rong,
                Cao = nut.Cao,
                KhoiLuongToiDa = nut.KhoiLuongToiDa,
                DangKhoa = nut.DangKhoa,
                TrangThai = nut.TrangThai
            };

            var kq = QuyTacSucChua.Tinh(entity, btThu.Dai, btThu.Rong, btThu.Cao, btThu.KhoiLuong, canXep, volDung, kgDung);
            if (kq.Du)
            {
                ketQuaThu = new KetQuaThuODto(
                    LoaiKetQuaThuO.DuCho,
                    kq.SoLuongToiDaThem,
                    $"Đặt vừa · Tối đa +{kq.SoLuongToiDaThem}",
                    $"Nhận đủ {canXep} đơn vị (còn nhận tối đa +{kq.SoLuongToiDaThem}).");
            }
            else if (!nut.DangHoatDong || nut.DangKhoa)
            {
                ketQuaThu = new KetQuaThuODto(
                    LoaiKetQuaThuO.BiChan,
                    0,
                    nut.DangKhoa ? "Đang khóa kiểm kê" : "Ngừng hoạt động",
                    kq.LyDo ?? "Ô không khả dụng.");
            }
            else if (!QuyTacSucChua.VuaKichThuoc3Chieu(btThu.Dai, btThu.Rong, btThu.Cao, nut.Dai, nut.Rong, nut.Cao))
            {
                ketQuaThu = new KetQuaThuODto(
                    LoaiKetQuaThuO.QuaKichThuoc3D,
                    0,
                    "Quá khổ 3 chiều (D×R×C)",
                    kq.LyDo ?? "Kích thước biến thể vượt lòng ô.");
            }
            else
            {
                var toiDa = kq.SoLuongToiDaThem ?? 0;
                var nhan = toiDa > 0 ? $"Chỉ nhận +{toiDa}/{canXep}" : "Hết tải trọng / thể tích";
                ketQuaThu = new KetQuaThuODto(
                    LoaiKetQuaThuO.ThieuSucChua,
                    toiDa,
                    nhan,
                    kq.LyDo ?? "Vượt sức chứa còn lại của ô.");
            }
        }

        return new OThongTinTrucQuanDto(
            nut, ton, giu, cho,
            volDung, tongVol, pctVol,
            kgDung, tongKg, pctKg,
            trangThai, ketQuaThu);
    }

    private static int TinhPhanTram(decimal daDung, decimal tong, bool coHang)
    {
        if (tong <= 0m || daDung <= 0m) return 0;
        var pct = (int)Math.Round(daDung * 100m / tong, MidpointRounding.AwayFromZero);
        if (pct == 0 && coHang) return 1;
        return Math.Min(100, pct);
    }

    private static void ThuThapO(IEnumerable<ViTriCayDto> ds, List<ViTriCayDto> ketQua)
    {
        foreach (var n in ds)
        {
            if (n.LaO) ketQua.Add(n);
            if (n.Con.Count > 0) ThuThapO(n.Con, ketQua);
        }
    }

    private static List<KhuVucTrucQuanDto> GomNhomKhuVaKe(
        IReadOnlyList<ViTriCayDto> cay,
        IReadOnlyDictionary<int, OThongTinTrucQuanDto> thongTinTheoMa)
    {
        var dsKhu = new List<KhuVucTrucQuanDto>();
        var oLuuTruGoc = new List<OThongTinTrucQuanDto>();

        foreach (var goc in cay)
        {
            if (goc.LaO)
            {
                if (goc.LoaiViTri != LoaiViTriGiaTri.TiepNhan)
                    oLuuTruGoc.Add(thongTinTheoMa[goc.MaViTri]);
                continue;
            }

            var dsKe = new List<KeTrucQuanDto>();
            ThuThapKe(goc, null, thongTinTheoMa, dsKe);
            dsKhu.Add(new KhuVucTrucQuanDto(goc.MaViTri, goc.TenViTri, goc.CapViTri, goc.DuongDanDayDu, goc.DangHoatDong, dsKe));
        }

        if (oLuuTruGoc.Count > 0)
        {
            dsKhu.Add(new KhuVucTrucQuanDto(
                null,
                "Ô lưu trữ độc lập",
                "Gốc",
                "",
                true,
                [new KeTrucQuanDto(null, "Ô lưu trữ cấp gốc", "", true, oLuuTruGoc)]));
        }

        return dsKhu;
    }

    private static void ThuThapKe(
        ViTriCayDto cha,
        string? tienTo,
        IReadOnlyDictionary<int, OThongTinTrucQuanDto> thongTinTheoMa,
        List<KeTrucQuanDto> dsKe)
    {
        var oTrucTiep = cha.Con
            .Where(c => c.LaO && c.LoaiViTri != LoaiViTriGiaTri.TiepNhan)
            .Select(c => thongTinTheoMa[c.MaViTri])
            .ToList();

        var conTrungGian = cha.Con.Where(c => !c.LaO).ToList();

        if (oTrucTiep.Count > 0)
        {
            var tenKe = tienTo is null
                ? (cha.CapViTri == CapViTriGiaTri.Ke ? cha.TenViTri : $"Ô trực thuộc {cha.TenViTri}")
                : tienTo;
            dsKe.Add(new KeTrucQuanDto(cha.MaViTri, tenKe, cha.DuongDanDayDu, cha.DangHoatDong, oTrucTiep));
        }
        else if (conTrungGian.Count == 0 && cha.CapViTri == CapViTriGiaTri.Ke)
        {
            // Kệ trống chưa tạo ô con nào vẫn hiển thị để quản lý kho bấm thêm ô.
            var tenKe = tienTo ?? cha.TenViTri;
            dsKe.Add(new KeTrucQuanDto(cha.MaViTri, tenKe, cha.DuongDanDayDu, cha.DangHoatDong, Array.Empty<OThongTinTrucQuanDto>()));
        }

        foreach (var con in conTrungGian)
        {
            var nhanMoi = tienTo is null ? con.TenViTri : $"{tienTo} ▷ {con.TenViTri}";
            ThuThapKe(con, nhanMoi, thongTinTheoMa, dsKe);
        }
    }
}
