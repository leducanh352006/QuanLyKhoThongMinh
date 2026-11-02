using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Tests.KhoViTri;

namespace QuanLyKho.Tests.GoiY;

internal sealed class MoiTruongGia(string ten) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = ten;
    public string ApplicationName { get; set; } = "QuanLyKho.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>Handler giả ghi lại mọi lần được gọi. Có thể chậm (để thử hai người duyệt cùng lúc), trả lỗi hoặc ném lỗi.</summary>
internal sealed class HandlerGia(LoaiGoiY loai, ThamChieuGoiY? tra = null) : IGoiYHandler
{
    public LoaiGoiY Loai => loai;
    public List<GoiYDto> DaNhan { get; } = new();
    public TimeSpan Cham { get; init; }
    public bool TraLoi { get; init; }
    public bool NemLoi { get; init; }

    public async Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default)
    {
        lock (DaNhan) DaNhan.Add(goiY);
        if (Cham > TimeSpan.Zero) await Task.Delay(Cham, ct);
        if (NemLoi) throw new InvalidOperationException("Lỗi bất ngờ trong handler.");
        return TraLoi ? KetQua<ThamChieuGoiY>.Loi("Không tạo được phiếu.", MaLoiDichVu.DuLieuKhongHopLe) : KetQua<ThamChieuGoiY>.Ok(tra!);
    }
}

internal sealed class MoiTruongGoiYThu
{
    // Người dùng mẫu (SeedDataSet.NguoiDung): 1 Quản trị viên, 2 Quản lý kho, 3 Thủ kho, 4 Nhân viên bán hàng.
    public const int QuanTri = 1, QuanLyKho = 2, ThuKho = 3, NhanVienBH = 4;

    public BoNhoGoiYStore Store { get; } = new();
    public NhatKyGia NhatKy { get; } = new();
    public BoNhoHeThongStore NguoiDung { get; }
    public GoiYService DichVu { get; }

    public MoiTruongGoiYThu(params IGoiYHandler[] handlers)
    {
        var cauHinh = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedPassword"] = "Demo@12345" }).Build();
        NguoiDung = new BoNhoHeThongStore(cauHinh, new MatKhauService(), NullLogger<BoNhoHeThongStore>.Instance);
        DichVu = new GoiYService(Store, handlers, NguoiDung, NhatKy, TimeProvider.System);
    }

    public static readonly ChiTietGoiYDto[] MotDong = { new(4, 20, "Kho quầy còn 5, dưới mức tối thiểu 10", MaKhoDich: 2) };

    public async Task<int> Tao(LoaiGoiY loai, ThamChieuGoiY? tc = null, IReadOnlyList<ChiTietGoiYDto>? dong = null)
    {
        tc ??= loai switch { LoaiGoiY.G1 => new(MaPX: 1), LoaiGoiY.G2 => new(MaPN: 1), _ => new() };
        var kq = await DichVu.Tao(loai, tc, dong ?? MotDong);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        return kq.GiaTri;
    }
}

public class GoiYServiceTests
{
    private static readonly ChiTietGoiYDto[] MotDong = MoiTruongGoiYThu.MotDong;

