using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;

namespace QuanLyKho.Tests;

public class CapacityServiceStubTests
{
    private static (CapacityServiceStub Cap, LockServiceStub Khoa, StockServiceStub Ton) Tao()
    {
        var ton = new StockServiceStub();
        var khoa = new LockServiceStub();
        return (new CapacityServiceStub(ton, khoa), khoa, ton);
    }

    [Fact]
    public async Task O_ConNhieuCho_GioiHanTheoTheTich()
    {
        var (cap, _, _) = Tao();
        // Ô 3 (100x60x50 cm, 120 kg) đang có 50 áo M + 24 kem. Áo 30x25x3 cm: còn 81 theo thể tích, 540 theo khối lượng.
        var kq = await cap.KiemTra(3, 1, 81);
        Assert.True(kq.Du);
        Assert.Equal(81, kq.SoLuongToiDaThem);

        var vuot = await cap.KiemTra(3, 1, 82);
        Assert.False(vuot.Du);
        Assert.Equal(MaLoiDichVu.VuotSucChua, vuot.MaLoi);
        Assert.Contains("thể tích", vuot.LyDo);
    }

    [Fact]
    public async Task O_NhoNhe_GioiHanTheoKhoiLuong()
    {
        var (cap, _, _) = Tao();
        // Ô 6 (60x40x40 cm, 20 kg) có 60 tai nghe (15 kg): còn 20 theo khối lượng, 28 theo thể tích.
        Assert.True((await cap.KiemTra(6, 4, 20)).Du);
        var vuot = await cap.KiemTra(6, 4, 21);
        Assert.False(vuot.Du);
        Assert.Equal(20, vuot.SoLuongToiDaThem);
        Assert.Contains("khối lượng", vuot.LyDo);
    }

    [Fact]
    public async Task SucChuaTinhTheoTonHienTai_NhanThemHangThiConItCho()
    {
        var (cap, _, ton) = Tao();
        await ton.GhiTang(new DongTon(5, 6, 10), LoaiBienDong.SapXep, new ThamChieuChungTu(LoaiChungTu.NK, 1), 3);   // +10 tai nghe vào ô 6
        Assert.Equal(10, (await cap.KiemTra(6, 4, 1)).SoLuongToiDaThem);
    }

    [Fact]
    public async Task OTiepNhan_KhongGioiHan()
    {
        var (cap, _, _) = Tao();
        var kq = await cap.KiemTra(7, 1, 1_000_000);
        Assert.True(kq.Du);
        Assert.Null(kq.SoLuongToiDaThem);
    }

    [Fact]
    public async Task OBiKhoa_BiTuChoi_KhiMoLaiThiDuocLai()
    {
        var (cap, khoa, _) = Tao();
        await khoa.KhoaO(3, 2);
        var kq = await cap.KiemTra(3, 1, 1);
        Assert.False(kq.Du);
        Assert.Equal(MaLoiDichVu.OBiKhoa, kq.MaLoi);

        await khoa.MoO(3, 2);
        Assert.True((await cap.KiemTra(3, 1, 1)).Du);
    }

    [Fact]
    public async Task DauVaoSai_ViTriKhongCo_BienTheKhongCo_KhongPhaiO_SoLuongKhongDuong()
    {
        var (cap, _, _) = Tao();
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await cap.KiemTra(999, 1, 1)).MaLoi);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await cap.KiemTra(3, 999, 1)).MaLoi);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, (await cap.KiemTra(2, 1, 1)).MaLoi);   // ô 2 là Kệ
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, (await cap.KiemTra(3, 1, 0)).MaLoi);
    }
}

public class LockAuditStubTests
{
    [Fact]
    public async Task KhoaO_LanHai_BiTuChoi_MoOAnToanKhiGoiLai()
    {
        var k = new LockServiceStub();
        Assert.True((await k.KhoaO(3, 2)).ThanhCong);
        Assert.True(await k.DangKhoa(3));

        var lanHai = await k.KhoaO(3, 3);
        Assert.False(lanHai.ThanhCong);
        Assert.Equal(MaLoiDichVu.OBiKhoa, lanHai.MaLoi);

        Assert.True((await k.MoO(3, 2)).ThanhCong);
        Assert.True((await k.MoO(3, 2)).ThanhCong);   // gọi lại vẫn thành công
        Assert.False(await k.DangKhoa(3));
    }

