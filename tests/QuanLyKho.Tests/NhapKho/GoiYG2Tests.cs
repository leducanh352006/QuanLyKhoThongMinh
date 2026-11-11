using QuanLyKho.Web.Areas.NhapKho.Services;
using static QuanLyKho.Web.Areas.NhapKho.Services.QuyTacG2;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-3 (gợi ý G2 vị trí sắp xếp): luật chọn ô và ràng buộc sức chứa, không cần SQL Server.</summary>
public class GoiYG2Tests
{
    // Mỗi đơn vị hàng: 10 cm³, 1 kg.
    private const decimal TheTich1 = 10m, Khoi1 = 1m;
    private static readonly IReadOnlyDictionary<int, (decimal TheTich, decimal Khoi)> ChuaDung = new Dictionary<int, (decimal, decimal)>();

    private static OUngVien O(int ma, decimal theTichCon, decimal khoiCon, double gan = 0.5, double cungHang = 0) =>
        new(ma, "Ô " + ma, theTichCon, khoiCon, gan, cungHang);

    [Theory]
    [InlineData(100, 100, 10)]   // thể tích là chiều chật: 100 / 10
    [InlineData(100, 3, 3)]      // khối lượng là chiều chật: 3 / 1
    [InlineData(99, 100, 9)]     // lẻ thì làm tròn xuống, không vượt
    [InlineData(0, 100, 0)]
    [InlineData(-5, 100, 0)]     // ô đã quá tải không nhận thêm
    public void ToiDa_LayChieuChatHon(decimal theTichCon, decimal khoiCon, int mong) =>
        Assert.Equal(mong, ToiDa(theTichCon, khoiCon, TheTich1, Khoi1));

