using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HeThong;

public class NguoiDungQuanLyTests
{
    private static NguoiDungMoi Moi(string ten = "nhanvien2", string matKhau = "Moi@123456", int maVaiTro = MoiTruongThu.ThuKho)
        => new(ten, matKhau, "Nhân viên mới", "moi@example.com", "0901234567", maVaiTro);

    [Fact]
    public async Task Them_ThanhCong_DangNhapDuocVaMatKhauLuuDangBam()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.Them(Moi(), MoiTruongThu.QuanTriVien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var luu = (await e.Store.TimNguoiDungTheoTen("nhanvien2"))!;
        Assert.NotEqual("Moi@123456", luu.MatKhauHash);
        Assert.True((await e.TaiKhoan.DangNhap("nhanvien2", "Moi@123456", "1.1.1.1")).ThanhCong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm người dùng" && d.MaND == MoiTruongThu.QuanTriVien);
        Assert.DoesNotContain(e.NhatKy.Dong, d => (d.ChiTiet ?? "").Contains("Moi@123456"));   // nhật ký không chứa mật khẩu
    }

    [Fact]
    public async Task Them_TrungTenDangNhap_KhongPhanBietHoaThuong_BiTuChoi()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.Them(Moi(ten: "QuanTri"), MoiTruongThu.QuanTriVien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Theory]
    [InlineData("ab")]                 // quá ngắn
    [InlineData("co khoang trang")]
    [InlineData("tiếng.việt")]
    [InlineData("a;drop table")]
    public async Task Them_TenDangNhapSaiDinhDang_BiTuChoi(string ten)
    {
        var e = new MoiTruongThu();
        Assert.False((await e.NguoiDung.Them(Moi(ten: ten), MoiTruongThu.QuanTriVien)).ThanhCong);
    }

    [Theory]
    [InlineData("ngan1")]
    [InlineData("khongcoso")]
    [InlineData("12345678")]
    public async Task Them_MatKhauYeu_BiTuChoi(string matKhau)
    {
        var e = new MoiTruongThu();
        Assert.False((await e.NguoiDung.Them(Moi(matKhau: matKhau), MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.Null(await e.Store.TimNguoiDungTheoTen("nhanvien2"));
    }

    [Fact]
    public async Task Them_VaiTroKhongTonTai_EmailSaiSdtSai_HoTenRong_BiTuChoi()
    {
        var e = new MoiTruongThu();
        Assert.False((await e.NguoiDung.Them(Moi(maVaiTro: 99), MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.False((await e.NguoiDung.Them(Moi() with { Email = "khong-phai-email" }, MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.False((await e.NguoiDung.Them(Moi() with { SDT = "abc" }, MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.False((await e.NguoiDung.Them(Moi() with { HoTen = "   " }, MoiTruongThu.QuanTriVien)).ThanhCong);
    }

    [Fact]
    public async Task Them_EmailVaSdtDeTrong_Duoc()
    {
        var e = new MoiTruongThu();
        Assert.True((await e.NguoiDung.Them(Moi() with { Email = null, SDT = " " }, MoiTruongThu.QuanTriVien)).ThanhCong);
    }

    [Fact]
    public async Task DanhSach_KhongBaoGioMangTheoBanBamMatKhau()
    {
        Assert.DoesNotContain(typeof(NguoiDungDong).GetProperties(), p => p.Name.Contains("MatKhau") || p.Name.Contains("Hash"));
        var e = new MoiTruongThu();
        Assert.Equal(4, (await e.NguoiDung.LayDanhSach()).Count);
    }

    [Fact]
    public async Task Sua_DoiHoTenEmailVaiTro_VaGhiNhatKyCoVaiTroCuMoi()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.Sua(MoiTruongThu.NhanVienBanHang, new NguoiDungSuaDoi("Tên mới", "moi@example.com", null, MoiTruongThu.ThuKho), MoiTruongThu.QuanTriVien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var n = (await e.NguoiDung.Lay(MoiTruongThu.NhanVienBanHang))!;
        Assert.Equal("Tên mới", n.HoTen);
        Assert.Equal("Thủ kho", n.TenVaiTro);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Sửa người dùng" && d.ChiTiet!.Contains("Nhân viên bán hàng") && d.ChiTiet.Contains("Thủ kho"));
    }

    [Fact]
    public async Task Sua_NguoiKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.Sua(999, new NguoiDungSuaDoi("A", null, null, 1), MoiTruongThu.QuanTriVien);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }

    [Fact]
    public async Task Sua_SaiDuLieu_KhongLamDoiGiCaNguoiDung()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.Sua(MoiTruongThu.ThuKho, new NguoiDungSuaDoi("Tên mới", "sai-email", null, MoiTruongThu.ThuKho), MoiTruongThu.QuanTriVien);
        Assert.False(kq.ThanhCong);
        Assert.NotEqual("Tên mới", (await e.NguoiDung.Lay(MoiTruongThu.ThuKho))!.HoTen);
    }

    [Fact]
    public async Task Sua_TuDoiVaiTroCuaChinhMinh_BiTuChoi()
    {
        var e = new MoiTruongThu();
        await e.NguoiDung.Them(Moi(ten: "quantri2", maVaiTro: 1), MoiTruongThu.QuanTriVien);   // còn quản trị viên khác nên không dính luật "cuối cùng"
        var kq = await e.NguoiDung.Sua(MoiTruongThu.QuanTriVien, new NguoiDungSuaDoi("Quản trị", null, null, 3), MoiTruongThu.QuanTriVien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("tự đổi vai trò", kq.ThongBao);
    }

    [Fact]
    public async Task Sua_HaQuanTriVienCuoiCung_BiTuChoi_ConNguoiKhacThiDuoc()
    {
        var e = new MoiTruongThu();
        var haVai = new NguoiDungSuaDoi("Quản trị", null, null, MoiTruongThu.ThuKho);

        var cuoi = await e.NguoiDung.Sua(MoiTruongThu.QuanTriVien, haVai, MoiTruongThu.QuanLyKho);
        Assert.False(cuoi.ThanhCong);
        Assert.Contains("cuối cùng", cuoi.ThongBao);

        await e.NguoiDung.Them(Moi(ten: "quantri2", maVaiTro: 1), MoiTruongThu.QuanTriVien);
        Assert.True((await e.NguoiDung.Sua(MoiTruongThu.QuanTriVien, haVai, MoiTruongThu.QuanLyKho)).ThanhCong);
    }

    [Fact]
    public async Task Khoa_TuKhoaChinhMinh_BiTuChoi()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.DatTrangThai(MoiTruongThu.ThuKho, false, MoiTruongThu.ThuKho);
        Assert.False(kq.ThanhCong);
        Assert.True((await e.NguoiDung.Lay(MoiTruongThu.ThuKho))!.DangHoatDong);
    }

    [Fact]
    public async Task Khoa_QuanTriVienCuoiCung_BiTuChoi_ConNguoiKhacThiDuoc()
    {
        var e = new MoiTruongThu();
        var cuoi = await e.NguoiDung.DatTrangThai(MoiTruongThu.QuanTriVien, false, MoiTruongThu.QuanLyKho);
        Assert.False(cuoi.ThanhCong);
        Assert.Contains("cuối cùng", cuoi.ThongBao);

        await e.NguoiDung.Them(Moi(ten: "quantri2", maVaiTro: 1), MoiTruongThu.QuanTriVien);
        Assert.True((await e.NguoiDung.DatTrangThai(MoiTruongThu.QuanTriVien, false, MoiTruongThu.QuanLyKho)).ThanhCong);
    }

    [Fact]
    public async Task Khoa_VaMoKhoa_GhiNhatKy_VaDoiTrangThai()
    {
        var e = new MoiTruongThu();
        await e.NguoiDung.DatTrangThai(MoiTruongThu.NhanVienBanHang, false, MoiTruongThu.QuanTriVien);
        Assert.False((await e.NguoiDung.Lay(MoiTruongThu.NhanVienBanHang))!.DangHoatDong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Khóa người dùng");

        await e.NguoiDung.DatTrangThai(MoiTruongThu.NhanVienBanHang, true, MoiTruongThu.QuanTriVien);
        Assert.True((await e.NguoiDung.Lay(MoiTruongThu.NhanVienBanHang))!.DangHoatDong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Mở khóa người dùng");
    }

    [Fact]
    public async Task Khoa_LamLaiKhiDaKhoa_KhongGhiNhatKyThua()
    {
        var e = new MoiTruongThu();
        await e.NguoiDung.DatTrangThai(MoiTruongThu.NhanVienBanHang, false, MoiTruongThu.QuanTriVien);
        var truoc = e.NhatKy.Dong.Count;
        Assert.True((await e.NguoiDung.DatTrangThai(MoiTruongThu.NhanVienBanHang, false, MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.Equal(truoc, e.NhatKy.Dong.Count);
    }

    [Fact]
    public async Task DatLaiMatKhau_MatKhauCuHetDung_MoiDungDuoc_VaKhongGhiMatKhauVaoNhatKy()
    {
        var e = new MoiTruongThu();
        var kq = await e.NguoiDung.DatLaiMatKhau(MoiTruongThu.ThuKho, "Moi@987654", MoiTruongThu.QuanTriVien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.False((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, "1.1.1.1")).ThanhCong);
        Assert.True((await e.TaiKhoan.DangNhap("thukho", "Moi@987654", "1.1.1.1")).ThanhCong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Đặt lại mật khẩu");
        Assert.DoesNotContain(e.NhatKy.Dong, d => (d.ChiTiet ?? "").Contains("Moi@987654"));
    }

    [Fact]
    public async Task DatLaiMatKhau_MatKhauYeuHoacNguoiKhongTonTai_BiTuChoi()
    {
        var e = new MoiTruongThu();
        Assert.False((await e.NguoiDung.DatLaiMatKhau(MoiTruongThu.ThuKho, "yeu", MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.True((await e.TaiKhoan.DangNhap("thukho", MoiTruongThu.MatKhauMau, "1.1.1.1")).ThanhCong);   // mật khẩu cũ còn nguyên
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.NguoiDung.DatLaiMatKhau(999, "Moi@987654", MoiTruongThu.QuanTriVien)).MaLoi);
    }
}
