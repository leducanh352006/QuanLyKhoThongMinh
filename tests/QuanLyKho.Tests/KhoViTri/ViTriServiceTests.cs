using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.KhoViTri;

public class CapViTriThuTuTests
{
    [Theory]
    [InlineData("Tòa", 0)]
    [InlineData("Tầng", 1)]
    [InlineData("Phòng", 2)]
    [InlineData("Dãy", 3)]
    [InlineData("Hàng", 4)]
    [InlineData("Kệ", 5)]
    [InlineData("Ô", 6)]
    public void ChiSo_DungThuTuBaoCao(string cap, int chiSoMongDoi)
        => Assert.Equal(chiSoMongDoi, QuanLyKho.Web.Areas.HeThong.Models.CapViTriGiaTri.ChiSo(cap));

    [Fact]
    public void ChiSo_CapKhongCo_TraAm1()
        => Assert.Equal(-1, QuanLyKho.Web.Areas.HeThong.Models.CapViTriGiaTri.ChiSo("Không có"));
}

public class ViTriServiceTests
{
    // Seed Kho 1 ("Kho tổng"): MaViTri 1 Dãy A (gốc) > 2 Kệ A1 > 3,4 Ô; 5 Kệ A2 > 6 Ô; 7 Khu tiếp nhận (ô gốc).
    private static ViTriMoi MoiDay(string ten = "Dãy B", int maKho = MoiTruongKhoThu.MaKho1, int? cha = null) => new(ten, maKho, cha, "Dãy");
    private static ViTriMoi MoiOLuuTru(string ten, int maCha, string maVach) => new(ten, MoiTruongKhoThu.MaKho1, maCha, "Ô", "Lưu trữ", maVach, 50, 40, 40, 30);
    private static ViTriMoi MoiOTiepNhan(string ten, int? maCha, string maVach) => new(ten, MoiTruongKhoThu.MaKho1, maCha, "Ô", "Tiếp nhận", maVach);

    [Fact]
    public async Task LayCay_DungSoCapVaDuongDanDayDu()
    {
        var e = new MoiTruongKhoThu();
        var cay = (await e.ViTri.LayCay(MoiTruongKhoThu.MaKho1)).GiaTri!;

        var dayA = Assert.Single(cay, v => v.TenViTri == "Dãy A");
        Assert.Equal("Kho tổng ▷ Dãy A", dayA.DuongDanDayDu);
        var keA1 = Assert.Single(dayA.Con, v => v.TenViTri == "Kệ A1");
        Assert.Equal("Kho tổng ▷ Dãy A ▷ Kệ A1", keA1.DuongDanDayDu);
        var oA101 = Assert.Single(keA1.Con, v => v.TenViTri == "Ô A1-01");
        Assert.Equal("Kho tổng ▷ Dãy A ▷ Kệ A1 ▷ Ô A1-01", oA101.DuongDanDayDu);
        Assert.True(oA101.LaO);

        // Khu tiếp nhận: ô gốc, bỏ qua mọi cấp trung gian.
        var khuTiepNhan = Assert.Single(cay, v => v.TenViTri == "Khu tiếp nhận");
        Assert.Equal("Kho tổng ▷ Khu tiếp nhận", khuTiepNhan.DuongDanDayDu);
        Assert.Equal("Tiếp nhận", khuTiepNhan.LoaiViTri);
    }

