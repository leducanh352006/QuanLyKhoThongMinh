using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;

namespace QuanLyKho.Tests;

/// <summary>Hành vi bắt buộc của dịch vụ tồn kho (bản giả phải cư xử như bản thật sẽ cư xử).</summary>
public class StockServiceStubTests
{
    private static readonly ThamChieuChungTu PhieuXuat = new(LoaiChungTu.XK, 1);
    private const int MaND = 3;

    // Dữ liệu mẫu: lô 1 (áo M) ở ô 3 có tồn 50; lô 2 (áo L) ở ô 4 có tồn 40, đang giữ 10.
    private static readonly DongTon Ao50 = new(1, 3, 50);

    [Fact]
    public async Task GiuCho_TangSoLuongGiu_VaKhongGhiSoCai()
    {
        var s = new StockServiceStub();
        var kq = await s.GiuCho(new DongTon(1, 3, 20));
        Assert.True(kq.ThanhCong);
        var ton = await s.LayTon(1, 3);
        Assert.Equal(50, ton!.SoLuongTon);
        Assert.Equal(20, ton.SoLuongGiu);
        Assert.Equal(30, ton.KhaDung);
        Assert.Empty(s.SoCai);
    }

    [Fact]
    public async Task GiuCho_VuotKhaDung_BiTuChoiKemSoLieuCanVaCon()
    {
        var s = new StockServiceStub();
        var kq = await s.GiuCho(new DongTon(2, 4, 31));   // tồn 40, giữ 10 => khả dụng 30
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotTon, kq.MaLoi);
        Assert.Contains("cần 31", kq.ThongBao);
        Assert.Contains("còn 30", kq.ThongBao);
        Assert.Equal(10, (await s.LayTon(2, 4))!.SoLuongGiu);   // không đổi
    }

    [Fact]
    public async Task TraCho_GiamSoLuongGiu_VaKhongTraNhieuHonDangGiu()
    {
        var s = new StockServiceStub();
        Assert.True((await s.TraCho(new DongTon(2, 4, 10))).ThanhCong);
        Assert.Equal(0, (await s.LayTon(2, 4))!.SoLuongGiu);
        Assert.False((await s.TraCho(new DongTon(2, 4, 1))).ThanhCong);
    }

    [Fact]
    public async Task GhiTang_TangTon_VaGhiMotDongSoCaiDuong()
    {
        var s = new StockServiceStub();
        var kq = await s.GhiTang(new DongTon(1, 3, 5), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, 7), MaND);
        Assert.True(kq.ThanhCong);
        Assert.Equal(55, (await s.LayTon(1, 3))!.SoLuongTon);

        var dong = Assert.Single(s.SoCai);
        Assert.Equal(5, dong.SoLuong);
        Assert.Equal(LoaiBienDong.NhanHang, dong.LoaiBienDong);
        Assert.Equal(LoaiChungTu.NK, dong.LoaiCT);
        Assert.Equal(7, dong.MaCT);
        Assert.Equal(MaND, dong.MaND);
    }

    [Fact]
    public async Task GhiGiam_VuotTon_BiTuChoi_KhongDoiTon_KhongGhiSoCai()
    {
        var s = new StockServiceStub();
        var kq = await s.GhiGiam(new DongTon(1, 3, 51), LoaiBienDong.XuatKho, PhieuXuat, MaND);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotTon, kq.MaLoi);
        Assert.Contains("cần 51", kq.ThongBao);
        Assert.Contains("còn 50", kq.ThongBao);
        Assert.Equal(50, (await s.LayTon(1, 3))!.SoLuongTon);
        Assert.Empty(s.SoCai);
    }

    [Fact]
    public async Task GhiGiam_KhongTuGiuCho_KhongDuocLayPhanDangGiu()
    {
        var s = new StockServiceStub();
        // ô 4 lô 2: tồn 40, giữ 10; chỉ được giảm tối đa 30 nếu không dùng phần đã giữ
        Assert.False((await s.GhiGiam(new DongTon(2, 4, 31), LoaiBienDong.XuatKho, PhieuXuat, MaND)).ThanhCong);
        Assert.True((await s.GhiGiam(new DongTon(2, 4, 30), LoaiBienDong.XuatKho, PhieuXuat, MaND)).ThanhCong);
    }

    [Fact]
    public async Task GhiGiam_TuGiuCho_GiamCaTonVaGiu()
    {
        var s = new StockServiceStub();
        var kq = await s.GhiGiam(new DongTon(2, 4, 10), LoaiBienDong.XuatKho, PhieuXuat, MaND, tuGiuCho: true);
        Assert.True(kq.ThanhCong);
        var ton = await s.LayTon(2, 4);
        Assert.Equal(30, ton!.SoLuongTon);
        Assert.Equal(0, ton.SoLuongGiu);
        Assert.Equal(-10, Assert.Single(s.SoCai).SoLuong);
    }

    [Fact]
    public async Task GhiGiam_TuGiuCho_QuaPhanDangGiu_BiTuChoi()
    {
        var s = new StockServiceStub();
        var kq = await s.GhiGiam(new DongTon(2, 4, 11), LoaiBienDong.XuatKho, PhieuXuat, MaND, tuGiuCho: true);
        Assert.False(kq.ThanhCong);
        Assert.Contains("đang giữ 10", kq.ThongBao);
    }

    [Fact]
    public async Task ChuyenO_TonTongKhongDoi_GhiHaiDongSoCaiDoiO()
    {
        var s = new StockServiceStub();
        var kq = await s.ChuyenO(1, 3, 4, 20, new ThamChieuChungTu(LoaiChungTu.KK, 2), MaND);
        Assert.True(kq.ThanhCong);

        Assert.Equal(30, (await s.LayTon(1, 3))!.SoLuongTon);
        Assert.Equal(20, (await s.LayTon(1, 4))!.SoLuongTon);
        Assert.Equal(2, s.SoCai.Count);
        Assert.All(s.SoCai, d => Assert.Equal(LoaiBienDong.DoiO, d.LoaiBienDong));
        Assert.Equal(0, s.SoCai.Sum(d => d.SoLuong));          // tồn tổng không đổi
    }

    [Fact]
    public async Task ChuyenO_VuotKhaDung_VaCungO_BiTuChoi_KhongDoiGi()
    {
        var s = new StockServiceStub();
        Assert.False((await s.ChuyenO(1, 3, 4, 51, PhieuXuat, MaND)).ThanhCong);
        Assert.False((await s.ChuyenO(1, 3, 3, 1, PhieuXuat, MaND)).ThanhCong);
        Assert.Equal(50, (await s.LayTon(1, 3))!.SoLuongTon);
        Assert.Null(await s.LayTon(1, 4));
        Assert.Empty(s.SoCai);
    }

    [Fact]
    public async Task SoLuongKhongDuong_ViTriKhongPhaiO_ViTriKhongCo_BiTuChoi()
    {
        var s = new StockServiceStub();
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, (await s.GiuCho(new DongTon(1, 3, 0))).MaLoi);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, (await s.GhiTang(new DongTon(1, 2, 1), LoaiBienDong.NhanHang, PhieuXuat, MaND)).MaLoi);   // ô 2 là Kệ
        Assert.Equal(MaLoiDichVu.KhongTimThay, (await s.GhiTang(new DongTon(1, 999, 1), LoaiBienDong.NhanHang, PhieuXuat, MaND)).MaLoi);
    }

    [Fact]
    public async Task HaiThaoTacLayHangDongThoi_KhongLamTonAm()
    {
        var s = new StockServiceStub();
        // 100 luồng cùng lấy 1 đơn vị từ tồn 50: đúng 50 thành công, 50 bị từ chối, tồn về 0 chứ không âm.
        var kq = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => s.GhiGiam(new DongTon(1, 3, 1), LoaiBienDong.XuatKho, PhieuXuat, MaND))));
        Assert.Equal(50, kq.Count(k => k.ThanhCong));
        Assert.Equal(50, kq.Count(k => !k.ThanhCong && k.MaLoi == MaLoiDichVu.VuotTon));
        Assert.Equal(0, (await s.LayTon(1, 3))!.SoLuongTon);
        Assert.Equal(50, s.SoCai.Count);
    }

    [Fact]
    public async Task DoiChieuSoCai_KhopSauChuoiThaoTac_VaLayTonTheoViTriDung()
    {
        var s = new StockServiceStub();
        await s.GhiTang(new DongTon(1, 3, 10), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, 1), MaND);
        await s.GhiGiam(new DongTon(1, 3, 5), LoaiBienDong.XuatKho, PhieuXuat, MaND);
        await s.ChuyenO(1, 3, 4, 7, PhieuXuat, MaND);
        await s.GiuCho(new DongTon(1, 4, 2));

        Assert.Empty(await s.DoiChieuSoCai());

        var o3 = await s.LayTonTheoViTri(3);
        Assert.Equal(new[] { 1, 3 }, o3.Select(t => t.MaLo).OrderBy(x => x).ToArray());   // lô 1 và lô 3 (kem) ở ô 3
        Assert.Equal(48, o3.Single(t => t.MaLo == 1).SoLuongTon);                          // 50 + 10 - 5 - 7
    }
}
