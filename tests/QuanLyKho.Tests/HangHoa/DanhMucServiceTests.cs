using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

public class DanhMucServiceTests
{
    [Fact]
    public async Task BaDanhMucMau_DaCoSanTuSeedData()
    {
        var e = new MoiTruongHangHoaThu();
        var ds = await e.DanhMuc.LayDanhSach();
        Assert.Equal(3, ds.Count);
    }

    [Fact]
    public async Task Them_ThanhCong_GhiNhatKy()
    {
        var e = new MoiTruongHangHoaThu();
        var kq = await e.DanhMuc.Them(new DanhMucMoi("Danh mục mới"), MoiTruongHangHoaThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Contains(await e.DanhMuc.LayDanhSach(), d => d.TenDM == "Danh mục mới");
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Thêm danh mục" && d.ChiTiet == "Danh mục mới");
    }

    [Fact]
    public async Task Them_TrungTen_BiTuChoi()
    {
        var e = new MoiTruongHangHoaThu();
        var ten = (await e.DanhMuc.LayDanhSach()).First().TenDM;
        var kq = await e.DanhMuc.Them(new DanhMucMoi(ten), MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Fact]
    public async Task Sua_DoiTen()
    {
        var e = new MoiTruongHangHoaThu();
        var dm = (await e.DanhMuc.LayDanhSach()).First();
        var kq = await e.DanhMuc.Sua(dm.MaDM, new DanhMucSuaDoi("Tên đã đổi"), MoiTruongHangHoaThu.MaNguoiThucHien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal("Tên đã đổi", (await e.DanhMuc.Lay(dm.MaDM))!.TenDM);
    }

    [Fact]
    public async Task Xoa_DanhMucConSanPhamThamChieu_BiTuChoi()
    {
        // SeedDataSet.SanPham tham chiếu MaDM 1..3 (cùng bộ dữ liệu mẫu dùng ở BoNhoHangHoaStore).
        var e = new MoiTruongHangHoaThu();
        var kq = await e.DanhMuc.Xoa(1, MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
    }

    [Fact]
    public async Task Xoa_DanhMucKhongAiThamChieu_ThanhCong()
    {
        var e = new MoiTruongHangHoaThu();
        var them = await e.DanhMuc.Them(new DanhMucMoi("Danh mục chưa dùng"), MoiTruongHangHoaThu.MaNguoiThucHien);
        var moi = (await e.DanhMuc.LayDanhSach()).First(d => d.TenDM == "Danh mục chưa dùng");

        var kq = await e.DanhMuc.Xoa(moi.MaDM, MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.DoesNotContain(await e.DanhMuc.LayDanhSach(), d => d.MaDM == moi.MaDM);
    }

    [Fact]
    public async Task Xoa_KhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongHangHoaThu();
        var kq = await e.DanhMuc.Xoa(999, MoiTruongHangHoaThu.MaNguoiThucHien);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }
}
