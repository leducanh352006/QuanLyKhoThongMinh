using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Tests.HangHoa;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-4 (Sắp xếp vào ô): quét mã, sức chứa, chia nhiều ô, tồn khả dụng sau xác nhận.</summary>
public class SapXepTests
{
    private const int Nguoi = TonKhoSeeder.MaNguoiGhiTonMau;

    // ---- Không cần SQL Server ----

    [Fact]
    public void HangSoTrangThai_KhopBang216()
    {
        Assert.Equal("Chờ xác nhận", SapXep.ChoXacNhan);
        Assert.Equal("Đã xác nhận", SapXep.DaXacNhan);
        Assert.Equal("Đã hủy", SapXep.DaHuy);
    }

    [Theory]
    [InlineData("O-K1-A1-01", "O-K1-A1-01", true)]
    [InlineData("  o-k1-a1-01 ", "O-K1-A1-01", true)]   // máy quét có thể thừa khoảng trắng; không phân biệt hoa thường
    [InlineData("O-K1-A1-02", "O-K1-A1-01", false)]
    [InlineData(null, "O-K1-A1-01", false)]
    [InlineData("", "O-K1-A1-01", false)]
    [InlineData("O-K1-A1-01", null, false)]
    [InlineData("", "", false)]                          // ô hoặc lô chưa có mã vạch thì không bao giờ khớp
    public void Khop_BoQuaHoaThuongVaKhoangTrang(string? quet, string? maVach, bool mong) =>
        Assert.Equal(mong, SapXepService.Khop(quet, maVach));

