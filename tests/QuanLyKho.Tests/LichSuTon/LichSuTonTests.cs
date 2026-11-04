using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.HeThong.Controllers;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T4-6 (Lịch sử biến động tồn): lọc theo lô, ô, loại chứng từ, thời gian; đối chiếu sổ cái (dùng lại T4-4);
/// lịch sử xuất theo ngày cho G3, G4. Phần SQL Server chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu.
/// </summary>
public class LichSuTonTests
{
    private const int Nguoi = TonKhoSeeder.MaNguoiGhiTonMau;

    // Dữ liệu mẫu (SeedDataSet): lô 1 (áo M, biến thể 1) ở ô 3 và ô 10; lô 5 (tai nghe, biến thể 4) ở ô 6 và ô 11.
    // Ô 3, 4, 6 thuộc Kho 1; ô 10, 11 thuộc Kho 2.

    private static BoNhoHeThongStore TaoNguoiDung()
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedPassword"] = "Demo@12345" }).Build();
        return new BoNhoHeThongStore(cauHinh, new MatKhauService(), NullLogger<BoNhoHeThongStore>.Instance);
    }

    private static LichSuTonService TaoDichVu(StockServiceStub stock) =>
        new(new BoNhoLichSuTonStore(stock), stock, TaoNguoiDung(), TimeProvider.System);

    private static async Task Ok(Task<KetQua> viec)
    {
        var kq = await viec;
        Assert.True(kq.ThanhCong, kq.ThongBao);
    }

    /// <summary>Ghi 3 biến động: xuất lô 1 ô 3 (-5), nhận lô 1 ô 10 (+4), chuyển lô 5 từ ô 6 sang ô 11 (-2, +2).</summary>
    private static async Task GhiMau(StockServiceStub stock)
    {
        await Ok(stock.GhiGiam(new DongTon(1, 3, 5), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 100), Nguoi));
        await Ok(stock.GhiTang(new DongTon(1, 10, 4), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, 200), Nguoi));
        await Ok(stock.ChuyenO(5, 6, 11, 2, new ThamChieuChungTu(LoaiChungTu.CK, 300), Nguoi));
    }

    // ---- Lọc theo lô, ô, loại chứng từ, loại biến động ----

    [Fact]
    public async Task Tim_LocTheoLo_ChiTraDongCuaLoDo()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        var dv = TaoDichVu(stock);

        var kq = await dv.Tim(new LichSuTonLoc(MaLo: 1));

        Assert.Equal(2, kq.Tong);
        Assert.All(kq.Dong, d => { Assert.Equal(1, d.MaLo); Assert.Equal("LO-2610-001", d.MaVachLo); });
    }

    [Fact]
    public async Task Tim_LocTheoLoVaO_ChiTraDungMotDong()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        var dv = TaoDichVu(stock);

        var kq = await dv.Tim(new LichSuTonLoc(MaLo: 1, MaViTri: 3));

        var d = Assert.Single(kq.Dong);
        Assert.Equal(-5, d.SoLuong);
        Assert.Equal("Xuất kho", d.LoaiBienDong);
        Assert.Equal("XK", d.LoaiCT);
        Assert.Equal(100, d.MaCT);
        Assert.Equal("Ô A1-01", d.TenViTri);
        Assert.Equal("Quản lý kho (mẫu)", d.NguoiThucHien);
    }

    [Fact]
    public async Task Tim_LocTheoO_ThayCaHaiLoCuaO()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        await Ok(stock.GhiTang(new DongTon(2, 3, 1), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, 201), Nguoi));
        var dv = TaoDichVu(stock);

        var kq = await dv.Tim(new LichSuTonLoc(MaViTri: 3));

        Assert.Equal(new[] { 1, 2 }, kq.Dong.Select(d => d.MaLo).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task Tim_LocTheoLoaiChungTuVaLoaiBienDong()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        var dv = TaoDichVu(stock);

        var chuyen = await dv.Tim(new LichSuTonLoc(LoaiCT: LoaiChungTu.CK));
        var xuat = await dv.Tim(new LichSuTonLoc(LoaiBienDong: LoaiBienDongGiaTri.XuatKho));
        var doiO = await dv.Tim(new LichSuTonLoc(LoaiBienDong: LoaiBienDongGiaTri.DoiO));
        var theoMa = await dv.Tim(new LichSuTonLoc(LoaiCT: LoaiChungTu.NK, MaCT: 200));

        Assert.Equal(2, chuyen.Tong);   // chuyển ô ghi hai dòng: âm ở ô nguồn, dương ở ô đích
        Assert.Equal(1, xuat.Tong);
        Assert.Equal(2, doiO.Tong);
        Assert.Equal(4, Assert.Single(theoMa.Dong).SoLuong);
    }

    [Fact]
    public async Task Tim_LocKhongKhop_TraRong()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);

        var kq = await TaoDichVu(stock).Tim(new LichSuTonLoc(MaLo: 4, MaViTri: 3));

        Assert.Empty(kq.Dong);
        Assert.Equal(0, kq.Tong);
    }

    [Fact]
    public async Task Tim_LocTheoThoiGian_TinhCaHaiDau()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        var dv = TaoDichVu(stock);
        var bay = DateTime.UtcNow;

        var trongKhoang = await dv.Tim(new LichSuTonLoc(TuUtc: bay.AddHours(-1), DenUtc: bay.AddHours(1)));
        var sau = await dv.Tim(new LichSuTonLoc(TuUtc: bay.AddHours(1)));
        var truoc = await dv.Tim(new LichSuTonLoc(DenUtc: bay.AddHours(-1)));

        Assert.Equal(4, trongKhoang.Tong);   // 1 xuất + 1 nhận + 2 dòng chuyển ô
        Assert.Equal(0, sau.Tong);
        Assert.Equal(0, truoc.Tong);
    }

    [Fact]
    public async Task Tim_PhanTrang_MoiTruocCuSauVaChanGiaTriSai()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);
        var dv = TaoDichVu(stock);

        var t1 = await dv.Tim(new LichSuTonLoc(Trang: 1, CoTrang: 3));
        var t2 = await dv.Tim(new LichSuTonLoc(Trang: 2, CoTrang: 3));
        var sai = await dv.Tim(new LichSuTonLoc(Trang: -5, CoTrang: 100000));

        Assert.Equal(4, t1.Tong);
        Assert.Equal(2, t1.TongTrang);
        Assert.Equal(3, t1.Dong.Count);
        Assert.Single(t2.Dong);
        Assert.True(t1.Dong[0].MaBD > t1.Dong[1].MaBD);   // mới trước, cũ sau
        Assert.True(t1.Dong[^1].MaBD > t2.Dong[0].MaBD);
        Assert.Equal(1, sai.Trang);
        Assert.Equal(LichSuTonLoc.CoTrangToiDa, sai.CoTrang);
    }

    // ---- Đối chiếu sổ cái: liên kết với T4-4 (IStockService.DoiChieuSoCai) ----

    [Fact]
    public async Task DoiChieu_KhopTrenDuLieuMau()
    {
        var kq = await TaoDichVu(new StockServiceStub()).DoiChieu();

        Assert.True(kq.Khop);
        Assert.Empty(kq.Chenh);
    }

    [Fact]
    public async Task DoiChieu_VanKhopSauCacThaoTacQuaStockService()
    {
        var stock = new StockServiceStub();
        await GhiMau(stock);

        var kq = await TaoDichVu(stock).DoiChieu();

        Assert.True(kq.Khop, "Mọi thao tác đi qua IStockService đều ghi sổ cái nên tổng sổ cái phải bằng TonKho.");
    }

    [Fact]
    public async Task DoiChieu_LayKetQuaTuIStockService_KhongTuTinhLai()
    {
        var chenh = new[] { new ChenhLechSoCai(1, 3, 51, 53) };
        var dv = new LichSuTonService(new BoNhoLichSuTonStore(new StockServiceStub()), new StockGiaChenhLech(chenh), TaoNguoiDung(), TimeProvider.System);

        var kq = await dv.DoiChieu();

        Assert.False(kq.Khop);
        Assert.Equal(chenh, kq.Chenh);
    }

    /// <summary>IStockService giả chỉ để trả danh sách chênh lệch cho DoiChieuSoCai; các hàm khác không dùng tới.</summary>
    private sealed class StockGiaChenhLech(IReadOnlyList<ChenhLechSoCai> chenh) : IStockService
    {
        public Task<IReadOnlyList<ChenhLechSoCai>> DoiChieuSoCai(CancellationToken ct = default) => Task.FromResult(chenh);
        public Task<KetQua> GiuCho(DongTon dong, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> TraCho(DongTon dong, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> GhiTang(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> GhiGiam(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, bool tuGiuCho = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> ChuyenO(int maLo, int maViTriNguon, int maViTriDich, int soLuong, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<TonKhoDto?> LayTon(int maLo, int maViTri, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TonKhoDto>> LayTonTheoViTri(int maViTri, CancellationToken ct = default) => throw new NotSupportedException();
    }

    // ---- Lịch sử xuất theo ngày cho G3, G4 ----

    private static readonly TimeZoneInfo GioVietNam = TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");

    [Fact]
    public void TongHopXuatTheoNgay_GomTheoNgayGioMayChu_BienTheVaKho()
    {
        var dong = new[]
        {
            new XuatThanh(new DateTime(2026, 11, 1, 16, 30, 0, DateTimeKind.Utc), 1, 1, -3),   // 23:30 ngày 1 (UTC+7)
            new XuatThanh(new DateTime(2026, 11, 1, 2, 0, 0, DateTimeKind.Utc), 1, 1, -2),     // 09:00 ngày 1
            new XuatThanh(new DateTime(2026, 11, 1, 17, 30, 0, DateTimeKind.Utc), 1, 1, -4),   // 00:30 ngày 2
            new XuatThanh(new DateTime(2026, 11, 1, 2, 0, 0, DateTimeKind.Utc), 1, 2, -1),     // cùng ngày, khác kho
            new XuatThanh(new DateTime(2026, 11, 1, 2, 0, 0, DateTimeKind.Utc), 4, 1, -6),     // cùng ngày, khác biến thể
        };

        var kq = LichSuTonService.TongHopXuatTheoNgay(dong, GioVietNam);

        Assert.Equal(new[]
        {
            new XuatTheoNgayDto(new DateOnly(2026, 11, 1), 1, 1, 5),
            new XuatTheoNgayDto(new DateOnly(2026, 11, 1), 4, 1, 6),
            new XuatTheoNgayDto(new DateOnly(2026, 11, 1), 1, 2, 1),
            new XuatTheoNgayDto(new DateOnly(2026, 11, 2), 1, 1, 4),
        }.OrderBy(x => x.Ngay).ThenBy(x => x.MaKho).ThenBy(x => x.MaBT).ToArray(), kq);
    }

    [Fact]
    public void TongHopXuatTheoNgay_BoNhomKhongCoXuatRong()
    {
        var bay = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
        var dong = new[] { new XuatThanh(bay, 1, 1, -5), new XuatThanh(bay, 1, 1, 5), new XuatThanh(bay, 2, 1, 3) };

        var kq = LichSuTonService.TongHopXuatTheoNgay(dong, GioVietNam);

        Assert.Empty(kq);   // xuất ròng bằng 0 hoặc dương (không phải xuất) thì không báo là nhu cầu
    }

    [Fact]
    public async Task LichSuXuatTheoNgay_ChiTinhXuatKho_LocTheoKhoVaBienThe()
    {
        var stock = new StockServiceStub();
        await Ok(stock.GhiGiam(new DongTon(1, 3, 5), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 1), Nguoi));    // biến thể 1, kho 1
        await Ok(stock.GhiGiam(new DongTon(1, 10, 2), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 2), Nguoi));   // biến thể 1, kho 2
        await Ok(stock.GhiGiam(new DongTon(5, 6, 3), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 3), Nguoi));    // biến thể 4, kho 1
        await Ok(stock.GhiGiam(new DongTon(1, 3, 7), LoaiBienDong.ChuyenDi, new ThamChieuChungTu(LoaiChungTu.CK, 4), Nguoi));   // chuyển kho không phải xuất
        await Ok(stock.GhiTang(new DongTon(1, 3, 9), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, 5), Nguoi));   // nhận hàng không phải xuất
        var dv = TaoDichVu(stock);
        var homNay = DateOnly.FromDateTime(DateTime.Now);

        var tatCa = await dv.LichSuXuatTheoNgay(homNay.AddDays(-1), homNay.AddDays(1));
        var kho2 = await dv.LichSuXuatTheoNgay(homNay, homNay, maKho: 2);
        var bienThe4 = await dv.LichSuXuatTheoNgay(homNay, homNay, maBT: 4);

        Assert.Equal(3, tatCa.Count);
        Assert.Equal(10, tatCa.Sum(x => x.SoLuong));   // 5 + 2 + 3; không có 7 chuyển kho, không có 9 nhận hàng
        Assert.All(tatCa, x => { Assert.Equal(homNay, x.Ngay); Assert.True(x.SoLuong > 0); });
        Assert.Equal(new XuatTheoNgayDto(homNay, 1, 2, 2), Assert.Single(kho2));
        Assert.Equal(new XuatTheoNgayDto(homNay, 4, 1, 3), Assert.Single(bienThe4));
    }

    [Fact]
    public async Task LichSuXuatTheoNgay_KhoangNgayNgoaiHoacDaoNguoc_TraRong()
    {
        var stock = new StockServiceStub();
        await Ok(stock.GhiGiam(new DongTon(1, 3, 5), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 1), Nguoi));
        var dv = TaoDichVu(stock);
        var homNay = DateOnly.FromDateTime(DateTime.Now);

        Assert.Empty(await dv.LichSuXuatTheoNgay(homNay.AddDays(-10), homNay.AddDays(-2)));
        Assert.Empty(await dv.LichSuXuatTheoNgay(homNay.AddDays(1), homNay.AddDays(-1)));
    }

    // ---- Phân quyền, menu, đăng ký DI ----

    [Fact]
    public void Controller_ChiQuanTriVienVaQuanLyKho_VaKhongCoHanhDongGhi()
    {
        var kieu = typeof(LichSuTonController);
        var quyen = kieu.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(quyen);
        var vaiTro = quyen!.Roles!.Split(',');
        Assert.Contains(VaiTroTen.QuanTriVien, vaiTro);
        Assert.Contains(VaiTroTen.QuanLyKho, vaiTro);
        Assert.DoesNotContain(VaiTroTen.ThuKho, vaiTro);
        Assert.DoesNotContain(VaiTroTen.NhanVienBanHang, vaiTro);
        Assert.Equal("HeThong", kieu.GetCustomAttribute<Microsoft.AspNetCore.Mvc.AreaAttribute>()!.RouteValue);
        Assert.DoesNotContain(kieu.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            m => m.GetCustomAttributes().Any(a => a is Microsoft.AspNetCore.Mvc.HttpPostAttribute));   // sổ cái chỉ đọc
    }

    [Fact]
    public void Menu_CoMucLichSuTonChoQuanTriVienVaQuanLyKho()
    {
        var muc = Assert.Single(new HeThongMenu().LayNhom().Muc, m => m.Controller == "LichSuTon");

        Assert.Equal("Index", muc.Action);
        Assert.Equal("HeThong", muc.Area);
        Assert.Contains(VaiTroTen.QuanTriVien, muc.VaiTro);
        Assert.Contains(VaiTroTen.QuanLyKho, muc.VaiTro);
        Assert.DoesNotContain(VaiTroTen.ThuKho, muc.VaiTro);
    }

    [Fact]
    public void DangKy_CoDichVuLichSuTonChoManHinhVaChoNhomKhac()
    {
        var services = new ServiceCollection().AddKhoInfrastructure().AddHeThong();

        Assert.Contains(services, d => d.ServiceType == typeof(ILichSuTonQuanLy));
        Assert.Contains(services, d => d.ServiceType == typeof(ILichSuTonService));
        Assert.Contains(services, d => d.ServiceType == typeof(ILichSuTonStore));
    }

    // ---- Cần SQL Server đã migrate và seed: mỗi bài chạy trong một giao dịch rồi hoàn tác ----

    private sealed record MoiTruong(KhoDbContext Db, StockService Ton, LichSuTonService LichSu, ViTri O, QuanLyKho.Web.Areas.HangHoa.Models.LoHang Lo);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var ton = new StockService(db);
        var lichSu = new LichSuTonService(new EfLichSuTonStore(db), ton, new EfHeThongStore(db), TimeProvider.System);
        var o = await db.ViTri.AsNoTracking().Where(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru
                                                     && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa).OrderBy(v => v.MaViTri).FirstAsync();
        var lo = await db.LoHang.AsNoTracking().OrderBy(l => l.MaLo).FirstAsync();

        await chay(new MoiTruong(db, ton, lichSu, o, lo));
    }

    private static async Task<int> LechAsync(IStockService ton, int maLo, int maViTri) =>
        (await ton.DoiChieuSoCai()).Where(c => c.MaLo == maLo && c.MaViTri == maViTri).Select(c => c.TonHienTai - c.TonTheoSoCai).SingleOrDefault();

    [CanSqlServerFact]
    public async Task Ef_Tim_ThayDongVuaGhiQuaStockService_LocTheoLoVaO() => await DungAsync(async e =>
    {
        const int maCT = 987_654;
        await Ok(e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.O.MaViTri, 3), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, maCT), Nguoi));

        var kq = await e.LichSu.Tim(new LichSuTonLoc(MaLo: e.Lo.MaLo, MaViTri: e.O.MaViTri, LoaiCT: LoaiChungTu.CK, MaCT: maCT));

        var d = Assert.Single(kq.Dong);
        Assert.Equal(3, d.SoLuong);
        Assert.Equal(LoaiBienDongGiaTri.NhanChuyen, d.LoaiBienDong);
        Assert.Equal(e.Lo.MaVachLo, d.MaVachLo);
        Assert.Equal(e.O.TenViTri, d.TenViTri);
        Assert.False(string.IsNullOrWhiteSpace(d.NguoiThucHien));
        Assert.DoesNotContain("Người dùng", d.NguoiThucHien);

        var khac = await e.LichSu.Tim(new LichSuTonLoc(MaLo: e.Lo.MaLo, MaViTri: e.O.MaViTri, LoaiCT: LoaiChungTu.XK, MaCT: maCT));
        Assert.Empty(khac.Dong);
    });

    [CanSqlServerFact]
    public async Task Ef_ThaoTacQuaStockService_KhongLamLechSoCai() => await DungAsync(async e =>
    {
        var truoc = await LechAsync(e.Ton, e.Lo.MaLo, e.O.MaViTri);

        await Ok(e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.O.MaViTri, 5), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, 987_655), Nguoi));
        await Ok(e.Ton.GhiGiam(new DongTon(e.Lo.MaLo, e.O.MaViTri, 2), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 987_656), Nguoi));

        Assert.Equal(truoc, await LechAsync(e.Ton, e.Lo.MaLo, e.O.MaViTri));   // sổ cái và TonKho cùng đổi nên độ lệch không đổi
        Assert.True((await e.LichSu.DoiChieu()).ThoiGianUtc > DateTime.UtcNow.AddMinutes(-1));
    });

    [CanSqlServerFact]
    public async Task Ef_DoiChieu_PhatHienKhiTonBiSuaNgoaiStockService() => await DungAsync(async e =>
    {
        await Ok(e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.O.MaViTri, 3), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, 987_657), Nguoi));
        var truoc = await LechAsync(e.Ton, e.Lo.MaLo, e.O.MaViTri);

        // Giả lập lỗi: ai đó sửa thẳng TonKho mà không ghi sổ cái.
        await e.Db.TonKho.Where(t => t.MaLo == e.Lo.MaLo && t.MaViTri == e.O.MaViTri)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.SoLuongTon, t => t.SoLuongTon + 7));

        var kq = await e.LichSu.DoiChieu();

        Assert.False(kq.Khop);
        var c = Assert.Single(kq.Chenh, x => x.MaLo == e.Lo.MaLo && x.MaViTri == e.O.MaViTri);
        Assert.Equal(truoc + 7, c.TonHienTai - c.TonTheoSoCai);
    });

    [CanSqlServerFact]
    public async Task Ef_LichSuXuatTheoNgay_ThayLuongXuatVuaGhi() => await DungAsync(async e =>
    {
        var homNay = DateOnly.FromDateTime(DateTime.Now);
        async Task<int> DaXuat() => (await e.LichSu.LichSuXuatTheoNgay(homNay, homNay, e.O.MaKho, e.Lo.MaBT)).Sum(x => x.SoLuong);
        var truoc = await DaXuat();

        await Ok(e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.O.MaViTri, 5), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, 987_658), Nguoi));
        await Ok(e.Ton.GhiGiam(new DongTon(e.Lo.MaLo, e.O.MaViTri, 2), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 987_659), Nguoi));
        await Ok(e.Ton.GhiGiam(new DongTon(e.Lo.MaLo, e.O.MaViTri, 1), LoaiBienDong.ChuyenDi, new ThamChieuChungTu(LoaiChungTu.CK, 987_660), Nguoi));

        Assert.Equal(truoc + 2, await DaXuat());   // chỉ dòng Xuất kho được tính, chuyển đi và nhận chuyển thì không
    });
}
