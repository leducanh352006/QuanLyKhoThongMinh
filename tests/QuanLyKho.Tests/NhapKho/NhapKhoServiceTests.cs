using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Tests.HangHoa;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Chỉ chạy khi có biến môi trường QLK_TEST_DB (chuỗi kết nối tới SQL Server đã migrate và seed); không có thì bỏ qua.</summary>
public sealed class CanSqlServerFactAttribute : FactAttribute
{
    public const string BienMoiTruong = "QLK_TEST_DB";

    public CanSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BienMoiTruong)))
            Skip = "Đặt biến môi trường QLK_TEST_DB là chuỗi kết nối tới CSDL đã chạy migration và seed để chạy bài kiểm thử này.";
    }
}

/// <summary>Kiểm thử việc T1-8 (hàm cho nhóm khác gọi: phiếu Thừa và phiếu nháp G3).</summary>
public class NhapKhoServiceTests
{
    private const int MaPKThu = 987654;

    // ---- Không cần SQL Server ----

    [Fact]
    public void KhongCoDong_BiTuChoi() => Assert.Equal("Phiếu phải có ít nhất một dòng hàng.", NhapKhoService.KiemTraDong(Array.Empty<DongPhieu>(), false));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void SoLuongKhongDuong_BiTuChoi(int soLuong) =>
        Assert.Equal("Số lượng mỗi dòng phải lớn hơn 0.", NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, soLuong, 1, 1) }, true));

    [Fact]
    public void ChuaChonMatHang_BiTuChoi() =>
        Assert.Equal("Chọn mặt hàng cho mỗi dòng.", NhapKhoService.KiemTraDong(new[] { new DongPhieu(0, 5) }, false));

    [Fact]
    public void PhieuThua_MoiDongPhaiCoLoVaO()
    {
        Assert.NotNull(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5) }, true));
        Assert.NotNull(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, MaLo: 1) }, true));
        Assert.NotNull(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, MaViTri: 1) }, true));
        Assert.Null(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, 1, 1) }, true));
    }

    [Fact]
    public void PhieuNhap_KhongCanLoVaO()
    {
        Assert.Null(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5) }, false));
        Assert.Null(NhapKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, 1, 1) }, false));   // có thì bỏ qua
    }

    [Fact]
    public void HangSoTrangThaiVaLoaiPhieu_KhopBangMienGiaTri()
    {
        Assert.Equal("Thừa do kiểm kê", PhieuNhap.LoaiThua);
        Assert.Equal("Hoàn tất", PhieuNhap.HoanTat);
    }

    [Fact]
    public void CauHinhBang_CoMaPKVaRangBuocThuaCoPhieuKiem()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var phieu = db.Model.FindEntityType(typeof(PhieuNhap))!;

        Assert.True(phieu.FindProperty("MaPK")!.IsNullable);
        Assert.Contains(phieu.GetIndexes(), i => i.Properties.Any(c => c.Name == "MaPK"));
        Assert.Contains(phieu.GetCheckConstraints(), c => c.Name == "CK_PhieuNhap_ThuaCoPhieuKiem");
    }

    [Fact]
    public void KhongCoCsdl_MoiTruongKhongXacDinh_DungBanGia()
    {
        using var sp = new ServiceCollection().AddContractStubs().AddNhapKho().BuildServiceProvider();
        using var scope = sp.CreateScope();
        Assert.IsType<NhapKhoServiceStub>(scope.ServiceProvider.GetRequiredService<INhapKhoService>());
    }

    // ---- Cần SQL Server đã seed: mỗi bài chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu ----

    private sealed record MoiTruong(KhoDbContext Db, NhapKhoService Nhap, ViTri O, LoHang Lo);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc
        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru);
        var lo = await db.LoHang.AsNoTracking().FirstAsync();
        var moiTruong = new MoiTruong(db, new NhapKhoService(db, new StockService(db), new NhatKyGia()), o, lo);
        await chay(moiTruong);
    }

    private static async Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon).FirstOrDefaultAsync();

    [CanSqlServerFact]
    public async Task PhieuThua_HoanTatVaTangDungTon() => await DungAsync(async e =>
    {
        const int nguoi = TonKhoSeeder.MaNguoiGhiTonMau;
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);
        var dong = new[] { new DongPhieu(e.Lo.MaBT, 3, e.Lo.MaLo, e.O.MaViTri), new DongPhieu(e.Lo.MaBT, 2, e.Lo.MaLo, e.O.MaViTri) };

        var kq = await e.Nhap.TaoPhieuNhapThua(e.O.MaKho, MaPKThu, dong, nguoi);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = await e.Db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).SingleAsync(x => x.MaPN == kq.GiaTri!.MaPhieu);
        Assert.Equal(PhieuNhap.HoanTat, p.TrangThai);
        Assert.Equal(PhieuNhap.LoaiThua, p.LoaiPhieu);
        Assert.Equal(MaPKThu, p.MaPK);
        Assert.Null(p.MaNCC);
        Assert.Equal(kq.GiaTri!.SoPhieu, p.SoPhieu);
        Assert.StartsWith("NK", p.SoPhieu);
        Assert.Equal(2, p.ChiTiet.Count);
        Assert.Equal(truoc + 5, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));

        var lai = await e.Nhap.TaoPhieuNhapThua(e.O.MaKho, MaPKThu, dong, nguoi);   // một phiếu kiểm chỉ có một phiếu Thừa
        Assert.False(lai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, lai.MaLoi);
        Assert.Equal(truoc + 5, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });

    [CanSqlServerFact]
    public async Task PhieuThua_LoSaiBienTheHoacOKhacKho_BiTuChoiVaKhongDoiTon() => await DungAsync(async e =>
    {
        const int nguoi = TonKhoSeeder.MaNguoiGhiTonMau;
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);
        var khoKhac = await e.Db.Kho.Where(k => k.MaKho != e.O.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong).Select(k => k.MaKho).FirstAsync();

        var saiKho = await e.Nhap.TaoPhieuNhapThua(khoKhac, MaPKThu, new[] { new DongPhieu(e.Lo.MaBT, 3, e.Lo.MaLo, e.O.MaViTri) }, nguoi);
        var bienTheKhac = await e.Db.BienThe.Where(b => b.MaBT != e.Lo.MaBT).Select(b => b.MaBT).FirstAsync();
        var saiBienThe = await e.Nhap.TaoPhieuNhapThua(e.O.MaKho, MaPKThu, new[] { new DongPhieu(bienTheKhac, 3, e.Lo.MaLo, e.O.MaViTri) }, nguoi);

        Assert.False(saiKho.ThanhCong);
        Assert.False(saiBienThe.ThanhCong);
        Assert.False(await e.Db.PhieuNhap.AnyAsync(x => x.MaPK == MaPKThu));
        Assert.Equal(truoc, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });

    [CanSqlServerFact]
    public async Task PhieuNhapNhap_LaNhapChuaCoNhaCungCapVaKhongDoiTon() => await DungAsync(async e =>
    {
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);

        var kq = await e.Nhap.TaoPhieuNhapNhap(e.O.MaKho, new[] { new DongPhieu(e.Lo.MaBT, 4) }, TonKhoSeeder.MaNguoiGhiTonMau, maGoiY: 77);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = await e.Db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).SingleAsync(x => x.MaPN == kq.GiaTri!.MaPhieu);
        Assert.Equal(PhieuNhap.Nhap, p.TrangThai);
        Assert.Equal("Mua hàng", p.LoaiPhieu);
        Assert.Null(p.MaNCC);
        Assert.Null(p.MaPK);
        Assert.Equal(4, p.ChiTiet.Single().SLDuKien);
        Assert.Contains("77", p.DienGiai);
        Assert.Equal(truoc, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });
}
