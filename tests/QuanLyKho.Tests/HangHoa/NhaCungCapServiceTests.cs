using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

public class NhaCungCapServiceTests
{
    [Fact]
    public async Task BaNhaCungCapMau_DaCoSanTuSeedData()
    {
        var e = new MoiTruongHangHoaThu();
        var ds = await e.NhaCungCap.LayDanhSach();
        Assert.Equal(3, ds.Count);
        Assert.All(ds, n => Assert.True(n.DangHoatDong));
    }

    [Fact]
    public async Task Them_ThanhCong_GhiNhatKy()
    {
        var e = new MoiTruongHangHoaThu();
        var kq = await e.NhaCungCap.Them(new NhaCungCapMoi("Nhà cung cấp mới", "0987654321", "123 Đường ABC"), MoiTruongHangHoaThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var ds = await e.NhaCungCap.LayDanhSach();
        Assert.Contains(ds, n => n.TenNCC == "Nhà cung cấp mới");
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm nhà cung cấp" && d.ChiTiet == "Nhà cung cấp mới");
    }

    [Fact]
    public async Task Them_TrungTen_BiTuChoi()
    {
        var e = new MoiTruongHangHoaThu();
        var ten = (await e.NhaCungCap.LayDanhSach()).First().TenNCC;
        var kq = await e.NhaCungCap.Them(new NhaCungCapMoi(ten, null, null), MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Fact]
    public async Task Them_TenRong_BiTuChoi()
    {
        var e = new MoiTruongHangHoaThu();
        var kq = await e.NhaCungCap.Them(new NhaCungCapMoi("  ", null, null), MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task Sua_DoiTen()
    {
        var e = new MoiTruongHangHoaThu();
        var ncc = (await e.NhaCungCap.LayDanhSach()).First();
        var kq = await e.NhaCungCap.Sua(ncc.MaNCC, new NhaCungCapSuaDoi("Tên đã đổi", ncc.SDT, ncc.DiaChi), MoiTruongHangHoaThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal("Tên đã đổi", (await e.NhaCungCap.Lay(ncc.MaNCC))!.TenNCC);
    }

    [Fact]
    public async Task Sua_KhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongHangHoaThu();
        var kq = await e.NhaCungCap.Sua(999, new NhaCungCapSuaDoi("A", null, null), MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }

    [Fact]
    public async Task NgungDung_KhongConTrongDanhSachHoatDong_VaMoLaiDuocLai()
    {
        var e = new MoiTruongHangHoaThu();
        var ncc = (await e.NhaCungCap.LayDanhSach()).First();

        await e.NhaCungCap.DatTrangThai(ncc.MaNCC, false, MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.DoesNotContain(await e.NhaCungCap.LayHoatDong(), n => n.MaNCC == ncc.MaNCC);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Ngừng dùng nhà cung cấp");

        await e.NhaCungCap.DatTrangThai(ncc.MaNCC, true, MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.Contains(await e.NhaCungCap.LayHoatDong(), n => n.MaNCC == ncc.MaNCC);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Mở lại nhà cung cấp");
    }
}
