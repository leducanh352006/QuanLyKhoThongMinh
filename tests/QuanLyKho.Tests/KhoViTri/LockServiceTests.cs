using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.KhoViTri;

public class LockServiceTests
{
    private const int MaOLuuTru = 3;    // ô Lưu trữ có sẵn trong SeedData
    private const int MaToaGoc = 1;     // vị trí cấp cao, không phải ô

    private static (LockService Khoa, MoiTruongKhoThu Moi) Tao()
    {
        var moi = new MoiTruongKhoThu();
        return (new LockService(moi.Store, moi.NhatKy), moi);
    }

    [Fact]
    public async Task KhoaO_ThanhCong_DanhDauDangKhoa()
    {
        var (khoa, moi) = Tao();
        Assert.False(await khoa.DangKhoa(MaOLuuTru));

        var kq = await khoa.KhoaO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong);
        Assert.True(await khoa.DangKhoa(MaOLuuTru));
        Assert.Contains(moi.NhatKy.Dong, d => d.HanhDong == "Khóa ô kiểm kê");
    }

    [Fact]
    public async Task KhoaO_DaKhoaRoi_TraLoi_OBiKhoa()
    {
        var (khoa, _) = Tao();
        await khoa.KhoaO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);

        var kq = await khoa.KhoaO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.OBiKhoa, kq.MaLoi);
    }

    [Fact]
    public async Task KhoaO_KhongPhaiO_BiTuChoi()
    {
        var (khoa, _) = Tao();
        var kq = await khoa.KhoaO(MaToaGoc, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task MoO_SauKhiKhoa_TraVeMoVaGhiNhatKy()
    {
        var (khoa, moi) = Tao();
        await khoa.KhoaO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);

        var kq = await khoa.MoO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong);
        Assert.False(await khoa.DangKhoa(MaOLuuTru));
        Assert.Contains(moi.NhatKy.Dong, d => d.HanhDong == "Mở khóa ô kiểm kê");
    }

    [Fact]
    public async Task MoO_DangMoSan_AnToanKhiGoiLai()
    {
        var (khoa, _) = Tao();
        var kq = await khoa.MoO(MaOLuuTru, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong);
    }

    [Fact]
    public async Task KhoaO_KhongTonTai_KhongTimThay()
    {
        var (khoa, _) = Tao();
        var kq = await khoa.KhoaO(999_999, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }
}