    [Fact]
    public async Task LayCay_KhoKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.LayCay(999);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }

    [Fact]
    public async Task Them_VaiTroGoc_DuocPhepOMoiCap()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Them(MoiDay(), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
    }

    [Fact]
    public async Task Them_CapConPhaiThapHonCapCha()
    {
        var e = new MoiTruongKhoThu();
        // Dãy A (MaViTri=1) là Dãy; thêm con cấp "Tầng" (cao hơn Dãy) phải bị từ chối.
        var kq = await e.ViTri.Them(new ViTriMoi("Tầng lạ", MoiTruongKhoThu.MaKho1, 1, "Tầng"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("phải thấp hơn", kq.ThongBao);
    }

    [Fact]
    public async Task Them_DuocPhepBoQuaCap()
    {
        var e = new MoiTruongKhoThu();
        // Dãy A (MaViTri=1) thêm thẳng con cấp "Kệ" đã có sẵn (seed), thử thêm cấp "Ô" trực tiếp dưới Dãy (bỏ qua Kệ, Hàng).
        var kq = await e.ViTri.Them(MoiOLuuTru("Ô bỏ cấp", 1, "O-BO-CAP"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
    }

    [Fact]
    public async Task Them_ChaLaO_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // MaViTri 3 = "Ô A1-01" (seed); không thể thêm con dưới ô.
        var kq = await e.ViTri.Them(MoiOLuuTru("Con của ô", 3, "O-CON"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("không thể có vị trí con", kq.ThongBao);
    }

    [Fact]
    public async Task Them_ChaKhacKho_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // MaViTri 8 = "Dãy A" của Kho 2 (seed); thêm vị trí Kho 1 với cha thuộc Kho 2 phải bị từ chối.
        var kq = await e.ViTri.Them(new ViTriMoi("Kệ lạ", MoiTruongKhoThu.MaKho1, 8, "Kệ"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("không thuộc kho này", kq.ThongBao);
    }

    [Fact]
    public async Task Them_OLuuTru_ThieuKichThuoc_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Them(new ViTriMoi("Ô thiếu", MoiTruongKhoThu.MaKho1, 1, "Ô", "Lưu trữ", "O-THIEU", 50, 40, null, 30), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đủ dài, rộng, cao", kq.ThongBao);
    }

    [Fact]
    public async Task Them_OLuuTru_KichThuocAmHoacBang0_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Them(new ViTriMoi("Ô âm", MoiTruongKhoThu.MaKho1, 1, "Ô", "Lưu trữ", "O-AM", 0, 40, 40, 30), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
    }

    [Fact]
    public async Task Them_OTiepNhan_CoKichThuoc_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Them(new ViTriMoi("TN sai", MoiTruongKhoThu.MaKho1, null, "Ô", "Tiếp nhận", "O-TN-SAI", 10, 10, 10, 10), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("không giới hạn kích thước", kq.ThongBao);
    }

    [Fact]
    public async Task Them_O_ThieuLoaiHoacMaVach_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        Assert.False((await e.ViTri.Them(new ViTriMoi("Ô 1", MoiTruongKhoThu.MaKho1, 1, "Ô", null, "O-X"), MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
        Assert.False((await e.ViTri.Them(new ViTriMoi("Ô 2", MoiTruongKhoThu.MaKho1, 1, "Ô", "Lưu trữ", null, 1, 1, 1, 1), MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
    }

    [Fact]
    public async Task Them_CapKhacO_MaCoTruongCuaO_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Them(new ViTriMoi("Kệ sai", MoiTruongKhoThu.MaKho1, 1, "Kệ", MaVachO: "K-SAI"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("Chỉ ô", kq.ThongBao);
    }

    [Fact]
    public async Task Them_TrungTenCungCapCha_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // "Kệ A1" đã có sẵn dưới Dãy A (MaViTri=1).
        var kq = await e.ViTri.Them(new ViTriMoi("Kệ A1", MoiTruongKhoThu.MaKho1, 1, "Kệ"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Fact]
    public async Task Them_TrungMaVachO_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // "O-K1-A1-01" đã dùng cho Ô A1-01 (seed).
        var kq = await e.ViTri.Them(MoiOLuuTru("Ô trùng mã", 1, "O-K1-A1-01"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("Mã vạch", kq.ThongBao);
    }

    [Fact]
    public async Task LayUngVienLamCha_LoaiTruOVaKhoKhac()
    {
        var e = new MoiTruongKhoThu();
        var ungVien = await e.ViTri.LayUngVienLamCha(MoiTruongKhoThu.MaKho1);
        Assert.DoesNotContain(ungVien, x => x.MaViTri == 3);   // Ô A1-01 không thể làm cha
        Assert.DoesNotContain(ungVien, x => x.MaViTri == 8);   // thuộc Kho 2
        Assert.Contains(ungVien, x => x.MaViTri == 1);         // Dãy A
    }

    [Fact]
    public async Task Sua_DoiTenOVaKichThuoc()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Sua(3, new ViTriSuaDoi("Ô A1-01 (đổi)", "Lưu trữ", "O-K1-A1-01", 120, 60, 50, 150), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        var v = await e.ViTri.LayMot(3);
        Assert.Equal("Ô A1-01 (đổi)", v!.TenViTri);
        Assert.Equal(150, v.KhoiLuongToiDa);
    }

    [Fact]
    public async Task Sua_KhongTimThay_BaoLoi()
        => Assert.Equal(MaLoiDichVu.KhongTimThay, (await new MoiTruongKhoThu().ViTri.Sua(999, new ViTriSuaDoi("X"), MoiTruongKhoThu.MaNguoiThucHien)).MaLoi);

    [Fact]
    public async Task Khoa_ConCoVaiTroConHoatDong_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // "Dãy A" (MaViTri=1) còn "Kệ A1" đang hoạt động bên dưới.
        var kq = await e.ViTri.DatTrangThai(1, false, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("vị trí con", kq.ThongBao);
    }

    [Fact]
    public async Task Khoa_KhuTiepNhanCuoiCungCuaKho_BiTuChoi_ConKhuKhacThiDuoc()
    {
        var e = new MoiTruongKhoThu();
        var cuoiCung = await e.ViTri.DatTrangThai(7, false, MoiTruongKhoThu.MaNguoiThucHien);   // Khu tiếp nhận Kho 1, duy nhất
        Assert.False(cuoiCung.ThanhCong);
        Assert.Contains("khu tiếp nhận", cuoiCung.ThongBao);

        await e.ViTri.Them(MoiOTiepNhan("Khu tiếp nhận 2", null, "O-TN-2"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True((await e.ViTri.DatTrangThai(7, false, MoiTruongKhoThu.MaNguoiThucHien)).ThanhCong);
    }

    [Fact]
    public async Task Khoa_OLuuTru_KhongVuongKhuTiepNhan_Duoc()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.DatTrangThai(6, false, MoiTruongKhoThu.MaNguoiThucHien);   // Ô A2-01, không phải khu tiếp nhận
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.False((await e.ViTri.LayMot(6))!.DangHoatDong);
    }

    [Fact]
    public async Task Khoa_VaMoLai_GhiNhatKy()
    {
        var e = new MoiTruongKhoThu();
        await e.ViTri.DatTrangThai(4, false, MoiTruongKhoThu.MaNguoiThucHien);   // Ô A1-02
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Khóa vị trí");
        await e.ViTri.DatTrangThai(4, true, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Mở lại vị trí");
    }

    [Fact]
    public async Task Xoa_OConTon_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        // Ô A1-01 (MaViTri=3) có tồn theo SeedData.TonKho.
        var kq = await e.ViTri.Xoa(3, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.False(kq.ThanhCong);
        Assert.Contains("còn tồn kho", kq.ThongBao);
    }

    [Fact]
    public async Task Xoa_ConVaiTroCon_BiTuChoi()
    {
        var e = new MoiTruongKhoThu();
        var kq = await e.ViTri.Xoa(2, MoiTruongKhoThu.MaNguoiThucHien);   // Kệ A1 còn 2 ô con
        Assert.False(kq.ThanhCong);
        Assert.Contains("vị trí con", kq.ThongBao);
    }

    [Fact]
    public async Task Xoa_OKhongTonVaKhongConCon_ThanhCong()
    {
        var e = new MoiTruongKhoThu();
        var them = await e.ViTri.Them(MoiOLuuTru("Ô tạm xóa", 1, "O-TAM-XOA"), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(them.ThanhCong);
        var moi = (await e.ViTri.LayCay(MoiTruongKhoThu.MaKho1)).GiaTri!.Single(v => v.TenViTri == "Dãy A").Con.Single(v => v.TenViTri == "Ô tạm xóa");

        var kq = await e.ViTri.Xoa(moi.MaViTri, MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Null(await e.ViTri.LayMot(moi.MaViTri));
    }
}
