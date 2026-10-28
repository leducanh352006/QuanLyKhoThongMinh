using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

public class BienTheServiceTests
{
    private static BienTheMoi Moi(int maSP, string maVach = "8930000000999", string thuocTinh = "Size XL, xanh",
        decimal dai = 30, decimal rong = 25, decimal cao = 3, decimal khoiLuong = 0.2m)
        => new(maSP, maVach, thuocTinh, dai, rong, cao, khoiLuong);

    [Fact]
    public async Task BonBienTheMau_DaCoSanTuSeedData()
    {
        var e = new MoiTruongSanPhamThu();
        var aoThun = (await e.SanPham.LayDanhSach()).First(s => s.TenSP == "Áo thun cổ tròn");
        var ds = await e.BienThe.LayTheoSanPham(aoThun.MaSP);
        Assert.Equal(2, ds.Count);
    }

    [Fact]
    public async Task Them_ThanhCong_TinhDungTheTich_GhiNhatKy()
    {
        var e = new MoiTruongSanPhamThu();
        var sp = (await e.SanPham.LayDanhSach()).First();
        var kq = await e.BienThe.Them(Moi(sp.MaSP), MoiTruongSanPhamThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var bt = Assert.Single(await e.BienThe.LayTheoSanPham(sp.MaSP), b => b.MaVach == "8930000000999");
        Assert.Equal(30m * 25m * 3m, bt.TheTichCm3);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm biến thể");
    }

    [Fact]
    public async Task Them_TrungMaVach_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var sp = (await e.SanPham.LayDanhSach()).First();
        var maVachCoSan = (await e.BienThe.LayTheoSanPham(sp.MaSP)).First().MaVach;

        var kq = await e.BienThe.Them(Moi(sp.MaSP, maVach: maVachCoSan), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Theory]
    [InlineData(0, 25, 3, 0.2)]
    [InlineData(30, 0, 3, 0.2)]
    [InlineData(30, 25, 0, 0.2)]
    [InlineData(30, 25, 3, 0)]
    public async Task Them_ThieuKichThuocHoacKhoiLuong_BiTuChoi(decimal dai, decimal rong, decimal cao, decimal khoiLuong)
    {
        var e = new MoiTruongSanPhamThu();
        var sp = (await e.SanPham.LayDanhSach()).First();
        var kq = await e.BienThe.Them(Moi(sp.MaSP, dai: dai, rong: rong, cao: cao, khoiLuong: khoiLuong), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task Them_SanPhamKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongSanPhamThu();
        var kq = await e.BienThe.Them(Moi(999), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }

    [Fact]
    public async Task Sua_DoiThuocTinh()
    {
        var e = new MoiTruongSanPhamThu();
        var sp = (await e.SanPham.LayDanhSach()).First();
        var bt = (await e.BienThe.LayTheoSanPham(sp.MaSP)).First();

        var kq = await e.BienThe.Sua(bt.MaBT, new BienTheSuaDoi(bt.MaVach, "Thuộc tính mới", bt.Dai, bt.Rong, bt.Cao, bt.KhoiLuong), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal("Thuộc tính mới", (await e.BienThe.Lay(bt.MaBT))!.ThuocTinh);
    }

    [Fact]
    public async Task Sua_DoiMaVachTrungVoiBienTheKhac_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var aoThun = (await e.SanPham.LayDanhSach()).First(s => s.TenSP == "Áo thun cổ tròn");
        var ds = await e.BienThe.LayTheoSanPham(aoThun.MaSP);
        var a = ds[0];
        var b = ds[1];

        var kq = await e.BienThe.Sua(a.MaBT, new BienTheSuaDoi(b.MaVach, a.ThuocTinh, a.Dai, a.Rong, a.Cao, a.KhoiLuong), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
    }
}
