using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QuanLyKho.Tests.HangHoa;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-7 (Hủy phiếu, danh sách): điều kiện hủy, hoàn tác tồn, nhật ký, tìm và lọc danh sách.</summary>
public class PhieuNhapServiceTests
{
    private const int Nguoi = TonKhoSeeder.MaNguoiGhiTonMau;

    // ---- Không cần SQL Server ----

    [Theory]
    [InlineData("Nháp", true)]
    [InlineData("Đã xác nhận", true)]
    [InlineData("Đã kiểm tra", true)]
    [InlineData("Hoàn tất", false)]
    [InlineData("Đã hủy", false)]
    public void HuyDuoc_ChiPhieuChuaHoanTatVaChuaHuy(string trangThai, bool mong) =>
        Assert.Equal(mong, new PhieuNhap { TrangThai = trangThai }.HuyDuoc);

    [Fact]
    public void TatCaTrangThai_DuNamTrangThaiBang222() =>
        Assert.Equal(new[] { "Nháp", "Đã xác nhận", "Đã kiểm tra", "Hoàn tất", "Đã hủy" }, PhieuNhap.TatCaTrangThai);

    [Fact]
    public void HuyDuoc_KhongPhaiCotTrongCsdl()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var pn = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PhieuNhap))!;
        Assert.Null(pn.FindProperty("HuyDuoc"));
        Assert.Null(pn.FindProperty("DaKhoa"));
    }

    [Fact]
    public void DauNgayUtc_LaNuaDemNgayTheoGioMayChu()
    {
        var ngay = new DateOnly(2026, 11, 2);
        Assert.Equal(DateTime.SpecifyKind(new DateTime(2026, 11, 2), DateTimeKind.Local).ToUniversalTime(), PhieuNhapService.DauNgayUtc(ngay));
        Assert.Equal(DateTimeKind.Utc, PhieuNhapService.DauNgayUtc(ngay).Kind);
    }

    // ---- Cần SQL Server đã migrate và seed: mỗi bài chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu ----

    private sealed record MoiTruong(KhoDbContext Db, PhieuNhapService Pn, StockService Ton, NhatKyGia NhatKy, ViTri O, ViTri TiepNhan, LoHang Lo, NhaCungCap Ncc);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var ton = new StockService(db);
        var nhatKy = new NhatKyGia();
        var o = await db.ViTri.AsNoTracking().Where(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru
                                                         && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa).OrderBy(v => v.MaViTri).FirstAsync();
        var tiepNhan = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaKho == o.MaKho && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan);
        var lo = await db.LoHang.AsNoTracking().OrderBy(l => l.MaLo).FirstAsync();
        var ncc = await db.NhaCungCap.AsNoTracking().OrderBy(n => n.MaNCC).FirstAsync();

        await chay(new MoiTruong(db, new PhieuNhapService(db, ton, nhatKy), ton, nhatKy, o, tiepNhan, lo, ncc));
    }

    /// <summary>Phiếu một dòng. Trạng thái Đã kiểm tra thì <paramref name="soLuong"/> hàng đang nằm ở ô Tiếp nhận (như sau khi T1-2 tiếp nhận xong), trừ khi <paramref name="ghiTon"/> là false.</summary>
    private static async Task<(PhieuNhap Phieu, int MaCTN)> TaoPhieuAsync(MoiTruong e, string trangThai, int soLuong = 2, bool ghiTon = true)
    {
        var daKiem = trangThai is PhieuNhap.DaKiemTra or PhieuNhap.HoanTat;
        var p = new PhieuNhap
        {
            SoPhieu = "T17" + Guid.NewGuid().ToString("N")[..10], LoaiPhieu = "Mua hàng", ThoiGian = DateTime.UtcNow, MaKho = e.O.MaKho, MaNCC = e.Ncc.MaNCC,
            MaNguoiLap = Nguoi, TrangThai = trangThai,
            ChiTiet = { daKiem
                ? new ChiTietPhieuNhap { MaBT = e.Lo.MaBT, SLDuKien = soLuong, MaLo = e.Lo.MaLo, SLThucNhan = soLuong, SLDat = soLuong }
                : new ChiTietPhieuNhap { MaBT = e.Lo.MaBT, SLDuKien = soLuong } },
        };
        e.Db.PhieuNhap.Add(p);
        await e.Db.SaveChangesAsync();
        if (daKiem && ghiTon)
        {
            var tang = await e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.TiepNhan.MaViTri, soLuong), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, p.MaPN), Nguoi);
            Assert.True(tang.ThanhCong, tang.ThongBao);
        }
        return (p, p.ChiTiet[0].MaCTN);
    }

    private static Task<string> TrangThaiPhieu(KhoDbContext db, int maPN) =>
        db.PhieuNhap.AsNoTracking().Where(p => p.MaPN == maPN).Select(p => p.TrangThai).SingleAsync();

    private static Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon).FirstOrDefaultAsync();

    private static async Task<int> LenhSapXepAsync(MoiTruong e, int maCTN, string trangThai, int soLuong)
    {
        var xn = trangThai == SapXep.DaXacNhan;
        var sx = new SapXep { MaCTN = maCTN, MaViTri = e.O.MaViTri, SoLuong = soLuong, TrangThai = trangThai, MaNguoiXN = xn ? Nguoi : null, ThoiGianXN = xn ? DateTime.UtcNow : null };
        e.Db.SapXep.Add(sx);
        await e.Db.SaveChangesAsync();
        return sx.MaSX;
    }

    [CanSqlServerFact]
    public async Task Huy_PhieuNhap_ChuyenDaHuyVaGhiNhatKyCoLyDo() => await DungAsync(async e =>
    {
        var (p, _) = await TaoPhieuAsync(e, PhieuNhap.Nhap);

        var kq = await e.Pn.Huy(p.MaPN, "  Nhà cung cấp báo hủy đơn  ", Nguoi);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(PhieuNhap.DaHuy, await TrangThaiPhieu(e.Db, p.MaPN));
        var dong = Assert.Single(e.NhatKy.Dong);
        Assert.Equal("Hủy phiếu nhập", dong.HanhDong);
        Assert.Equal(Nguoi, dong.MaND);
        Assert.Contains("Nhà cung cấp báo hủy đơn", dong.ChiTiet);
        Assert.Contains(p.SoPhieu, dong.ChiTiet);
    });

    [CanSqlServerFact]
    public async Task Huy_ThieuLyDoHoacLyDoQuaDai_KhongDoiGiCa() => await DungAsync(async e =>
    {
        var (p, _) = await TaoPhieuAsync(e, PhieuNhap.DaXacNhan);

        foreach (var lyDo in new[] { null, "", "   ", new string('a', PhieuNhapService.LyDoToiDa + 1) })
        {
            var kq = await e.Pn.Huy(p.MaPN, lyDo, Nguoi);
            Assert.False(kq.ThanhCong);
            Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
        }
        Assert.Equal(PhieuNhap.DaXacNhan, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.Empty(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task Huy_PhieuHoanTatDaHuyHoacKhongCo_BiTuChoi() => await DungAsync(async e =>
    {
        var (hoanTat, _) = await TaoPhieuAsync(e, PhieuNhap.HoanTat);
        var (daHuy, _) = await TaoPhieuAsync(e, PhieuNhap.DaHuy);

        foreach (var p in new[] { hoanTat, daHuy })
        {
            var kq = await e.Pn.Huy(p.MaPN, "thử", Nguoi);
            Assert.False(kq.ThanhCong);
            Assert.Equal(MaLoiDichVu.SaiTrangThai, kq.MaLoi);
        }
        var khong = await e.Pn.Huy(int.MaxValue, "thử", Nguoi);
        Assert.Equal(MaLoiDichVu.KhongTimThay, khong.MaLoi);
        Assert.Equal(PhieuNhap.HoanTat, await TrangThaiPhieu(e.Db, hoanTat.MaPN));
        Assert.Empty(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task Huy_HaiLan_LanHaiBiTuChoiVaChiGhiMotNhatKy() => await DungAsync(async e =>
    {
        var (p, _) = await TaoPhieuAsync(e, PhieuNhap.DaXacNhan);

        var mot = await e.Pn.Huy(p.MaPN, "lần một", Nguoi);
        var hai = await e.Pn.Huy(p.MaPN, "lần hai", Nguoi);

        Assert.True(mot.ThanhCong, mot.ThongBao);
        Assert.False(hai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, hai.MaLoi);
        Assert.Single(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task Huy_PhieuDaKiemTra_HoanTacHangKhoiKhuTiepNhanVaHuyLenhCho() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuAsync(e, PhieuNhap.DaKiemTra, 3);
        var lenh = await LenhSapXepAsync(e, ctn, SapXep.ChoXacNhan, 2);
        var tnTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri);

        var kq = await e.Pn.Huy(p.MaPN, "giao nhầm hàng", Nguoi);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(PhieuNhap.DaHuy, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.Equal(tnTruoc - 3, await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri));   // hàng ra khỏi khu tiếp nhận
        Assert.Equal(SapXep.DaHuy, await e.Db.SapXep.AsNoTracking().Where(s => s.MaSX == lenh).Select(s => s.TrangThai).SingleAsync());
        var bd = await e.Db.BienDongTon.AsNoTracking().SingleAsync(b => b.MaCT == p.MaPN && b.LoaiCT == "NK" && b.LoaiBienDong == LoaiBienDongGiaTri.HoanTac);
        Assert.Equal(-3, bd.SoLuong);
        Assert.Equal(e.TiepNhan.MaViTri, bd.MaViTri);
        Assert.DoesNotContain(await e.Ton.DoiChieuSoCai(), c => c.MaLo == e.Lo.MaLo);      // sổ cái vẫn khớp tồn
        Assert.Single(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task Huy_DaCoDongXepVaoO_BiTuChoiVaKhongDoiGiCa() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuAsync(e, PhieuNhap.DaKiemTra, 3);
        var lenhCho = await LenhSapXepAsync(e, ctn, SapXep.ChoXacNhan, 1);
        await LenhSapXepAsync(e, ctn, SapXep.DaXacNhan, 2);
        var tnTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri);

        var kq = await e.Pn.Huy(p.MaPN, "muốn hủy", Nguoi);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, kq.MaLoi);
        Assert.Equal(PhieuNhap.DaKiemTra, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.Equal(tnTruoc, await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri));
        Assert.Equal(SapXep.ChoXacNhan, await e.Db.SapXep.AsNoTracking().Where(s => s.MaSX == lenhCho).Select(s => s.TrangThai).SingleAsync());
        Assert.Empty(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task Huy_KhuTiepNhanKhongConDuHang_HoanTacHetVaPhieuGiuNguyen() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuAsync(e, PhieuNhap.DaKiemTra, 3, ghiTon: false);   // phiếu ghi đã kiểm tra nhưng hàng không có ở khu tiếp nhận
        var lenh = await LenhSapXepAsync(e, ctn, SapXep.ChoXacNhan, 1);

        var kq = await e.Pn.Huy(p.MaPN, "thử", Nguoi);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotTon, kq.MaLoi);
        Assert.Equal(PhieuNhap.DaKiemTra, await TrangThaiPhieu(e.Db, p.MaPN));   // bước chuyển trạng thái và hủy lệnh đã được hoàn tác
        Assert.Equal(SapXep.ChoXacNhan, await e.Db.SapXep.AsNoTracking().Where(s => s.MaSX == lenh).Select(s => s.TrangThai).SingleAsync());
        Assert.Empty(e.NhatKy.Dong);
    });

    [CanSqlServerFact]
    public async Task TimKiem_TheoSoPhieuNhaCungCapNgayVaTrangThai() => await DungAsync(async e =>
    {
        var (p, _) = await TaoPhieuAsync(e, PhieuNhap.DaXacNhan);
        var homNay = DateOnly.FromDateTime(DateTime.Now);

        var theoSo = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu.ToLowerInvariant()));
        var theoNcc = await e.Pn.TimKiem(new BoLocPhieu(Tim: e.Ncc.TenNCC, TuNgay: homNay, DenNgay: homNay));
        var dungTrangThai = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu, TrangThai: PhieuNhap.DaXacNhan));
        var saiTrangThai = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu, TrangThai: PhieuNhap.Nhap));
        var ngayCu = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu, TuNgay: homNay.AddDays(-30), DenNgay: homNay.AddDays(-1)));
        var ngayTuongLai = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu, TuNgay: homNay.AddDays(1)));
        var homNayDu = await e.Pn.TimKiem(new BoLocPhieu(Tim: p.SoPhieu, TuNgay: homNay, DenNgay: homNay));

        Assert.Equal(p.MaPN, Assert.Single(theoSo.Phieu).MaPN);                 // không phân biệt hoa thường
        Assert.Contains(theoNcc.Phieu, x => x.MaPN == p.MaPN);
        Assert.Equal(p.MaPN, Assert.Single(dungTrangThai.Phieu).MaPN);
        Assert.Empty(saiTrangThai.Phieu);
        Assert.Empty(ngayCu.Phieu);
        Assert.Empty(ngayTuongLai.Phieu);
        Assert.Equal(p.MaPN, Assert.Single(homNayDu.Phieu).MaPN);               // "đến ngày" tính cả ngày đó
        Assert.NotNull(homNayDu.Phieu[0].Kho);
        Assert.Single(homNayDu.Phieu[0].ChiTiet);
    });

    [CanSqlServerFact]
    public async Task TimKiem_PhieuMoiNhatTruocVaKhongVuotToiDa() => await DungAsync(async e =>
    {
        var (cu, _) = await TaoPhieuAsync(e, PhieuNhap.Nhap);
        var (moi, _) = await TaoPhieuAsync(e, PhieuNhap.Nhap);

        var kq = await e.Pn.TimKiem(new BoLocPhieu());

        Assert.True(kq.Phieu.Count <= PhieuNhapService.ToiDa);
        Assert.True(kq.Phieu.FindIndex(x => x.MaPN == moi.MaPN) < kq.Phieu.FindIndex(x => x.MaPN == cu.MaPN));
    });
}