    [Fact]
    public void CauHinhBang_DuCotRangBuocKhoaNgoaiVaChiMuc()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var sx = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(SapXep))!;

        Assert.Equal("SapXep", sx.GetTableName());
        Assert.False(sx.FindProperty("MaCTN")!.IsNullable);
        Assert.False(sx.FindProperty("MaViTri")!.IsNullable);
        Assert.True(sx.FindProperty("MaNguoiXN")!.IsNullable);
        Assert.True(sx.FindProperty("ThoiGianXN")!.IsNullable);
        Assert.Equal(SapXep.ChoXacNhan, sx.FindProperty("TrangThai")!.GetDefaultValue());
        Assert.Equal(new[] { "ChiTietPhieuNhap", "NguoiDung", "ViTri" },
            sx.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());
        Assert.All(sx.GetForeignKeys(), f => Assert.Equal(DeleteBehavior.Restrict, f.DeleteBehavior));
        Assert.Equal(new[] { "CK_SapXep_SoLuong", "CK_SapXep_TrangThai", "CK_SapXep_XacNhan" },
            sx.GetCheckConstraints().Select(c => c.Name!).OrderBy(x => x).ToArray());
        Assert.Contains(sx.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "MaViTri", "TrangThai" }));
        Assert.Contains("20261029000000_TaoSapXep", db.Database.GetMigrations());
    }

    [Fact]
    public void NguonHangChoXep_NhapKhoDangKySauHeThongNenThayBanMacDinh()
    {
        var services = new ServiceCollection().AddKhoInfrastructure().AddHeThong().AddNhapKho();
        Assert.Equal(typeof(HangChoXepTuSapXep), services.Last(d => d.ServiceType == typeof(INguonHangChoXep)).ImplementationType);
    }

    [Fact]
    public void Menu_CoMucSapXepVaoOChoThuKho() =>
        Assert.Contains(new NhapKhoMenu().LayNhom().Muc,
            m => m.Controller == "SapXep" && m.Action == "Index" && m.VaiTro.Contains(VaiTroTen.ThuKho));

    // ---- Cần SQL Server đã migrate và seed: mỗi bài chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu ----

    /// <summary>ToiDa: số đơn vị của biến thể thử mà ô O còn nhận thêm được (tính cả hàng sẵn có trong dữ liệu mẫu).</summary>
    private sealed record MoiTruong(KhoDbContext Db, SapXepService Sx, StockService Ton, ICapacityService Cap, ViTri O, ViTri OKhac, ViTri TiepNhan, LoHang Lo, int ToiDa);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var ton = new StockService(db);
        var cap = new CapacityService(db, new HangChoXepTuSapXep(db));
        var oLuuTru = db.ViTri.AsNoTracking().Where(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru
                                                         && v.TrangThai == TrangThaiHoatDong.HoatDong && !v.DangKhoa).OrderBy(v => v.MaViTri);
        var o = await oLuuTru.FirstAsync();
        var oKhac = await oLuuTru.FirstAsync(v => v.MaKho == o.MaKho && v.MaViTri != o.MaViTri);
        var tiepNhan = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaKho == o.MaKho && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan);
        var lo = await db.LoHang.AsNoTracking().OrderBy(l => l.MaLo).FirstAsync();
        var toiDa = (await cap.KiemTra(o.MaViTri, lo.MaBT, 1_000_000)).SoLuongToiDaThem ?? int.MaxValue;
        Assert.True(toiDa is >= 3 and < 100_000, $"Dữ liệu mẫu: ô {o.TenViTri} phải nhận thêm được từ 3 đến 99999 đơn vị của biến thể thử, hiện là {toiDa}.");

        await chay(new MoiTruong(db, new SapXepService(db, ton, cap, new NhatKyGia()), ton, cap, o, oKhac, tiepNhan, lo, toiDa));
    }

    /// <summary>Phiếu Đã kiểm tra một dòng, <paramref name="dat"/> hàng đạt đang nằm ở ô Tiếp nhận (như sau khi T1-2 tiếp nhận xong).</summary>
    private static async Task<(PhieuNhap Phieu, int MaCTN)> TaoPhieuDaKiemTraAsync(MoiTruong e, int dat)
    {
        var p = new PhieuNhap
        {
            SoPhieu = "T14" + Guid.NewGuid().ToString("N")[..10], LoaiPhieu = "Mua hàng", ThoiGian = DateTime.UtcNow, MaKho = e.O.MaKho,
            MaNguoiLap = Nguoi, TrangThai = PhieuNhap.DaKiemTra,
            ChiTiet = { new ChiTietPhieuNhap { MaBT = e.Lo.MaBT, SLDuKien = dat, MaLo = e.Lo.MaLo, SLThucNhan = dat, SLDat = dat } },
        };
        e.Db.PhieuNhap.Add(p);
        await e.Db.SaveChangesAsync();
        var tang = await e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.TiepNhan.MaViTri, dat), LoaiBienDong.NhanHang, new ThamChieuChungTu(LoaiChungTu.NK, p.MaPN), Nguoi);
        Assert.True(tang.ThanhCong, tang.ThongBao);
        return (p, p.ChiTiet[0].MaCTN);
    }

    private static async Task<SapXep> LapAsync(MoiTruong e, int maCTN, ViTri o, int soLuong)
    {
        var kq = await e.Sx.LapLenh(maCTN, o.MaViTri, soLuong, false, Nguoi);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        return kq.GiaTri!.Single();
    }

    private static Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon).FirstOrDefaultAsync();

    private static Task<string> TrangThaiLenh(KhoDbContext db, int maSX) =>
        db.SapXep.AsNoTracking().Where(s => s.MaSX == maSX).Select(s => s.TrangThai).SingleAsync();

    private static Task<string> TrangThaiPhieu(KhoDbContext db, int maPN) =>
        db.PhieuNhap.AsNoTracking().Where(p => p.MaPN == maPN).Select(p => p.TrangThai).SingleAsync();

    [CanSqlServerFact]
    public async Task QuetSaiOHoacLo_KhongGhiNhanGiCa() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuDaKiemTraAsync(e, 2);
        var lenh = await LapAsync(e, ctn, e.O, 2);
        var tnTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri);
        var oTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);

        var saiO = await e.Sx.XacNhan(lenh.MaSX, e.OKhac.MaVachO, e.Lo.MaVachLo, Nguoi);
        var saiLo = await e.Sx.XacNhan(lenh.MaSX, e.O.MaVachO, "LO-KHONG-DUNG", Nguoi);
        var khongQuet = await e.Sx.XacNhan(lenh.MaSX, null, null, Nguoi);

        Assert.False(saiO.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, saiO.MaLoi);
        Assert.Contains(e.O.TenViTri, saiO.ThongBao);   // nói ô cần quét, không nói mã
        Assert.False(saiLo.ThanhCong);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, saiLo.MaLoi);
        Assert.False(khongQuet.ThanhCong);
        Assert.Equal(SapXep.ChoXacNhan, await TrangThaiLenh(e.Db, lenh.MaSX));
        Assert.Equal(PhieuNhap.DaKiemTra, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.Equal(tnTruoc, await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri));
        Assert.Equal(oTruoc, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });

    [CanSqlServerFact]
    public async Task XacNhanDungMa_ChuyenTonKhuTiepNhanSangOVaHoanTatPhieuKhiXepHet() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuDaKiemTraAsync(e, 3);
        var lenh1 = await LapAsync(e, ctn, e.O, 2);
        var tnTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri);
        var oTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);

        var kq1 = await e.Sx.XacNhan(lenh1.MaSX, " " + e.O.MaVachO!.ToLowerInvariant() + " ", e.Lo.MaVachLo, Nguoi);

        Assert.True(kq1.ThanhCong, kq1.ThongBao);
        var da = await e.Db.SapXep.AsNoTracking().SingleAsync(s => s.MaSX == lenh1.MaSX);
        Assert.Equal(SapXep.DaXacNhan, da.TrangThai);
        Assert.Equal(Nguoi, da.MaNguoiXN);
        Assert.NotNull(da.ThoiGianXN);
        Assert.Equal(tnTruoc - 2, await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri));   // khu tiếp nhận giảm đúng
        Assert.Equal(oTruoc + 2, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));            // ô tăng đúng, từ đây mới là tồn khả dụng
        Assert.Equal(PhieuNhap.DaKiemTra, await TrangThaiPhieu(e.Db, p.MaPN));             // còn 1 hàng đạt chưa xếp

        var lai = await e.Sx.XacNhan(lenh1.MaSX, e.O.MaVachO, e.Lo.MaVachLo, Nguoi);       // quét lại lệnh đã xác nhận
        Assert.False(lai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, lai.MaLoi);
        Assert.Equal(oTruoc + 2, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));

        var lenh2 = await LapAsync(e, ctn, e.OKhac, 1);
        var kq2 = await e.Sx.XacNhan(lenh2.MaSX, e.OKhac.MaVachO, e.Lo.MaVachLo, Nguoi);
        Assert.True(kq2.ThanhCong, kq2.ThongBao);
        Assert.Equal(PhieuNhap.HoanTat, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.DoesNotContain(await e.Ton.DoiChieuSoCai(), c => c.MaLo == e.Lo.MaLo);      // sổ cái vẫn khớp tồn
    });

    [CanSqlServerFact]
    public async Task LapLenh_DongVuotSucChuaMotO_ChiaVaoNhieuO() => await DungAsync(async e =>
    {
        var (_, ctn) = await TaoPhieuDaKiemTraAsync(e, e.ToiDa + 1);

        var khongChia = await e.Sx.LapLenh(ctn, e.O.MaViTri, e.ToiDa + 1, false, Nguoi);
        Assert.False(khongChia.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotSucChua, khongChia.MaLoi);
        Assert.False(await e.Db.SapXep.AnyAsync(s => s.MaCTN == ctn));   // không lập lệnh nào

        var chia = await e.Sx.LapLenh(ctn, e.O.MaViTri, e.ToiDa + 1, true, Nguoi);

        Assert.True(chia.ThanhCong, chia.ThongBao);
        var lenh = chia.GiaTri!;
        Assert.True(lenh.Count >= 2);
        Assert.Equal(e.O.MaViTri, lenh[0].MaViTri);
        Assert.Equal(e.ToiDa, lenh[0].SoLuong);                         // ô đã chọn nhận đầy, phần dư sang ô khác
        Assert.Equal(e.ToiDa + 1, lenh.Sum(l => l.SoLuong));
        Assert.Equal(lenh.Count, lenh.Select(l => l.MaViTri).Distinct().Count());
        Assert.All(lenh, l => { Assert.True(l.MaSX > 0); Assert.Equal(SapXep.ChoXacNhan, l.TrangThai); });
    });

    [CanSqlServerFact]
    public async Task LenhChoXacNhan_ChiemChoTrongOCuaSucChuaVaHuyThiTraLai() => await DungAsync(async e =>
    {
        var (_, ctn) = await TaoPhieuDaKiemTraAsync(e, e.ToiDa);

        var lenh = await LapAsync(e, ctn, e.O, e.ToiDa);   // lấp đầy chỗ còn lại của ô nhưng hàng chưa vào TonKho

        Assert.False((await e.Cap.KiemTra(e.O.MaViTri, e.Lo.MaBT, 1)).Du);
        var huy = await e.Sx.Huy(lenh.MaSX, Nguoi);
        Assert.True(huy.ThanhCong, huy.ThongBao);
        Assert.Equal(SapXep.DaHuy, await TrangThaiLenh(e.Db, lenh.MaSX));
        Assert.True((await e.Cap.KiemTra(e.O.MaViTri, e.Lo.MaBT, e.ToiDa)).Du);
        Assert.False((await e.Sx.Huy(lenh.MaSX, Nguoi)).ThanhCong);   // hủy rồi không hủy lại được
    });

    [CanSqlServerFact]
    public async Task XacNhan_OHetChoSauKhiLapLenh_BiTuChoiVaLenhVanChoXacNhan() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuDaKiemTraAsync(e, 2);
        var lenh = await LapAsync(e, ctn, e.O, 2);
        // Có hàng khác vào ô sau khi lập lệnh (ví dụ nhận chuyển kho), chỉ còn chỗ cho 1 đơn vị.
        var them = await e.Ton.GhiTang(new DongTon(e.Lo.MaLo, e.O.MaViTri, e.ToiDa - 1), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, 1_000_000), Nguoi);
        Assert.True(them.ThanhCong, them.ThongBao);
        var tnTruoc = await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri);

        var kq = await e.Sx.XacNhan(lenh.MaSX, e.O.MaVachO, e.Lo.MaVachLo, Nguoi);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotSucChua, kq.MaLoi);
        Assert.Equal(SapXep.ChoXacNhan, await TrangThaiLenh(e.Db, lenh.MaSX));   // hoàn tác cả việc đổi trạng thái lệnh
        Assert.Null((await e.Db.SapXep.AsNoTracking().SingleAsync(s => s.MaSX == lenh.MaSX)).MaNguoiXN);
        Assert.Equal(PhieuNhap.DaKiemTra, await TrangThaiPhieu(e.Db, p.MaPN));
        Assert.Equal(tnTruoc, await TonAsync(e.Db, e.Lo.MaLo, e.TiepNhan.MaViTri));
    });

    [CanSqlServerFact]
    public async Task LapLenh_HaiLenhCungSoLuongCuaMotDong_BiTuChoi() => await DungAsync(async e =>
    {
        var (_, ctn) = await TaoPhieuDaKiemTraAsync(e, 4);
        await LapAsync(e, ctn, e.O, 2);

        var trung = await e.Sx.LapLenh(ctn, e.OKhac.MaViTri, 2, false, Nguoi);   // StockService.ChuyenO sẽ bỏ qua lần chuyển thứ hai cùng số lượng

        Assert.False(trung.ThanhCong);
        Assert.Contains("cùng số lượng", trung.ThongBao);
        Assert.Equal(1, await e.Db.SapXep.CountAsync(s => s.MaCTN == ctn));
    });

    [CanSqlServerFact]
    public async Task LapLenh_SaiODichSoLuongHoacTrangThaiPhieu_BiTuChoi() => await DungAsync(async e =>
    {
        var (p, ctn) = await TaoPhieuDaKiemTraAsync(e, 2);

        var vaoTiepNhan = await e.Sx.LapLenh(ctn, e.TiepNhan.MaViTri, 2, false, Nguoi);   // hàng đạt chỉ xếp vào ô Lưu trữ
        var quaSoDat = await e.Sx.LapLenh(ctn, e.O.MaViTri, 3, false, Nguoi);
        var khongSo = await e.Sx.LapLenh(ctn, e.O.MaViTri, 0, false, Nguoi);
        await e.Db.PhieuNhap.Where(x => x.MaPN == p.MaPN).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuNhap.DaXacNhan));
        var chuaKiem = await e.Sx.LapLenh(ctn, e.O.MaViTri, 2, false, Nguoi);

        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, vaoTiepNhan.MaLoi);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, quaSoDat.MaLoi);
        Assert.Equal(MaLoiDichVu.DuLieuKhongHopLe, khongSo.MaLoi);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, chuaKiem.MaLoi);
        Assert.False(await e.Db.SapXep.AnyAsync(s => s.MaCTN == ctn));
    });
}
