using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

public class SanPhamServiceTests
{
    [Fact]
    public async Task BaSanPhamMau_DaCoSanTuSeedData()
    {
        var e = new MoiTruongSanPhamThu();
        var ds = await e.SanPham.LayDanhSach();
        Assert.Equal(3, ds.Count);
        Assert.Contains(ds, s => s.TenSP == "Áo thun cổ tròn" && s.TenDM == "Thời trang và phụ kiện");
    }

    [Fact]
    public async Task Them_ThanhCong_GhiNhatKy()
    {
        var e = new MoiTruongSanPhamThu();
        var maDM = (await e.DanhMuc.LayDanhSach()).First().MaDM;
        var kq = await e.SanPham.Them(new SanPhamMoi("Sản phẩm mới", maDM, "cái", false), MoiTruongSanPhamThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Contains(await e.SanPham.LayDanhSach(), s => s.TenSP == "Sản phẩm mới");
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm sản phẩm" && d.ChiTiet == "Sản phẩm mới");
    }

    [Fact]
    public async Task Them_DanhMucKhongTonTai_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var kq = await e.SanPham.Them(new SanPhamMoi("Sản phẩm mới", 999, "cái", false), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
    }

    [Fact]
    public async Task Them_TenRong_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var maDM = (await e.DanhMuc.LayDanhSach()).First().MaDM;
        var kq = await e.SanPham.Them(new SanPhamMoi("  ", maDM, "cái", false), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task Sua_DoiTenVaDanhMuc()
    {
        var e = new MoiTruongSanPhamThu();
        var sp = (await e.SanPham.LayDanhSach()).First();
        var dmKhac = (await e.DanhMuc.LayDanhSach()).First(d => d.MaDM != sp.MaDM);
        var kq = await e.SanPham.Sua(sp.MaSP, new SanPhamSuaDoi("Tên đã đổi", dmKhac.MaDM, sp.DonViTinh, sp.CoHanDung), MoiTruongSanPhamThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var sau = await e.SanPham.Lay(sp.MaSP);
        Assert.Equal("Tên đã đổi", sau!.TenSP);
        Assert.Equal(dmKhac.MaDM, sau.MaDM);
    }

    [Fact]
    public async Task Sua_KhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongSanPhamThu();
        var maDM = (await e.DanhMuc.LayDanhSach()).First().MaDM;
        var kq = await e.SanPham.Sua(999, new SanPhamSuaDoi("A", maDM, "cái", false), MoiTruongSanPhamThu.MaNguoiThucHien);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }
}
