using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Tests.HeThong;

public class TaiKhoanServiceTests
{
    private const string Ip = "10.0.0.1";

    [Fact]
    public async Task BonTaiKhoanMau_DangNhapDuoc_DungVaiTro()
    {
        var e = new MoiTruongThu();
        foreach (var n in SeedDataSet.NguoiDung)
        {
            var kq = await e.TaiKhoan.DangNhap(n.TenDangNhap, MoiTruongThu.MatKhauMau, Ip);
            Assert.True(kq.ThanhCong, $"{n.TenDangNhap}: {kq.ThongBao}");
            Assert.Equal(n.MaND, kq.GiaTri!.MaND);
            Assert.Equal(SeedDataSet.VaiTro.Single(v => v.MaVaiTro == n.MaVaiTro).TenVaiTro, kq.GiaTri.TenVaiTro);
        }
    }

    [Fact]
    public async Task KhongCoMatKhauMauTrongCauHinh_KhongCoTaiKhoanNao()
    {
        var e = new MoiTruongThu(matKhauMau: null);
        Assert.Empty(await e.Store.LayNguoiDung());
        Assert.False((await e.TaiKhoan.DangNhap("quantri", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);
    }

    [Fact]
    public async Task MatKhauMauYeu_KhongTaoTaiKhoan()
    {
        var e = new MoiTruongThu(matKhauMau: "123");
        Assert.Empty(await e.Store.LayNguoiDung());
    }

    [Fact]
    public async Task TenDangNhap_KhongPhanBietHoaThuong_VaCatKhoangTrang()
    {
        var e = new MoiTruongThu();
        Assert.True((await e.TaiKhoan.DangNhap("  QuanTri ", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);
    }

    [Fact]
    public async Task SaiMatKhau_VaTenKhongTonTai_CungMotThongBao_KhongLoTenDangNhap()
    {
        var e = new MoiTruongThu();
        var sai = await e.TaiKhoan.DangNhap("quantri", "Sai@12345", Ip);
        var khongCo = await e.TaiKhoan.DangNhap("khongco", "Sai@12345", Ip);
        Assert.False(sai.ThanhCong);
        Assert.False(khongCo.ThanhCong);
        Assert.Equal(sai.ThongBao, khongCo.ThongBao);
        Assert.Equal(MaLoiTaiKhoan.SaiThongTin, sai.MaLoi);
        Assert.Equal(MaLoiTaiKhoan.SaiThongTin, khongCo.MaLoi);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("", "x")]
    [InlineData("quantri", null)]
    [InlineData("quantri", "")]
    public async Task ThieuThongTin_BaoNhapDu(string? ten, string? matKhau)
    {
        var e = new MoiTruongThu();
        var kq = await e.TaiKhoan.DangNhap(ten, matKhau, Ip);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task TaiKhoanBiKhoa_DungMatKhauThiBaoKhoa_SaiMatKhauThiVanBaoChung()
    {
        var e = new MoiTruongThu();
        Assert.True((await e.NguoiDung.DatTrangThai(MoiTruongThu.NhanVienBanHang, false, MoiTruongThu.QuanTriVien)).ThanhCong);

        var dung = await e.TaiKhoan.DangNhap("nhanvienbh", MoiTruongThu.MatKhauMau, Ip);
        Assert.False(dung.ThanhCong);
        Assert.Equal(MaLoiTaiKhoan.TaiKhoanBiKhoa, dung.MaLoi);

        var sai = await e.TaiKhoan.DangNhap("nhanvienbh", "Sai@12345", Ip);
        Assert.Equal(MaLoiTaiKhoan.SaiThongTin, sai.MaLoi);   // người lạ không dò được tài khoản nào bị khóa
    }

    [Fact]
    public async Task Sai5Lan_BiChan_DungMatKhauCungBiChan_HetHanThiDangNhapLai()
    {
        var e = new MoiTruongThu();
        for (var i = 0; i < DangNhapGioiHan.SoLanToiDa; i++)
            Assert.Equal(MaLoiTaiKhoan.SaiThongTin, (await e.TaiKhoan.DangNhap("quanlykho", "Sai@12345", Ip)).MaLoi);

        var biChan = await e.TaiKhoan.DangNhap("quanlykho", MoiTruongThu.MatKhauMau, Ip);   // đúng mật khẩu nhưng đang bị chặn
        Assert.False(biChan.ThanhCong);
        Assert.Equal(MaLoiTaiKhoan.QuaNhieuLan, biChan.MaLoi);
        Assert.Contains("phút", biChan.ThongBao);

        e.DongHo.Tang(DangNhapGioiHan.ThoiGianChan + TimeSpan.FromSeconds(1));
        Assert.True((await e.TaiKhoan.DangNhap("quanlykho", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);
    }

    [Fact]
    public async Task ChanTheoCapTenVaIp_MayKhacVanDangNhapDuoc()
    {
        var e = new MoiTruongThu();
        for (var i = 0; i < DangNhapGioiHan.SoLanToiDa; i++)
            await e.TaiKhoan.DangNhap("thukho", "Sai@12345", "1.1.1.1");

        Assert.Equal(MaLoiTaiKhoan.QuaNhieuLan, (await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, "1.1.1.1")).MaLoi);
        Assert.True((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, "2.2.2.2")).ThanhCong);
    }

    [Fact]
    public async Task DangNhapThanhCong_XoaBoDemSai()
    {
        var e = new MoiTruongThu();
        for (var i = 0; i < DangNhapGioiHan.SoLanToiDa - 1; i++) await e.TaiKhoan.DangNhap("thukho", "Sai@12345", Ip);
        Assert.True((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);
        for (var i = 0; i < DangNhapGioiHan.SoLanToiDa - 1; i++) await e.TaiKhoan.DangNhap("thukho", "Sai@12345", Ip);
        Assert.True((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);   // 4 + 4 lần sai nhưng không dồn lại
    }

    [Fact]
    public async Task SaiRaiRac_QuaCuaSo_KhongDonLai()
    {
        var e = new MoiTruongThu();
        for (var i = 0; i < DangNhapGioiHan.SoLanToiDa - 1; i++) await e.TaiKhoan.DangNhap("thukho", "Sai@12345", Ip);
        e.DongHo.Tang(DangNhapGioiHan.CuaSo + TimeSpan.FromMinutes(1));
        await e.TaiKhoan.DangNhap("thukho", "Sai@12345", Ip);   // lần thứ 5 nhưng đã quá cửa sổ nên tính là lần 1
        Assert.True((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, Ip)).ThanhCong);
    }

    [Fact]
    public async Task DangNhapThanhCong_GhiNhatKy()
    {
        var e = new MoiTruongThu();
        await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, Ip);
        Assert.Contains(e.NhatKy.Dong, d => d.MaND == MoiTruongThu.ThuKho && d.HanhDong == "Đăng nhập");
        await e.TaiKhoan.GhiDangXuat(MoiTruongThu.ThuKho);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Đăng xuất");
    }

    [Fact]
    public async Task MatKhauKhongBaoGioLuuGoc()
    {
        var e = new MoiTruongThu();
        foreach (var n in await e.Store.LayNguoiDung())
        {
            Assert.NotEqual(MoiTruongThu.MatKhauMau, n.MatKhauHash);
            Assert.DoesNotContain(MoiTruongThu.MatKhauMau, n.MatKhauHash);
        }
    }

    // ---- phiên đăng nhập ----

    [Fact]
    public async Task KiemTraPhien_NguoiDangHoatDong_TraThongTinMoiNhat()
    {
        var e = new MoiTruongThu();
        var p = await e.TaiKhoan.KiemTraPhien(MoiTruongThu.QuanLyKho);
        Assert.NotNull(p);
        Assert.Equal("Quản lý kho", p!.TenVaiTro);
    }

    [Fact]
    public async Task KiemTraPhien_NguoiKhongTonTai_LaNull()
    {
        var e = new MoiTruongThu();
        Assert.Null(await e.TaiKhoan.KiemTraPhien(999));
    }

    [Fact]
    public async Task Khoa_CoHieuLucNgayVoiPhienDangMo_DuDaNhoTam()
    {
        var e = new MoiTruongThu();
        Assert.NotNull(await e.TaiKhoan.KiemTraPhien(MoiTruongThu.ThuKho));   // làm nóng nhớ tạm

        Assert.True((await e.NguoiDung.DatTrangThai(MoiTruongThu.ThuKho, false, MoiTruongThu.QuanTriVien)).ThanhCong);

        Assert.Null(await e.TaiKhoan.KiemTraPhien(MoiTruongThu.ThuKho));       // bị đăng xuất ngay, không chờ hết nhớ tạm
        Assert.True((await e.NguoiDung.DatTrangThai(MoiTruongThu.ThuKho, true, MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.NotNull(await e.TaiKhoan.KiemTraPhien(MoiTruongThu.ThuKho));    // mở khóa thì dùng lại được
    }

    [Fact]
    public async Task DoiVaiTro_CoHieuLucNgayVoiPhienDangMo()
    {
        var e = new MoiTruongThu();
        Assert.Equal("Nhân viên bán hàng", (await e.TaiKhoan.KiemTraPhien(MoiTruongThu.NhanVienBanHang))!.TenVaiTro);

        var kq = await e.NguoiDung.Sua(MoiTruongThu.NhanVienBanHang, new NguoiDungSuaDoi("Nhân viên", null, null, 3), MoiTruongThu.QuanTriVien);
        Assert.True(kq.ThanhCong, kq.ThongBao);

        Assert.Equal("Thủ kho", (await e.TaiKhoan.KiemTraPhien(MoiTruongThu.NhanVienBanHang))!.TenVaiTro);
    }
}
