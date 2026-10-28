using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.NhatKyDinhKem;

/// <summary>Dựng sẵn nhật ký và đính kèm trên kho dữ liệu trong bộ nhớ, thư mục tệp tạm; xóa thư mục khi xong.</summary>
internal sealed class MoiTruongNhatKyThu : IDisposable
{
    public const int QuanTri = 1, QuanLyKho = 2;

    public string ThuMuc { get; } = Path.Combine(Path.GetTempPath(), "qlk-dinhkem-" + Guid.NewGuid().ToString("N"));
    public BoNhoNhatKyStore KhoNhatKy { get; } = new();
    public BoNhoTepDinhKemStore KhoTep { get; } = new();
    public IHeThongStore NguoiDung { get; }
    public AuditService Audit { get; }
    public NhatKyService NhatKy { get; }
    public TepDinhKemService Tep { get; }

    public MoiTruongNhatKyThu(long toiDaByte = QuyTacTep.ToiDaByteMacDinh)
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedPassword"] = "Demo@12345" }).Build();
        NguoiDung = new BoNhoHeThongStore(cauHinh, new MatKhauService(), NullLogger<BoNhoHeThongStore>.Instance);
        Audit = new AuditService(KhoNhatKy, TimeProvider.System);
        NhatKy = new NhatKyService(KhoNhatKy, NguoiDung);
        Tep = new TepDinhKemService(KhoTep, NguoiDung, Audit, new TepLuuTruCauHinh(ThuMuc, toiDaByte), TimeProvider.System);
    }

    public void Dispose()
    {
        if (Directory.Exists(ThuMuc)) Directory.Delete(ThuMuc, recursive: true);
    }
}

public class AuditServiceTests
{
    [Fact]
    public async Task Ghi_LuuDungNguoiHanhDongVaChungTu()
    {
        using var e = new MoiTruongNhatKyThu();
        await e.Audit.Ghi(MoiTruongNhatKyThu.QuanLyKho, "Duyệt phiếu", LoaiChungTu.XK, 5, "đủ hàng");

        var dong = Assert.Single(await e.Audit.LayTheoChungTu(LoaiChungTu.XK, 5));
        Assert.Equal(MoiTruongNhatKyThu.QuanLyKho, dong.MaND);
        Assert.Equal("Duyệt phiếu", dong.HanhDong);
        Assert.Equal(LoaiChungTu.XK, dong.LoaiCT);
        Assert.Equal(5, dong.MaCT);
        Assert.Equal("đủ hàng", dong.ChiTiet);
        Assert.True((DateTime.UtcNow - dong.ThoiGianUtc).TotalMinutes < 1);
    }

    [Fact]
    public async Task LayTheoChungTu_ChiLayDungChungTu_CuTruocMoiSau()
    {
        using var e = new MoiTruongNhatKyThu();
        await e.Audit.Ghi(1, "tạo", LoaiChungTu.NK, 7);
        await e.Audit.Ghi(2, "duyệt", LoaiChungTu.NK, 7);
        await e.Audit.Ghi(2, "tạo", LoaiChungTu.NK, 8);
        await e.Audit.Ghi(2, "tạo", LoaiChungTu.XK, 7);
        await e.Audit.Ghi(1, "đăng nhập");

        var ls = await e.Audit.LayTheoChungTu(LoaiChungTu.NK, 7);
        Assert.Equal(new[] { "tạo", "duyệt" }, ls.Select(n => n.HanhDong).ToArray());
    }

