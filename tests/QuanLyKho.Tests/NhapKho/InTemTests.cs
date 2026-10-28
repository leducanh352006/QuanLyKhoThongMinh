using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-6 (In tem mã): mã vạch Code 128 và mã lô. Không cần SQL Server.</summary>
public class InTemTests
{
    [Fact]
    public void MaHoa_KyTuA_DungTheoChuan()
    {
        // Bắt đầu B (104) + 'A' (33) + kiểm tra (104 + 33 * 1) % 103 = 34 + kết thúc.
        Assert.Equal("211214" + "111323" + "131123" + "2331112", string.Concat(Code128.MaHoa("A")!));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("8935001234567")]
    [InlineData("LO-NK000001-01")]
    public void MaHoa_TongDoRongBangMuoiMotModuleMoiKyTuCongHaiMuoiHai(string ma)
    {
        // Bắt đầu + từng ký tự + kiểm tra = (n + 2) ký hiệu rộng 11, cộng ký hiệu kết thúc rộng 13.
        Assert.Equal(11 * (ma.Length + 2) + 13, Code128.MaHoa(ma)!.Sum());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Áo thun")]      // có dấu tiếng Việt, Code 128 bộ B không mã hóa được
    [InlineData("a\tb")]         // ký tự điều khiển
    public void MaKhongHopLe_KhongTaoDuocVach(string? ma)
    {
        Assert.False(Code128.HopLe(ma));
        Assert.Null(Code128.MaHoa(ma));
        Assert.Null(Code128.Svg(ma));
    }

    [Fact]
    public void Svg_CoVachVaVungTrongHaiDau()
    {
        var svg = Code128.Svg("A")!;
        Assert.StartsWith("<svg", svg);
        Assert.Contains("viewBox=\"0 0 " + (11 * 3 + 13 + 20) + " 1\"", svg);   // 3 ký hiệu + kết thúc + 2 lề 10 module
        Assert.Contains("M10 0h2v1h-2z", svg);                                // vạch đầu tiên bắt đầu sau lề 10 module, rộng 2
    }

    [Fact]
    public void MaLo_DuyNhatTheoSoPhieuVaThuTuDong()
    {
        var ds = new[] { "NK000001", "NK000002" }.SelectMany(sp => Enumerable.Range(1, 120).Select(i => PhieuNhap.MaLo(sp, i))).ToList();
        Assert.Equal(ds.Count, ds.Distinct().Count());
        Assert.Equal("LO-NK000001-01", PhieuNhap.MaLo("NK000001", 1));
        Assert.All(ds, m => Assert.True(Code128.HopLe(m) && m.Length <= 50));   // vừa cột varchar(50) của LoHang.MaVachLo
    }
}
