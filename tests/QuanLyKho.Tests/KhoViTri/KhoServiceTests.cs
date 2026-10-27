using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.KhoViTri;

public class KhoServiceTests
{
    private static KhoMoi Moi(string ten = "Kho mới", string loai = "Kho lưu trữ", int maQuanLy = 2, int thoiGianCho = 2)
        => new(ten, loai, "123 Đường ABC", maQuanLy, thoiGianCho);

    [Fact]
    public async Task HaiKhoMau_DaCoSanTuSeedData()
    {
        var e = new MoiTruongKhoThu();
        var ds = await e.Kho.LayDanhSach();
        Assert.Equal(2, ds.Count);
        Assert.Contains(ds, k => k.TenKho == "Kho tổng" && k.DangHoatDong);
    }

    [Fact]
    public async Task Them_ThanhCong_TuTaoKemKhuTiepNhan()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.Kho.Them(Moi(), MoiTruongKhoThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var ds = await e.Kho.LayDanhSach();
        var moi = Assert.Single(ds, k => k.TenKho == "Kho mới");

        var cay = (await e.ViTri.LayCay(moi.MaKho)).GiaTri!;
        var tiepNhan = Assert.Single(cay, v => v.LoaiViTri == "Tiếp nhận");
        Assert.True(tiepNhan.LaO);
        Assert.True(tiepNhan.DangHoatDong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm kho" && d.ChiTiet == "Kho mới");
    }

    [Fact]
    public async Task Them_TrungTen_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.Kho.Them(Moi(ten: "Kho tổng"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Loại không có")]
    public async Task Them_LoaiKhoSai_BiTuChoi(string loai)
    {
        var e = new MoiTruongKhoThu();
        Assert.False((await e.Kho.Them(Moi(loai: loai), MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
    }

    [Fact]
    public async Task Them_ThoiGianChoAm_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        Assert.False((await e.Kho.Them(Moi(thoiGianCho: -1), MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
    }

    [Fact]
    public async Task Them_NguoiQuanLyKhongTonTaiHoacBiKhoa_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        Assert.False((await e.Kho.Them(Moi(maQuanLy: 999), MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
    }

    [Fact]
    public async Task Sua_DoiTenVaNguoiQuanLy()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.Kho.Sua(MoiTruongKhoThu.MaKho1, new KhoSuaDoi("Kho tổng (đổi tên)", "Kho lưu trữ", null, 3, 5), MoiTruongKhoThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var k = await e.Kho.Lay(MoiTruongKhoThu.MaKho1);
        Assert.Equal("Kho tổng (đổi tên)", k!.TenKho);
        Assert.Equal(3, k.MaQuanLy);
    }

    [Fact]
    public async Task Sua_KhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongKhoThu();
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await e.Kho.Sua(999, new KhoSuaDoi("A", "Kho lưu trữ", null, 2, 0), MoiTruongKhoThu.MaNguoiThucHien)).MaLoi);
    }

    [Fact]
    public async Task NgungHoatDong_VaMoLai_GhiNhatKy()
    {
        var e = new MoiTruongKhoThu();
        await e.Kho.DatTrangThai(MoiTruongKhoThu.MaKho2, false, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False((await e.Kho.Lay(MoiTruongKhoThu.MaKho2))!.DangHoatDong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Ngừng hoạt động kho");

        await e.Kho.DatTrangThai(MoiTruongKhoThu.MaKho2, true, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True((await e.Kho.Lay(MoiTruongKhoThu.MaKho2))!.DangHoatDong);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Mở lại kho");
    }
}
