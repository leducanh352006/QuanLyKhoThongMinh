using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests;

public class SoChungTuTests
{
    [Theory]
    [InlineData("NK", 12, "NK000012")]
    [InlineData("XK", 7, "XK000007")]
    [InlineData("NK", 0, "Tồn ban đầu")]
    [InlineData("NK", null, "Tồn ban đầu")]
    public void SoChungTu_GiongSoTrenPhieu(string loai, int? ma, string mongDoi)
    {
        Assert.Equal(mongDoi, TenHienThiPhanQuyen.SoChungTu(loai, ma));
    }

    [Theory]
    [InlineData("NK000012", "NK", 12)]
    [InlineData(" nk12 ", "NK", 12)]
    [InlineData("XK 000007", "XK", 7)]
    [InlineData("15", null, 15)]
    public void TachSoChungTu_DocDuocCachGoThuongGap(string go, string? loai, int ma)
    {
        Assert.True(TenHienThiPhanQuyen.TachSoChungTu(go, out var l, out var m));
        Assert.Equal(loai, l);
        Assert.Equal(ma, m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("phiếu nhập")]
    [InlineData("NK-12")]
    public void TachSoChungTu_KhongDocDuoc_TraFalse(string? go)
    {
        Assert.False(TenHienThiPhanQuyen.TachSoChungTu(go, out var l, out var m));
        Assert.Null(l);
        Assert.Null(m);
    }
}

/// <summary>Chuyển hướng đăng nhập không mang tên máy (tránh bị đưa tới https://localhost sau proxy, đường hầm).</summary>
public class ChuyenHuongTuongDoiTests
{
    [Theory]
    [InlineData("https://localhost:5141/HeThong/TaiKhoan/DangNhap?ReturnUrl=%2FDieuChuyen", "/HeThong/TaiKhoan/DangNhap?ReturnUrl=%2FDieuChuyen")]
    [InlineData("http://x.app.github.dev/HeThong/TaiKhoan/TuChoi", "/HeThong/TaiKhoan/TuChoi")]
    [InlineData("/HeThong/TaiKhoan/DangNhap", "/HeThong/TaiKhoan/DangNhap")]
    public void DuongDanTuongDoi_BoTenMay(string vao, string ra) =>
        Assert.Equal(ra, QuanLyKho.Web.Areas.HeThong.Services.XacThucCookie.DuongDanTuongDoi(vao));
}