    [Fact]
    public async Task Ghi_DauVaoSai_BiTuChoi_ChiTietQuaDaiDuocCat()
    {
        using var e = new MoiTruongNhatKyThu();
        await Assert.ThrowsAsync<ArgumentException>(() => e.Audit.Ghi(1, "tạo", LoaiChungTu.XK, null));
        await Assert.ThrowsAsync<ArgumentException>(() => e.Audit.Ghi(1, "tạo", null, 5));
        await Assert.ThrowsAsync<ArgumentException>(() => e.Audit.Ghi(1, "   "));
        await Assert.ThrowsAsync<ArgumentException>(() => e.Audit.Ghi(1, new string('a', AuditService.DoDaiToiDaHanhDong + 1)));

        await e.Audit.Ghi(1, "tạo", LoaiChungTu.NK, 1, new string('x', 5000));
        var dong = Assert.Single(await e.Audit.LayTheoChungTu(LoaiChungTu.NK, 1));
        Assert.Equal(AuditService.DoDaiToiDaChiTiet, dong.ChiTiet!.Length);
    }

    [Fact]
    public async Task Tim_LocTheoNguoi_HanhDong_ChungTu_ThoiGian_MoiTruocCuSau()
    {
        using var e = new MoiTruongNhatKyThu();
        await e.Audit.Ghi(MoiTruongNhatKyThu.QuanTri, "Đăng nhập");
        await e.Audit.Ghi(MoiTruongNhatKyThu.QuanLyKho, "Duyệt phiếu xuất", LoaiChungTu.XK, 3);
        await e.Audit.Ghi(MoiTruongNhatKyThu.QuanLyKho, "Hủy phiếu nhập", LoaiChungTu.NK, 4);

        Assert.Equal(3, (await e.NhatKy.Tim(new NhatKyLoc())).Tong);
        Assert.Equal(new[] { "Hủy phiếu nhập", "Duyệt phiếu xuất" },
            (await e.NhatKy.Tim(new NhatKyLoc(MaND: MoiTruongNhatKyThu.QuanLyKho))).Dong.Select(d => d.HanhDong).ToArray());
        Assert.Single((await e.NhatKy.Tim(new NhatKyLoc(HanhDong: "duyệt"))).Dong);   // không phân biệt hoa thường
        Assert.Single((await e.NhatKy.Tim(new NhatKyLoc(LoaiCT: LoaiChungTu.NK))).Dong);
        Assert.Single((await e.NhatKy.Tim(new NhatKyLoc(LoaiCT: LoaiChungTu.XK, MaCT: 3))).Dong);
        Assert.Equal(3, (await e.NhatKy.Tim(new NhatKyLoc(TuUtc: DateTime.UtcNow.AddMinutes(-1), DenUtc: DateTime.UtcNow.AddMinutes(1)))).Tong);
        Assert.Equal(0, (await e.NhatKy.Tim(new NhatKyLoc(TuUtc: DateTime.UtcNow.AddMinutes(1)))).Tong);
        Assert.Equal(0, (await e.NhatKy.Tim(new NhatKyLoc(DenUtc: DateTime.UtcNow.AddMinutes(-1)))).Tong);
    }

    [Fact]
    public async Task Tim_KemTenNguoi_PhanTrangVaChanGiaTriSai()
    {
        using var e = new MoiTruongNhatKyThu();
        for (var i = 1; i <= 7; i++) await e.Audit.Ghi(MoiTruongNhatKyThu.QuanTri, $"thao tác {i}");

        var t1 = await e.NhatKy.Tim(new NhatKyLoc(Trang: 1, CoTrang: 3));
        Assert.Equal(7, t1.Tong);
        Assert.Equal(3, t1.TongTrang);
        Assert.Equal(new[] { "thao tác 7", "thao tác 6", "thao tác 5" }, t1.Dong.Select(d => d.HanhDong).ToArray());
        Assert.All(t1.Dong, d => Assert.False(string.IsNullOrWhiteSpace(d.NguoiThucHien)));

        Assert.Single((await e.NhatKy.Tim(new NhatKyLoc(Trang: 3, CoTrang: 3))).Dong);
        Assert.Equal(1, (await e.NhatKy.Tim(new NhatKyLoc(Trang: -5, CoTrang: 3))).Trang);
        Assert.Equal(NhatKyLoc.CoTrangToiDa, (await e.NhatKy.Tim(new NhatKyLoc(CoTrang: 100000))).CoTrang);
    }
}