    [Fact]
    public async Task Tao_KiemThamChieuTheoLoai_VaDongDeXuat()
    {
        var e = new MoiTruongGoiYThu();
        var s = e.DichVu;
        Assert.False((await s.Tao(LoaiGoiY.G1, new ThamChieuGoiY(), MotDong)).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPX: 1), MotDong)).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1), MotDong)).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(MaPC: 1), MotDong)).ThanhCong);
        Assert.False((await s.Tao((LoaiGoiY)9, new ThamChieuGoiY(), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: 1), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: 1), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(), MotDong)).ThanhCong);

        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), Array.Empty<ChiTietGoiYDto>())).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, "  ") })).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 0, "lý do") })).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(0, 5, "lý do") })).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, new string('a', 501)) })).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, "lý do", MaKhoNguon: 1, MaKhoDich: 1) })).ThanhCong);

        var choDuyet = await s.DanhSachChoDuyet();
        Assert.Equal(4, choDuyet.Count);
        Assert.All(choDuyet, g => Assert.Equal(TrangThaiGoiY.ChoDuyet, g.TrangThai));
        Assert.Single(await s.DanhSachChoDuyet(LoaiGoiY.G3));
    }

    [Fact]
    public async Task Duyet_GoiHandlerVoiDungDong_SinhThaoTac_DoiTrangThai_GhiNhatKy()
    {
        var handler = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 77));
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G3);

        var kq = await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var nhan = Assert.Single(handler.DaNhan);
        Assert.Equal(MotDong, nhan.ChiTiet);
        var g = (await e.DichVu.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.DaDuyet, g.TrangThai);
        Assert.Equal(MoiTruongGoiYThu.QuanLyKho, g.MaNguoiDuyet);
        Assert.NotNull(g.ThoiGianDuyetUtc);
        Assert.Equal(77, g.ThamChieu.MaPN);
        Assert.Empty(await e.DichVu.DanhSachChoDuyet());
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Duyệt gợi ý AI" && d.ChiTiet!.Contains($"G3 #{ma}"));

        // Đã xử lý thì không xử lý lại, handler không chạy lần hai.
        Assert.Equal(MaLoiDichVu.SaiTrangThai, (await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho)).MaLoi);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, (await e.DichVu.TuChoi(ma, MoiTruongGoiYThu.QuanLyKho)).MaLoi);
        Assert.Single(handler.DaNhan);
    }

    [Fact]
    public async Task G1_HandlerTraThamChieuRong_GiuPhieuXuatCu()
    {
        var handler = new HandlerGia(LoaiGoiY.G1, new ThamChieuGoiY());
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: 15));

        Assert.True((await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.Equal(new ThamChieuGoiY(MaPX: 15), (await e.DichVu.Lay(ma))!.ThamChieu);
    }

    [Fact]
    public async Task SuaVaDuyet_HandlerNhanDongDaSua_DongGocGiuNguyenDeSoSanh()
    {
        var handler = new HandlerGia(LoaiGoiY.G4, new ThamChieuGoiY(MaPC: 9));
        var e = new MoiTruongGoiYThu(handler);
        var goc = new[] { new ChiTietGoiYDto(4, 20, "Kho tổng dư 15, kho quầy thiếu 7", MaKhoNguon: 1, MaKhoDich: 2) };
        var ma = await e.Tao(LoaiGoiY.G4, dong: goc);
        var daSua = new[] { goc[0] with { SoLuong = 7 } };

        var kq = await e.DichVu.SuaVaDuyet(ma, daSua, MoiTruongGoiYThu.QuanLyKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(7, Assert.Single(Assert.Single(handler.DaNhan).ChiTiet).SoLuong);
        var g = (await e.DichVu.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.DaSuaVaDuyet, g.TrangThai);
        Assert.Equal(9, g.ThamChieu.MaPC);
        Assert.Equal(7, Assert.Single(g.ChiTiet).SoLuong);
        Assert.Equal(20, Assert.Single(await e.DichVu.LayChiTietGoc(ma)).SoLuong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Sửa và duyệt gợi ý AI");
    }

    [Fact]
    public async Task SuaVaDuyet_DongSuaKhongHopLe_BiTuChoi_KhongGoiHandler()
    {
        var handler = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY());
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G3);

        Assert.False((await e.DichVu.SuaVaDuyet(ma, Array.Empty<ChiTietGoiYDto>(), MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.False((await e.DichVu.SuaVaDuyet(ma, new[] { MotDong[0] with { SoLuong = -1 } }, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.Empty(handler.DaNhan);
        Assert.Equal(TrangThaiGoiY.ChoDuyet, (await e.DichVu.Lay(ma))!.TrangThai);
    }

    [Fact]
    public async Task TuChoi_KhongTaoThaoTacNao_GhiLyDoVaoNhatKy()
    {
        var handler = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1));
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G3);

        var kq = await e.DichVu.TuChoi(ma, MoiTruongGoiYThu.QuanLyKho, "  Hàng sắp ngừng kinh doanh  ");

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Empty(handler.DaNhan);
        var g = (await e.DichVu.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.TuChoi, g.TrangThai);
        Assert.Equal(MoiTruongGoiYThu.QuanLyKho, g.MaNguoiDuyet);
        Assert.Equal(new ThamChieuGoiY(), g.ThamChieu);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Từ chối gợi ý AI" && d.ChiTiet == $"G3 #{ma}: Hàng sắp ngừng kinh doanh");
        Assert.False((await e.DichVu.TuChoi(await e.Tao(LoaiGoiY.G3), MoiTruongGoiYThu.QuanLyKho, new string('x', 501))).ThanhCong);
    }

    [Theory]
    [InlineData(LoaiGoiY.G1, MoiTruongGoiYThu.QuanLyKho, true)]
    [InlineData(LoaiGoiY.G1, MoiTruongGoiYThu.ThuKho, false)]
    [InlineData(LoaiGoiY.G2, MoiTruongGoiYThu.ThuKho, true)]
    [InlineData(LoaiGoiY.G2, MoiTruongGoiYThu.QuanLyKho, false)]
    [InlineData(LoaiGoiY.G3, MoiTruongGoiYThu.QuanLyKho, true)]
    [InlineData(LoaiGoiY.G3, MoiTruongGoiYThu.ThuKho, false)]
    [InlineData(LoaiGoiY.G4, MoiTruongGoiYThu.QuanLyKho, true)]
    [InlineData(LoaiGoiY.G4, MoiTruongGoiYThu.NhanVienBH, false)]
    [InlineData(LoaiGoiY.G4, MoiTruongGoiYThu.QuanTri, true)]
    [InlineData(LoaiGoiY.G2, MoiTruongGoiYThu.QuanTri, true)]
    public async Task ThamQuyenDuyet_TheoLoaiGoiY(LoaiGoiY loai, int maND, bool duoc)
    {
        var tra = loai switch { LoaiGoiY.G1 => new ThamChieuGoiY(MaPX: 1), LoaiGoiY.G2 => new ThamChieuGoiY(MaPN: 1), _ => new ThamChieuGoiY() };
        var handler = new HandlerGia(loai, tra);
        var e = new MoiTruongGoiYThu(handler);
        var ma1 = await e.Tao(loai);
        var ma2 = await e.Tao(loai);

        var duyet = await e.DichVu.Duyet(ma1, maND);
        var tuChoi = await e.DichVu.TuChoi(ma2, maND);

        Assert.Equal(duoc, duyet.ThanhCong);
        Assert.Equal(duoc, tuChoi.ThanhCong);
        if (!duoc)
        {
            Assert.Equal(MaLoiPhanQuyen.KhongCoQuyen, duyet.MaLoi);
            Assert.Empty(handler.DaNhan);
            Assert.Equal(2, (await e.DichVu.DanhSachChoDuyet()).Count);
        }
    }

    [Fact]
    public async Task NguoiDuyetKhongTonTai_BiTuChoi()
    {
        var e = new MoiTruongGoiYThu(new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY()));
        var ma = await e.Tao(LoaiGoiY.G3);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.DichVu.Duyet(ma, 999)).MaLoi);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.DichVu.Duyet(12345, MoiTruongGoiYThu.QuanLyKho)).MaLoi);
    }

    [Fact]
    public async Task ChuaCoHandler_Duyet_BaoLamTay_GoiYVanChoDuyet_VanTuChoiDuoc()
    {
        var e = new MoiTruongGoiYThu();
        var ma = await e.Tao(LoaiGoiY.G3);

        var kq = await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.ChuaCoXuLy, kq.MaLoi);
        Assert.Contains("làm tay", kq.ThongBao);
        var g = (await e.DichVu.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.ChoDuyet, g.TrangThai);
        Assert.Null(g.MaNguoiDuyet);
        Assert.False(e.DichVu.CoXuLy(LoaiGoiY.G3));
        Assert.True((await e.DichVu.TuChoi(ma, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandlerLoiHoacNemLoi_HoanTacHet_GoiYVanChoDuyet(bool nemLoi)
    {
        var handler = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 5)) { TraLoi = !nemLoi, NemLoi = nemLoi };
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G3);

        var kq = await e.DichVu.SuaVaDuyet(ma, new[] { MotDong[0] with { SoLuong = 3 } }, MoiTruongGoiYThu.QuanLyKho);

        Assert.False(kq.ThanhCong);
        var g = (await e.DichVu.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.ChoDuyet, g.TrangThai);
        Assert.Null(g.MaNguoiDuyet);
        Assert.Null(g.ThoiGianDuyetUtc);
        Assert.Equal(new ThamChieuGoiY(), g.ThamChieu);
        Assert.Equal(20, Assert.Single(g.ChiTiet).SoLuong);   // dòng sửa không được lưu
        Assert.DoesNotContain(e.NhatKy.Dong, d => d.HanhDong.Contains("duyệt gợi ý", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandlerTraPhieuSaiLoai_BiTuChoi_GoiYVanChoDuyet()
    {
        var handler = new HandlerGia(LoaiGoiY.G4, new ThamChieuGoiY(MaPN: 3));   // G4 phải là phiếu chuyển
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G4);

        Assert.False((await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.Equal(TrangThaiGoiY.ChoDuyet, (await e.DichVu.Lay(ma))!.TrangThai);
    }

    [Fact]
    public async Task HaiNguoiDuyetCungLuc_HandlerChiChayMotLan()
    {
        var handler = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1)) { Cham = TimeSpan.FromMilliseconds(150) };
        var e = new MoiTruongGoiYThu(handler);
        var ma = await e.Tao(LoaiGoiY.G3);

        var kq = await Task.WhenAll(
            Task.Run(() => e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho)),
            Task.Run(() => e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanTri)),
            Task.Run(() => e.DichVu.TuChoi(ma, MoiTruongGoiYThu.QuanTri)));

        Assert.Equal(1, kq.Count(k => k.ThanhCong));
        Assert.All(kq.Where(k => !k.ThanhCong), k => Assert.Equal(MaLoiDichVu.SaiTrangThai, k.MaLoi));
        Assert.True(handler.DaNhan.Count <= 1);
    }

    [Fact]
    public async Task HandlerThatDuocUuTienHonHandlerMau_HandlerMauTatNgoaiDevelopment()
    {
        var noiGhi = new ThaoTacMauStore();
        var mauDev = new GoiYHandlerMau(LoaiGoiY.G3, noiGhi, new MoiTruongGia(Environments.Development), TimeProvider.System);
        var mauProd = new GoiYHandlerMau(LoaiGoiY.G4, noiGhi, new MoiTruongGia(Environments.Production), TimeProvider.System);
        var that = new HandlerGia(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 8));
        var e = new MoiTruongGoiYThu(that, mauDev, mauProd);   // handler mẫu đăng ký sau vẫn phải nhường handler thật

        var ma = await e.Tao(LoaiGoiY.G3);
        Assert.True((await e.DichVu.Duyet(ma, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.Single(that.DaNhan);
        Assert.Empty(noiGhi.TatCa());

        Assert.False(e.DichVu.CoXuLy(LoaiGoiY.G4));
        Assert.Equal(MaLoiDichVu.ChuaCoXuLy, (await e.DichVu.Duyet(await e.Tao(LoaiGoiY.G4), MoiTruongGoiYThu.QuanLyKho)).MaLoi);
    }

    [Fact]
    public async Task GoiYMau_TaoVaDuyet_SinhDungThaoTacTheoDongDaDuyet()
    {
        var noiGhi = new ThaoTacMauStore();
        var dev = new MoiTruongGia(Environments.Development);
        var handlers = Enum.GetValues<LoaiGoiY>().Select(l => (IGoiYHandler)new GoiYHandlerMau(l, noiGhi, dev, TimeProvider.System)).ToArray();
        var e = new MoiTruongGoiYThu(handlers);
        var mau = new GoiYMauService(e.DichVu);

        var tao = await mau.TaoMau(maPN: 4);
        Assert.True(tao.ThanhCong, tao.ThongBao);
        var ds = await e.DichVu.DanhSachChoDuyet();
        Assert.Equal(new[] { LoaiGoiY.G1, LoaiGoiY.G3, LoaiGoiY.G4, LoaiGoiY.G2 }, ds.Select(g => g.Loai).ToArray());

        var g3 = ds.Single(g => g.Loai == LoaiGoiY.G3);
        var g4 = ds.Single(g => g.Loai == LoaiGoiY.G4);
        var g2 = ds.Single(g => g.Loai == LoaiGoiY.G2);
        Assert.True((await e.DichVu.Duyet(g3.MaGoiY, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.True((await e.DichVu.SuaVaDuyet(g4.MaGoiY, new[] { g4.ChiTiet[0] with { SoLuong = 5 } }, MoiTruongGoiYThu.QuanLyKho)).ThanhCong);
        Assert.True((await e.DichVu.Duyet(g2.MaGoiY, MoiTruongGoiYThu.ThuKho)).ThanhCong);

        var thaoTac = noiGhi.TatCa();
        Assert.Equal(3, thaoTac.Count);
        Assert.Equal(42, thaoTac.Single(t => t.Loai == LoaiGoiY.G3).Dong.Single().SoLuong);
        Assert.Equal(5, thaoTac.Single(t => t.Loai == LoaiGoiY.G4).Dong.Single().SoLuong);   // dùng dòng đã sửa, không phải đề xuất gốc
        Assert.Contains("phiếu nhập #4", thaoTac.Single(t => t.Loai == LoaiGoiY.G2).MoTa);
        Assert.Equal(new ThamChieuGoiY(MaPN: 4), (await e.DichVu.Lay(g2.MaGoiY))!.ThamChieu);
    }

    [Fact]
    public async Task GoiYMau_AiLoi_KhongTaoGoiYNao_BaoLamTay()
    {
        var e = new MoiTruongGoiYThu();
        var kq = await new GoiYMauService(e.DichVu).TaoMauAiLoi();
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.AiKhongDung, kq.MaLoi);
        Assert.Contains("làm tay", kq.ThongBao);
        Assert.Empty(await e.DichVu.DanhSachChoDuyet());
    }
}

public class GoiYAnToanTests
{
    [Fact]
    public async Task ThanhCongHoacLoiNghiepVu_GiuNguyen()
    {
        Assert.Equal(5, (await GoiYAnToan.Chay(_ => Task.FromResult(KetQua<int>.Ok(5)))).GiaTri);
        var loi = await GoiYAnToan.Chay(_ => Task.FromResult(KetQua<int>.Loi("Chưa đủ 14 ngày lịch sử xuất.", MaLoiDichVu.DuLieuKhongHopLe)));
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, loi.MaLoi);
        Assert.Equal("Chưa đủ 14 ngày lịch sử xuất.", loi.ThongBao);
    }

    [Fact]
    public async Task AiNemLoi_TraKetQuaLamTay_KhongNem()
    {
        var kq = await GoiYAnToan.Chay<int>(_ => throw new InvalidOperationException("hỏng"));
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.AiKhongDung, kq.MaLoi);
        Assert.Contains("làm tay", kq.ThongBao);
    }

    [Fact]
    public async Task AiQuaThoiGian_TraKetQuaLamTay()
    {
        var kq = await GoiYAnToan.Chay<int>(async c => { await Task.Delay(5000, c); return KetQua<int>.Ok(1); }, TimeSpan.FromMilliseconds(50));
        Assert.Equal(MaLoiDichVu.AiKhongDung, kq.MaLoi);
        Assert.Contains("quá thời gian", kq.ThongBao);
    }

    [Fact]
    public async Task NguoiDungHuyYeuCau_VanNemHuy()
    {
        using var huy = new CancellationTokenSource();
        huy.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GoiYAnToan.Chay<int>(async c => { await Task.Delay(1000, c); return KetQua<int>.Ok(1); }, ct: huy.Token));
    }
}