    [Fact]
    public void PhanBo_ChonOCoDiemCaoNhat()
    {
        // Cả hai ô đều nhận đủ; ô 2 gần khu tiếp nhận hơn nên điểm S cao hơn.
        var kq = PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100, gan: 0.2), O(2, 100, 100, gan: 0.9)], ChuaDung);
        var p = Assert.Single(kq!);
        Assert.Equal(2, p.O.MaViTri);
        Assert.Equal(5, p.SoLuong);
    }

    [Fact]
    public void PhanBo_UuTienODangChuaHangCungLoai()
    {
        var kq = PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100, cungHang: 0), O(2, 100, 100, cungHang: 1.0)], ChuaDung);
        Assert.Equal(2, Assert.Single(kq!).O.MaViTri);
    }

    [Fact]
    public void PhanBo_VuotSucChuaThiChiaNhieuO_KhongOnaoVuot()
    {
        // Cần 15; ô 1 nhận 10, ô 2 nhận 6: chia 10 + 5.
        var kq = PhanBo(15, TheTich1, Khoi1, [O(1, 100, 100, gan: 0.9), O(2, 60, 100, gan: 0.5)], ChuaDung)!;
        Assert.Equal(15, kq.Sum(x => x.SoLuong));
        Assert.Equal(2, kq.Count);
        Assert.All(kq, x => Assert.True(x.SoLuong * TheTich1 <= x.O.TheTichCon && x.SoLuong * Khoi1 <= x.O.KhoiCon));
    }

    [Fact]
    public void PhanBo_KhongDuChoThiTraNull()
    {
        // Hai ô cộng lại chỉ nhận 10 + 6 = 16 < 20.
        Assert.Null(PhanBo(20, TheTich1, Khoi1, [O(1, 100, 100), O(2, 60, 100)], ChuaDung));
        Assert.Null(PhanBo(1, TheTich1, Khoi1, [], ChuaDung));
    }

    [Fact]
    public void PhanBo_TinhHangCacDongTruocXepVaoCungO()
    {
        // Dòng trước của cùng gợi ý đã chiếm hết 100 cm³ của ô 1, nên dòng này phải sang ô 2 dù ô 1 điểm cao hơn.
        var daDung = new Dictionary<int, (decimal, decimal)> { [1] = (100m, 10m) };
        var kq = PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100, gan: 0.9), O(2, 100, 100, gan: 0.2)], daDung);
        Assert.Equal(2, Assert.Single(kq!).O.MaViTri);

        // Chỉ chiếm 80 cm³: ô 1 còn nhận 2, phần còn lại 3 sang ô 2 (tổng vẫn đủ 5, không ô nào vượt).
        var conChoNho = PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100, gan: 0.9), O(2, 100, 100, gan: 0.2)],
            new Dictionary<int, (decimal, decimal)> { [1] = (80m, 8m) })!;
        Assert.Equal(new[] { 2, 3 }, conChoNho.Select(x => x.SoLuong).ToArray());
    }

    [Fact]
    public void PhanBo_TheoKhoiLuongKhiKhoiLuongChatHon()
    {
        // Thể tích còn 1000 (đủ 100 đơn vị) nhưng khối lượng chỉ còn 4 kg nên chỉ nhận 4.
        var kq = PhanBo(4, TheTich1, Khoi1, [O(1, 1000, 4)], ChuaDung);
        Assert.Equal(4, Assert.Single(kq!).SoLuong);
        Assert.Null(PhanBo(5, TheTich1, Khoi1, [O(1, 1000, 4)], ChuaDung));
    }

    [Fact]
    public void PhanBo_HaiPhanTrungSoLuongThiLechDi()
    {
        // Ô 1 nhận 5, ô 2 nhận 5 sẽ trùng số lượng (StockService.ChuyenO coi là gọi lặp) nên đổi thành 4 + 6.
        var kq = PhanBo(10, TheTich1, Khoi1, [O(1, 50, 100, gan: 0.9), O(2, 80, 100, gan: 0.5)], ChuaDung)!;
        Assert.Equal(10, kq.Sum(x => x.SoLuong));
        Assert.Equal(kq.Count, kq.Select(x => x.SoLuong).Distinct().Count());
        Assert.All(kq, x => Assert.True(x.SoLuong <= ToiDa(x.O.TheTichCon, x.O.KhoiCon, TheTich1, Khoi1)));
    }

    [Fact]
    public void PhanBo_KhongTrungSoLuongVoiLenhDaLapCuaDong()
    {
        // Dòng đã có lệnh xếp 5; phần mới cũng 5 thì bỏ gợi ý (thủ kho xếp tay), không gợi ý thứ chắc chắn bị LapLenh từ chối.
        Assert.Null(PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100)], ChuaDung, [5]));
        Assert.Equal(5, Assert.Single(PhanBo(5, TheTich1, Khoi1, [O(1, 100, 100)], ChuaDung, [3])!).SoLuong);
    }

    [Fact]
    public void Diem_LaTongCoTrongSo()
    {
        Assert.Equal(1.0, TrongSoGan + TrongSoCungHang + TrongSoLapDay, 9);
        var p = new PhanXep(O(1, 100, 100, gan: 1.0, cungHang: 1.0), 10, 1.0);
        Assert.Equal(1.0, p.Diem, 9);
        Assert.Equal(0.0, new PhanXep(O(1, 100, 100, gan: 0, cungHang: 0), 1, 0).Diem, 9);
    }

    [Fact]
    public void LapDay_TrongKhoang0Den1()
    {
        Assert.Equal(0.5, LapDay(5, 100, 100, TheTich1, Khoi1), 9);   // 50 / 100 theo thể tích
        Assert.Equal(1.0, LapDay(10, 100, 100, TheTich1, Khoi1), 9);
        Assert.Equal(1.0, LapDay(5, 0, 0, TheTich1, Khoi1), 9);       // ô không còn chỗ thì coi như đầy
    }

    [Fact]
    public void MoTaLyDo_NeuRoKichThuoc3ChieuVaPhuongPhapHeuristic()
    {
        var o = new OUngVien(3, "Ô A1-01", 200_000m, 100m, 0.5, 1.0, "100×60×50 cm");
        var phan = new PhanXep(o, 10, 0.55);
        var lyDo = GoiYG2Service.TaoLyDoGoiY(phan, "Áo thun Size M", "30×25×3 cm", 150_000m, 75m);
        Assert.Contains("Điểm S =", lyDo);
        Assert.Contains("30×25×3 cm", lyDo);
        Assert.Contains("100×60×50 cm", lyDo);
        Assert.Contains("150000 cm³", lyDo);
        Assert.Contains("75 kg", lyDo);
    }
}