public class QuyTacTepTests
{
    [Theory]
    [InlineData("bien-ban.pdf", "bien-ban.pdf")]
    [InlineData(@"C:\Users\a\Hóa đơn 01.PNG", "Hóa đơn 01.PNG")]
    [InlineData("../../etc/anh.jpg", "anh.jpg")]
    [InlineData("  ten co dau cach.xlsx  ", "ten co dau cach.xlsx")]
    public void ChuanHoaTen_BoDuongDanVaKyTuLa(string goc, string mong) => Assert.Equal(mong, QuyTacTep.ChuanHoaTen(goc));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("virus.exe")]
    [InlineData("trang.html")]
    [InlineData("script.js")]
    [InlineData("khongduoi")]
    [InlineData(".pdf")]
    public void ChuanHoaTen_RongHoacDuoiCam_TraNull(string? goc) => Assert.Null(QuyTacTep.ChuanHoaTen(goc));

    [Fact]
    public void ChuanHoaTen_TenQuaDai_DuocCatNhungGiuDuoi()
    {
        var ten = QuyTacTep.ChuanHoaTen(new string('a', 500) + ".pdf")!;
        Assert.Equal(QuyTacTep.DoDaiToiDaTenTep, ten.Length);
        Assert.EndsWith(".pdf", ten);
    }

    [Fact]
    public void DuongDanDayDu_ChanThoatKhoiThuMucGoc()
    {
        var goc = Path.Combine(Path.GetTempPath(), "qlk-goc");
        Assert.NotNull(QuyTacTep.DuongDanDayDu(goc, "NK/1/abc.pdf"));
        Assert.Null(QuyTacTep.DuongDanDayDu(goc, "../ngoai.pdf"));
        Assert.Null(QuyTacTep.DuongDanDayDu(goc, "NK/../../ngoai.pdf"));
        Assert.Null(QuyTacTep.DuongDanDayDu(goc, Path.Combine(Path.GetTempPath(), "qlk-goc-khac", "x.pdf")));
    }

    [Fact]
    public void DuongDanLuu_KhongChuaTenNguoiDung_MoiLanMotTenKhacNhau()
    {
        var a = QuyTacTep.DuongDanLuu("NK", 12, "Hóa đơn.PDF");
        var b = QuyTacTep.DuongDanLuu("NK", 12, "Hóa đơn.PDF");
        Assert.NotEqual(a, b);
        Assert.StartsWith("NK/12/", a);
        Assert.EndsWith(".pdf", a);
        Assert.DoesNotContain("Hóa", a);
    }
}