public class QuyTacGoiYTests
{
    [Fact]
    public void LoaiDuocDuyet_TheoVaiTro()
    {
        Assert.Equal(new[] { LoaiGoiY.G1, LoaiGoiY.G3, LoaiGoiY.G4 }, QuyTacGoiY.LoaiDuocDuyet(VaiTroTen.QuanLyKho));
        Assert.Equal(new[] { LoaiGoiY.G2 }, QuyTacGoiY.LoaiDuocDuyet(VaiTroTen.ThuKho));
        Assert.Equal(Enum.GetValues<LoaiGoiY>(), QuyTacGoiY.LoaiDuocDuyet(VaiTroTen.QuanTriVien));
        Assert.Empty(QuyTacGoiY.LoaiDuocDuyet(VaiTroTen.NhanVienBanHang));
        Assert.Empty(QuyTacGoiY.LoaiDuocDuyet(null));
    }

    [Fact]
    public void ThamChieuSauDuyet_DungLoaiPhieu()
    {
        Assert.Null(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G3, new ThamChieuGoiY()));
        Assert.Null(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1)));
        Assert.Null(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G4, new ThamChieuGoiY(MaPC: 1)));
        Assert.NotNull(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G4, new ThamChieuGoiY(MaPN: 1)));
        Assert.NotNull(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G1, new ThamChieuGoiY()));
        Assert.NotNull(QuyTacGoiY.KiemTraThamChieuSauDuyet(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: 1, MaPC: 2)));
    }
}
