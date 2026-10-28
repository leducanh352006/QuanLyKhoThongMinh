using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.KhoViTri;

/// <summary>Tiêu chí T4-5: ô 100x100x100 cm tối đa 50 kg báo đúng khi vượt thể tích hoặc khối lượng.</summary>
public class SucChuaTests
{
    private static ViTri O(string loai = LoaiViTriGiaTri.LuuTru, bool khoa = false) => new()
    {
        MaViTri = 1, TenViTri = "O-A1", CapViTri = CapViTriGiaTri.O, LoaiViTri = loai,
        Dai = 100, Rong = 100, Cao = 100, KhoiLuongToiDa = 50, DangKhoa = khoa,
    };

    [Fact]
    public void ODu_ChoVuaKhit_ChapNhan()
    {
        // 1.000.000 cm3, 50 kg: 10 thùng 50x50x40 (100.000 cm3) 5 kg vừa đủ cả thể tích lẫn khối lượng.
        var kq = QuyTacSucChua.Tinh(O(), 100_000m, 5m, 10, 0, 0);
        Assert.True(kq.Du);
        Assert.Equal(10, kq.SoLuongToiDaThem);
    }

    [Fact]
    public void VuotTheTich_BaoThieuTheTich()
    {
        // 600.000 cm3/đơn vị, nhẹ: 2 đơn vị = 1.200.000 > 1.000.000.
        var kq = QuyTacSucChua.Tinh(O(), 600_000m, 1m, 2, 0, 0);
        Assert.False(kq.Du);
        Assert.Equal(MaLoiDichVu.VuotSucChua, kq.MaLoi);
        Assert.Equal(1, kq.SoLuongToiDaThem);
        Assert.Contains("thể tích", kq.LyDo);
        Assert.DoesNotContain("khối lượng", kq.LyDo);
    }

    [Fact]
    public void VuotKhoiLuong_BaoThieuKhoiLuong()
    {
        // nhỏ 1.000 cm3, nặng 10 kg: 6 đơn vị = 60 kg > 50 kg.
        var kq = QuyTacSucChua.Tinh(O(), 1_000m, 10m, 6, 0, 0);
        Assert.False(kq.Du);
        Assert.Equal(5, kq.SoLuongToiDaThem);
        Assert.Contains("khối lượng", kq.LyDo);
        Assert.DoesNotContain("thể tích", kq.LyDo);
    }

    [Fact]
    public void VuotCaHai_NeuCaHai()
    {
        var kq = QuyTacSucChua.Tinh(O(), 600_000m, 40m, 2, 0, 0);
        Assert.False(kq.Du);
        Assert.Contains("thể tích", kq.LyDo);
        Assert.Contains("khối lượng", kq.LyDo);
    }

    [Fact]
    public void TinhPhanDaDung_ConLaiGiamTheo()
    {
        // Đã dùng 900.000 cm3 và 45 kg thì chỉ còn nhận 1 đơn vị 100.000 cm3 / 5 kg.
        Assert.True(QuyTacSucChua.Tinh(O(), 100_000m, 5m, 1, 900_000m, 45m).Du);
        Assert.False(QuyTacSucChua.Tinh(O(), 100_000m, 5m, 2, 900_000m, 45m).Du);
    }

    [Fact]
    public void DaDungVuotQuaTong_KhongNhanThemDon()
    {
        var kq = QuyTacSucChua.Tinh(O(), 1m, 0.1m, 1, 2_000_000m, 90m);
        Assert.False(kq.Du);
        Assert.Equal(0, kq.SoLuongToiDaThem);
    }

    [Fact]
    public void OTiepNhan_KhongGioiHan()
    {
        var o = O(LoaiViTriGiaTri.TiepNhan);
        o.Dai = o.Rong = o.Cao = o.KhoiLuongToiDa = null;
        var kq = QuyTacSucChua.Tinh(o, 1_000_000m, 1_000m, 1_000_000, 0, 0);
        Assert.True(kq.Du);
        Assert.Null(kq.SoLuongToiDaThem);
    }

    [Fact]
    public void OKhoa_BiTuChoi()
    {
        var kq = QuyTacSucChua.Tinh(O(khoa: true), 1m, 1m, 1, 0, 0);
        Assert.False(kq.Du);
        Assert.Equal(MaLoiDichVu.OBiKhoa, kq.MaLoi);
    }

    [Fact]
    public void KhongPhaiO_HoacSoLuongKhongDuong_BiTuChoi()
    {
        var ke = O();
        ke.CapViTri = CapViTriGiaTri.Ke;
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, QuyTacSucChua.Tinh(ke, 1m, 1m, 1, 0, 0).MaLoi);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, QuyTacSucChua.Tinh(O(), 1m, 1m, 0, 0, 0).MaLoi);
    }

    [Fact]
    public void ONgungHoatDong_BiTuChoi()
    {
        var o = O();
        o.TrangThai = TrangThaiHoatDong.NgungHoatDong;
        Assert.False(QuyTacSucChua.Tinh(o, 1m, 1m, 1, 0, 0).Du);
    }
}