    [Fact]
    public async Task NhatKy_GhiVaLayTheoChungTu()
    {
        var a = new AuditServiceStub();
        await a.Ghi(2, "duyệt", LoaiChungTu.XK, 5, "đủ hàng");
        await a.Ghi(3, "xác nhận", LoaiChungTu.XK, 5);
        await a.Ghi(3, "tạo", LoaiChungTu.NK, 5);
        await a.Ghi(1, "đăng nhập");                                  // không gắn chứng từ

        var xk5 = await a.LayTheoChungTu(LoaiChungTu.XK, 5);
        Assert.Equal(new[] { "duyệt", "xác nhận" }, xk5.Select(n => n.HanhDong).ToArray());
        Assert.Equal("đủ hàng", xk5[0].ChiTiet);
    }

    [Fact]
    public async Task NhatKy_LoaiCTVaMaCT_PhaiCungCoHoacCungKhong()
    {
        var a = new AuditServiceStub();
        await Assert.ThrowsAsync<ArgumentException>(() => a.Ghi(1, "tạo", LoaiChungTu.XK, null));
        await Assert.ThrowsAsync<ArgumentException>(() => a.Ghi(1, "tạo", null, 5));
        await Assert.ThrowsAsync<ArgumentException>(() => a.Ghi(1, "  "));
    }
}

public class GoiYServiceStubTests
{
    private sealed class HandlerThanhCong(LoaiGoiY loai, ThamChieuGoiY ketQua) : IGoiYHandler
    {
        public LoaiGoiY Loai => loai;
        public List<GoiYDto> DaNhan { get; } = new();
        public Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default)
        {
            DaNhan.Add(goiY);
            return Task.FromResult(KetQua<ThamChieuGoiY>.Ok(ketQua));
        }
    }

    private sealed class HandlerLoi(LoaiGoiY loai) : IGoiYHandler
    {
        public LoaiGoiY Loai => loai;
        public Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default) =>
            Task.FromResult(KetQua<ThamChieuGoiY>.Loi("Không tạo được phiếu.", MaLoiDichVu.DuLieuKhongHopLe));
    }

    private static readonly IReadOnlyList<ChiTietGoiYDto> MotDong = new[] { new ChiTietGoiYDto(4, 20, "Kho quầy còn 5, dưới mức tối thiểu 10", MaKhoDich: 2) };

    private static GoiYServiceStub Tao(params IGoiYHandler[] handlers) => new(new GoiYStubStore(), handlers);

    [Fact]
    public async Task Tao_G1CanMaPX_G2CanMaPN_G3G4KhongCoPhieuLucTao()
    {
        var s = Tao();
        Assert.False((await s.Tao(LoaiGoiY.G1, new ThamChieuGoiY(), MotDong)).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPX: 1), MotDong)).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: 1), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: 1), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).ThanhCong);
        Assert.True((await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(), MotDong)).ThanhCong);
    }

    [Fact]
    public async Task Tao_DongKhongCoLyDo_HoacKhongCoDong_BiTuChoi()
    {
        var s = Tao();
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), Array.Empty<ChiTietGoiYDto>())).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, "  ") })).ThanhCong);
        Assert.False((await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, "lý do", MaKhoNguon: 1, MaKhoDich: 1) })).ThanhCong);
    }

    [Fact]
    public async Task ChuaCoHandler_Duyet_BiTuChoi_GoiYVanChoDuyet_LamTayDuoc()
    {
        var s = Tao();
        var ma = (await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).GiaTri;
        var kq = await s.Duyet(ma, 2);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.ChuaCoXuLy, kq.MaLoi);
        Assert.Equal(TrangThaiGoiY.ChoDuyet, (await s.Lay(ma))!.TrangThai);
        Assert.Single(await s.DanhSachChoDuyet());
    }

    [Fact]
    public async Task Duyet_GoiHandler_RoiDoiTrangThai_VaLuuPhieuDuocTao()
    {
        var handler = new HandlerThanhCong(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 77));
        var s = Tao(handler);
        var ma = (await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).GiaTri;

        Assert.True((await s.Duyet(ma, 2)).ThanhCong);

        var g = (await s.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.DaDuyet, g.TrangThai);
        Assert.Equal(2, g.MaNguoiDuyet);
        Assert.NotNull(g.ThoiGianDuyetUtc);
        Assert.Equal(77, g.ThamChieu.MaPN);
        Assert.Single(handler.DaNhan);
        Assert.Empty(await s.DanhSachChoDuyet());
    }

    [Fact]
    public async Task SuaVaDuyet_HandlerNhanDongDaSua_TrangThaiDaSuaVaDuyet()
    {
        var handler = new HandlerThanhCong(LoaiGoiY.G4, new ThamChieuGoiY(MaPC: 9));
        var s = Tao(handler);
        var ma = (await s.Tao(LoaiGoiY.G4, new ThamChieuGoiY(), MotDong)).GiaTri;
        var daSua = new[] { new ChiTietGoiYDto(4, 12, "Quản lý giảm số lượng", MaKhoNguon: 1, MaKhoDich: 2) };

        Assert.True((await s.SuaVaDuyet(ma, daSua, 2)).ThanhCong);

        Assert.Equal(12, handler.DaNhan.Single().ChiTiet.Single().SoLuong);
        var g = (await s.Lay(ma))!;
        Assert.Equal(TrangThaiGoiY.DaSuaVaDuyet, g.TrangThai);
        Assert.Equal(12, g.ChiTiet.Single().SoLuong);
        Assert.Equal(9, g.ThamChieu.MaPC);
    }

    [Fact]
    public async Task HandlerLoi_GoiYVanChoDuyet()
    {
        var s = Tao(new HandlerLoi(LoaiGoiY.G3));
        var ma = (await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).GiaTri;
        var kq = await s.Duyet(ma, 2);
        Assert.False(kq.ThanhCong);
        Assert.Equal(TrangThaiGoiY.ChoDuyet, (await s.Lay(ma))!.TrangThai);
    }

    [Fact]
    public async Task TuChoi_DoiTrangThai_VaKhongXuLyLaiDuoc()
    {
        var s = Tao(new HandlerThanhCong(LoaiGoiY.G3, new ThamChieuGoiY(MaPN: 1)));
        var ma = (await s.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), MotDong)).GiaTri;

        Assert.True((await s.TuChoi(ma, 2, "không cần")).ThanhCong);
        Assert.Equal(TrangThaiGoiY.TuChoi, (await s.Lay(ma))!.TrangThai);

        var duyetLai = await s.Duyet(ma, 2);
        Assert.False(duyetLai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, duyetLai.MaLoi);
        Assert.False((await s.TuChoi(ma, 2)).ThanhCong);
    }

    [Fact]
    public async Task KhongTimThay_MaGoiYSai()
    {
        var s = Tao();
        Assert.Null(await s.Lay(123));
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await s.Duyet(123, 2)).MaLoi);
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await s.TuChoi(123, 2)).MaLoi);
    }
}

