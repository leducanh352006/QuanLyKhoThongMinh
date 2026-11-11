using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Infrastructure;

namespace QuanLyKho.Tests.KhoViTri;

public class ViTriTrucQuanTests
{
    private static (ViTriTrucQuanService Svc, MoiTruongKhoThu MoiTruong) Tao()
    {
        var mt = new MoiTruongKhoThu();
        var ton = new StockServiceStub();
        var svc = new ViTriTrucQuanService(mt.ViTri, ton, new KhongCoHangChoXep(), db: null);
        return (svc, mt);
    }

    [Fact]
    public async Task XayDung_Kho1Mau_ThongKeDungKhuTiepNhan_PhanTramLapDay_VaTonGiu()
    {
        var (svc, _) = Tao();

        var vm = await svc.XayDung(1, "Kho tổng", maBTThu: null, soLuongThu: null);

        Assert.Equal(3, vm.TongOLuuTru);
        Assert.Single(vm.KhuTiepNhan);
        Assert.Equal("Khu tiếp nhận", vm.KhuTiepNhan[0].Nut.TenViTri);

        var tatCaO = vm.KhuLuuTru.SelectMany(k => k.DanhSachKe).SelectMany(r => r.DanhSachO).ToList();
        Assert.Equal(3, tatCaO.Count);

        var oA101 = Assert.Single(tatCaO, x => x.Nut.MaViTri == 3);
        Assert.Equal(74, oA101.SoLuongTon);
        Assert.Equal(0, oA101.SoLuongGiu);
        Assert.True(oA101.PhanTramTheTich > 35);

        var oA102 = Assert.Single(tatCaO, x => x.Nut.MaViTri == 4);
        Assert.Equal(76, oA102.SoLuongTon);
        Assert.Equal(10, oA102.SoLuongGiu);

        // Ô A2-01 (60x40x40 cm, 20 kg) đang chứa 60 tai nghe x 0,25 kg = 15 kg (75% tải trọng).
        var oA201 = Assert.Single(tatCaO, x => x.Nut.MaViTri == 6);
        Assert.Equal(75, oA201.PhanTramKhoiLuong);
    }

    [Fact]
    public async Task XayDung_KiemTraNhanhVuaO_PhanBietDuCho_ThieuTai_VaQuaKho3D()
    {
        var (svc, mt) = Tao();

        // 1. Thử 25 tai nghe (BT 4: 15x12x6 cm, 0,25 kg): A1-01 & A1-02 đủ chỗ, A2-01 chỉ còn nhận +20 do tải trọng.
        var thuTaiNghe = await svc.XayDung(1, "Kho tổng", maBTThu: 4, soLuongThu: 25);
        var dsO = thuTaiNghe.KhuLuuTru.SelectMany(k => k.DanhSachKe).SelectMany(r => r.DanhSachO).ToDictionary(x => x.Nut.MaViTri);
        Assert.Equal(LoaiKetQuaThuO.DuCho, dsO[3].ThuKichThuoc!.Loai);
        Assert.Equal(LoaiKetQuaThuO.DuCho, dsO[4].ThuKichThuoc!.Loai);
        Assert.Equal(LoaiKetQuaThuO.ThieuSucChua, dsO[6].ThuKichThuoc!.Loai);
        Assert.Equal(20, dsO[6].ThuKichThuoc!.SoLuongToiDaThem);

        // 2. Thêm 1 ô hẹp 20x20x20 cm vào Kệ A2 (MaViTri = 5) rồi thử Áo thun Size M (BT 1: 30x25x3 cm) -> Quá khổ 3D!
        var kqThem = await mt.ViTri.Them(new ViTriMoi("Ô Hẹp 20cm", 1, 5, "Ô", "Lưu trữ", "O-HEP-20", 20, 20, 20, 30), MoiTruongKhoThu.MaNguoiThucHien);
        Assert.True(kqThem.ThanhCong);

        // Kiểm tra trực tiếp bằng dữ liệu cây có ô hẹp vừa thêm (với biến thể 1 kích thước 30x25x3 > 20x20x20).
        var thuAo = await svc.XayDung(1, "Kho tổng", maBTThu: 1, soLuongThu: 1);
        var oHep = thuAo.KhuLuuTru.SelectMany(k => k.DanhSachKe).SelectMany(r => r.DanhSachO)
            .Single(x => x.Nut.TenViTri == "Ô Hẹp 20cm");
        Assert.Equal(LoaiKetQuaThuO.QuaKichThuoc3D, oHep.ThuKichThuoc!.Loai);
        Assert.Equal(0, oHep.ThuKichThuoc!.SoLuongToiDaThem);
    }

    [Fact]
    public async Task XayDung_OBoCapTrucTiepDuoiDay_KhongBiBoSotTrenSoDo()
    {
        var (svc, mt) = Tao();
        await mt.ViTri.Them(new ViTriMoi("Ô Bỏ Cấp", 1, 1, "Ô", "Lưu trữ", "O-BOCAP", 80, 50, 40, 50), MoiTruongKhoThu.MaNguoiThucHien);

        var vm = await svc.XayDung(1, "Kho tổng", null, null);
        Assert.Equal(4, vm.TongOLuuTru);
        Assert.Contains(
            vm.KhuLuuTru.SelectMany(k => k.DanhSachKe).SelectMany(r => r.DanhSachO),
            o => o.Nut.TenViTri == "Ô Bỏ Cấp");
    }

    [Fact]
    public async Task DI_CheDoKhongCoCSDL_GiaiQuyetDuocViTriTrucQuanService_SauKhiDangKyNhapKho()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddKhoInfrastructure();
        services.AddContractStubs();
        services.AddHeThong();
        services.AddNhapKho();

        await using var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<IViTriTrucQuanService>();

        var vm = await svc.XayDung(1, "Kho tổng", maBTThu: 1, soLuongThu: 5);
        Assert.Equal(3, vm.TongOLuuTru);
        Assert.NotNull(vm.BienTheDangThu);
    }
}

