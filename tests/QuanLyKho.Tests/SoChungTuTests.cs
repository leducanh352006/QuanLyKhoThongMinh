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