public class DichVuKhacStubTests
{
    [Fact]
    public async Task TaoLo_HangCoHanDung_BatBuocHanDung_MaLoDuyNhat_TuSinhMa()
    {
        var h = new HangHoaServiceStub();

        var thieuHan = await h.TaoLo(3, null);                       // BT 3 là kem chống nắng (có hạn dùng)
        Assert.False(thieuHan.ThanhCong);

        var ok = await h.TaoLo(3, new DateOnly(2028, 1, 31));
        Assert.True(ok.ThanhCong);
        Assert.False(string.IsNullOrEmpty(ok.GiaTri!.MaVachLo));
        Assert.True(ok.GiaTri.MaLo > 5);

        var trung = await h.TaoLo(1, null, "LO-2610-001");           // trùng lô mẫu
        Assert.False(trung.ThanhCong);

        var khongHan = await h.TaoLo(1, null, "LO-MOI-01");           // áo thun không theo hạn dùng
        Assert.True(khongHan.ThanhCong);
        Assert.Equal(1, khongHan.GiaTri!.MaBT);

        Assert.Equal(MaLoiDichVu.KhongTimThay, (await h.TaoLo(999, null)).MaLoi);
    }

    [Fact]
    public async Task PhieuNhapThua_VaXuatThieu_CanLoVaO_ConPhieuNhapNhapThiKhong()
    {
        var nhap = new NhapKhoServiceStub();
        var xuat = new XuatKhoServiceStub();
        var dongDay = new[] { new DongPhieu(1, 2, MaLo: 1, MaViTri: 3) };
        var dongThieu = new[] { new DongPhieu(1, 2) };

        var thua = await nhap.TaoPhieuNhapThua(1, 5, dongDay, 3);
        Assert.True(thua.ThanhCong);
        Assert.StartsWith("NK", thua.GiaTri!.SoPhieu);
        Assert.False((await nhap.TaoPhieuNhapThua(1, 5, dongThieu, 3)).ThanhCong);

        var nhapNhap = await nhap.TaoPhieuNhapNhap(2, dongThieu, 2, maGoiY: 4);
        Assert.True(nhapNhap.ThanhCong);

        var thieu = await xuat.TaoPhieuXuatThieu(1, 5, dongDay, 3);
        Assert.True(thieu.ThanhCong);
        Assert.StartsWith("XK", thieu.GiaTri!.SoPhieu);
        Assert.False((await xuat.TaoPhieuXuatThieu(1, 5, dongThieu, 3)).ThanhCong);
    }

