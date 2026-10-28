using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

public class LoHangServiceTests
{
    [Fact]
    public async Task CacLoMauTuSeedData_DaCoSan()
    {
        var e = new MoiTruongSanPhamThu();
        var ds = await e.LoHang.LayTheoBienThe(3);   // kem chống nắng: 2 lô khác hạn dùng (Bảng seed)
        Assert.Equal(2, ds.Count);
    }

    [Fact]
    public async Task LoMauChuaHetHan_KhongDanhDauDaHetHan()
    {
        var e = new MoiTruongSanPhamThu();
        var ds = await e.LoHang.LayTheoBienThe(3);
        Assert.All(ds, l => Assert.False(l.DaHetHan));   // hai lô mẫu đều có hạn dùng ở tương lai
    }

    [Fact]
    public async Task LoHetHan_DuocDanhDauDaHetHan()
    {
        var e = new MoiTruongSanPhamThu();
        var kem = (await e.SanPham.LayDanhSach()).First(s => s.CoHanDung);
        var bt = (await e.BienThe.LayTheoSanPham(kem.MaSP)).First();

        var hanDaQua = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var kq = await e.HangHoa.TaoLo(bt.MaBT, hanDaQua);
        Assert.True(kq.ThanhCong, kq.ThongBao);

        var ds = await e.LoHang.LayTheoBienThe(bt.MaBT);
        var lo = ds.Single(l => l.MaVachLo == kq.GiaTri!.MaVachLo);
        Assert.True(lo.DaHetHan);
    }

    // ---- TaoLo (gọi bởi nhóm 2 khi tiếp nhận hàng nhập, Bảng 1.6 dòng 2) ----

    [Fact]
    public async Task TaoLo_HangKhongCoHanDung_TuSinhMa_ThanhCong()
    {
        var e = new MoiTruongSanPhamThu();
        var aoThun = (await e.SanPham.LayDanhSach()).First(s => s.TenSP == "Áo thun cổ tròn");
        var bt = (await e.BienThe.LayTheoSanPham(aoThun.MaSP)).First();

        var kq = await e.HangHoa.TaoLo(bt.MaBT, null);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.NotNull(kq.GiaTri);
        Assert.False(string.IsNullOrWhiteSpace(kq.GiaTri!.MaVachLo));

        var ds = await e.LoHang.LayTheoBienThe(bt.MaBT);
        Assert.Contains(ds, l => l.MaVachLo == kq.GiaTri.MaVachLo);
    }

    [Fact]
    public async Task TaoLo_HangCoHanDung_ThieuHanDung_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var kem = (await e.SanPham.LayDanhSach()).First(s => s.CoHanDung);
        var bt = (await e.BienThe.LayTheoSanPham(kem.MaSP)).First();

        var kq = await e.HangHoa.TaoLo(bt.MaBT, null);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
    }

    [Fact]
    public async Task TaoLo_HangCoHanDung_CoHanDung_ThanhCong()
    {
        var e = new MoiTruongSanPhamThu();
        var kem = (await e.SanPham.LayDanhSach()).First(s => s.CoHanDung);
        var bt = (await e.BienThe.LayTheoSanPham(kem.MaSP)).First();

        var kq = await e.HangHoa.TaoLo(bt.MaBT, new DateOnly(2028, 1, 31), "LO-MOI-KEM-01");
        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal("LO-MOI-KEM-01", kq.GiaTri!.MaVachLo);
        Assert.Equal(new DateOnly(2028, 1, 31), kq.GiaTri.HanDung);
    }

    [Fact]
    public async Task TaoLo_MaVachTrungLoMau_BiTuChoi()
    {
        var e = new MoiTruongSanPhamThu();
        var aoThun = (await e.SanPham.LayDanhSach()).First(s => s.TenSP == "Áo thun cổ tròn");
        var bt = (await e.BienThe.LayTheoSanPham(aoThun.MaSP)).First();

        var kq = await e.HangHoa.TaoLo(bt.MaBT, null, "LO-2610-001");   // trùng lô mẫu
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, kq.MaLoi);
        Assert.Contains("đã tồn tại", kq.ThongBao);
    }

    [Fact]
    public async Task TaoLo_BienTheKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongSanPhamThu();
        var kq = await e.HangHoa.TaoLo(999, null);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }
}