public class TepDinhKemServiceTests
{
    private static MemoryStream Noi(string s) => new(System.Text.Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task Them_LuuTep_XemLaiDuocDungNoiDung_VaGhiNhatKy()
    {
        using var e = new MoiTruongNhatKyThu();
        var kq = await e.Tep.Them(LoaiChungTu.NK, 12, "Hóa đơn.pdf", Noi("noi dung hoa don"), MoiTruongNhatKyThu.QuanLyKho);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal("Hóa đơn.pdf", kq.GiaTri!.TenTep);

        var ds = await e.Tep.LayTheoChungTu(LoaiChungTu.NK, 12);
        var tep = Assert.Single(ds);
        Assert.False(string.IsNullOrWhiteSpace(tep.NguoiTai));
        Assert.Empty(await e.Tep.LayTheoChungTu(LoaiChungTu.NK, 13));
        Assert.Empty(await e.Tep.LayTheoChungTu(LoaiChungTu.XK, 12));

        var mo = await e.Tep.Mo(tep.MaTep);
        Assert.True(mo.ThanhCong);
        await using (mo.GiaTri!.Luong)
        using (var doc = new StreamReader(mo.GiaTri.Luong))
            Assert.Equal("noi dung hoa don", await doc.ReadToEndAsync());
        Assert.Equal("application/pdf", mo.GiaTri.LoaiNoiDung);

        var nhatKy = Assert.Single(await e.Audit.LayTheoChungTu(LoaiChungTu.NK, 12));
        Assert.Equal("Đính kèm tệp", nhatKy.HanhDong);
        Assert.Equal("Hóa đơn.pdf", nhatKy.ChiTiet);
        Assert.Equal((LoaiChungTu.NK, 12), await e.Tep.ChungTuCuaTep(tep.MaTep));
    }

    [Fact]
    public async Task Them_NhieuTepCungTen_KhongGhiDeNhau()
    {
        using var e = new MoiTruongNhatKyThu();
        var a = (await e.Tep.Them(LoaiChungTu.XK, 1, "anh.png", Noi("A"), MoiTruongNhatKyThu.QuanLyKho)).GiaTri!;
        var b = (await e.Tep.Them(LoaiChungTu.XK, 1, "anh.png", Noi("B"), MoiTruongNhatKyThu.QuanLyKho)).GiaTri!;

        foreach (var (tep, mong) in new[] { (a, "A"), (b, "B") })
        {
            var mo = await e.Tep.Mo(tep.MaTep);
            using var doc = new StreamReader(mo.GiaTri!.Luong);
            Assert.Equal(mong, await doc.ReadToEndAsync());
        }
    }

    [Fact]
    public async Task Them_TepRong_QuaDungLuong_DuoiCam_BiTuChoi_VaKhongDeLaiTepMoCoi()
    {
        using var e = new MoiTruongNhatKyThu(toiDaByte: 10);

        var rong = await e.Tep.Them(LoaiChungTu.NK, 1, "a.pdf", new MemoryStream(), MoiTruongNhatKyThu.QuanLyKho);
        Assert.False(rong.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, rong.MaLoi);

        var lon = await e.Tep.Them(LoaiChungTu.NK, 1, "a.pdf", Noi("0123456789ABC"), MoiTruongNhatKyThu.QuanLyKho);
        Assert.False(lon.ThanhCong);

        var exe = await e.Tep.Them(LoaiChungTu.NK, 1, "a.exe", Noi("x"), MoiTruongNhatKyThu.QuanLyKho);
        Assert.False(exe.ThanhCong);

        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, (await e.Tep.Them(LoaiChungTu.NK, 0, "a.pdf", Noi("x"), MoiTruongNhatKyThu.QuanLyKho)).MaLoi);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.Tep.Them(LoaiChungTu.NK, 1, "a.pdf", Noi("x"), 999_999)).MaLoi);

        Assert.Empty(await e.Tep.LayTheoChungTu(LoaiChungTu.NK, 1));
        Assert.Empty(await e.Audit.LayTheoChungTu(LoaiChungTu.NK, 1));
        Assert.Empty(Directory.Exists(e.ThuMuc) ? Directory.GetFiles(e.ThuMuc, "*", SearchOption.AllDirectories) : Array.Empty<string>());
    }

    [Fact]
    public async Task Mo_KhongCoBanGhi_HoacMatTepTrenDia_BaoLoiRoRang()
    {
        using var e = new MoiTruongNhatKyThu();
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.Tep.Mo(12345)).MaLoi);
        Assert.Null(await e.Tep.ChungTuCuaTep(12345));

        var tep = (await e.Tep.Them(LoaiChungTu.KK, 2, "bb.txt", Noi("x"), MoiTruongNhatKyThu.QuanLyKho)).GiaTri!;
        foreach (var f in Directory.GetFiles(e.ThuMuc, "*", SearchOption.AllDirectories)) File.Delete(f);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.Tep.Mo(tep.MaTep)).MaLoi);
    }
}