    [Fact]
    public async Task PhieuStub_TuChoiKhoKhongCo_DongRong_SoLuongKhongDuong()
    {
        var nhap = new NhapKhoServiceStub();
        Assert.False((await nhap.TaoPhieuNhapNhap(99, new[] { new DongPhieu(1, 1) }, 2)).ThanhCong);
        Assert.False((await nhap.TaoPhieuNhapNhap(1, Array.Empty<DongPhieu>(), 2)).ThanhCong);
        Assert.False((await nhap.TaoPhieuNhapNhap(1, new[] { new DongPhieu(1, 0) }, 2)).ThanhCong);
    }

    [Fact]
    public void BienDongVaTrangThai_ChuyenDungChuoiCsdl()
    {
        Assert.Equal("Nhận hàng", LoaiBienDong.NhanHang.ToCsdl());
        Assert.Equal("Đổi ô", LoaiBienDong.DoiO.ToCsdl());
        Assert.Equal("Đã sửa và duyệt", TrangThaiGoiY.DaSuaVaDuyet.ToCsdl());
        Assert.Equal("Chờ duyệt", TrangThaiGoiY.ChoDuyet.ToCsdl());
        Assert.Equal("XK", LoaiChungTu.XK.ToCsdl());
        Assert.Equal("G3", LoaiGoiY.G3.ToCsdl());
    }

    [Fact]
    public void TatCaTamDichVuDangKyDuocVaResolveDuoc()
    {
        var services = new ServiceCollection().AddContractStubs();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStockService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICapacityService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILockService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuditService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IGoiYService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHangHoaService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<INhapKhoService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IXuatKhoService>());
    }

    [Fact]
    public async Task DangKySauThang_BanThatThayBanGia_VaGoiYStubVanChayVoiHandlerScoped()
    {
        var services = new ServiceCollection().AddContractStubs();
        services.AddSingleton<ILockService, LockThat>();                          // nhóm sở hữu đăng ký bản thật sau
        services.AddScoped<IGoiYHandler, HandlerG3>();                            // handler thường là scoped
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        Assert.IsType<LockThat>(scope.ServiceProvider.GetRequiredService<ILockService>());

        var goiY = scope.ServiceProvider.GetRequiredService<IGoiYService>();
        var ma = (await goiY.Tao(LoaiGoiY.G3, new ThamChieuGoiY(), new[] { new ChiTietGoiYDto(4, 5, "sắp hết") })).GiaTri;
        Assert.True((await goiY.Duyet(ma, 2)).ThanhCong);
    }

    private sealed class LockThat : ILockService
    {
        public Task<KetQua> KhoaO(int maViTri, int maND, CancellationToken ct = default) => Task.FromResult(KetQua.Ok());
        public Task<KetQua> MoO(int maViTri, int maND, CancellationToken ct = default) => Task.FromResult(KetQua.Ok());
        public Task<bool> DangKhoa(int maViTri, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class HandlerG3 : IGoiYHandler
    {
        public LoaiGoiY Loai => LoaiGoiY.G3;
        public Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default) =>
            Task.FromResult(KetQua<ThamChieuGoiY>.Ok(new ThamChieuGoiY(MaPN: 1)));
    }
}
