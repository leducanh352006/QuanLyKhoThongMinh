using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Tests.HangHoa;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.XuatKho;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T2-8 (danh sách, hủy phiếu, hàm phiếu Thiếu do kiểm kê).</summary>
public class XuatKhoServiceTests
{
    private const int MaPKThu = 876543;

    // ---- Không cần SQL Server ----

    [Fact]
    public void KhongCoDong_BiTuChoi() => Assert.Equal("Phiếu phải có ít nhất một dòng hàng.", XuatKhoService.KiemTraDong(Array.Empty<DongPhieu>()));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void SoLuongKhongDuong_BiTuChoi(int soLuong) =>
        Assert.Equal("Số lượng mỗi dòng phải lớn hơn 0.", XuatKhoService.KiemTraDong(new[] { new DongPhieu(1, soLuong, 1, 1) }));

    [Fact]
    public void ChuaChonMatHang_BiTuChoi() =>
        Assert.Equal("Chọn mặt hàng cho mỗi dòng.", XuatKhoService.KiemTraDong(new[] { new DongPhieu(0, 5, 1, 1) }));

    [Fact]
    public void MoiDongPhaiCoLoVaO()
    {
        Assert.NotNull(XuatKhoService.KiemTraDong(new[] { new DongPhieu(1, 5) }));
        Assert.NotNull(XuatKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, MaLo: 1) }));
        Assert.NotNull(XuatKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, MaViTri: 1) }));
        Assert.Null(XuatKhoService.KiemTraDong(new[] { new DongPhieu(1, 5, 1, 1) }));
    }

    [Fact]
    public void CauHinhBang_CoMaPKVaRangBuocThieuCoPhieuKiem()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var phieu = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PhieuXuat))!;

        Assert.True(phieu.FindProperty("MaPK")!.IsNullable);
        Assert.Contains(phieu.GetIndexes(), i => i.Properties.Any(c => c.Name == "MaPK"));
        Assert.Contains(phieu.GetCheckConstraints(), c => c.Name == "CK_PhieuXuat_ThieuCoPhieuKiem");
    }

    [Fact]
    public void KhongCoCsdl_MoiTruongKhongXacDinh_DungBanGia()
    {
        using var sp = new ServiceCollection().AddContractStubs().AddXuatKho().BuildServiceProvider();
        using var scope = sp.CreateScope();
        Assert.IsType<XuatKhoServiceStub>(scope.ServiceProvider.GetRequiredService<IXuatKhoService>());
    }

    // ---- Cần SQL Server đã seed: mỗi bài chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu ----

    private sealed record MoiTruong(KhoDbContext Db, XuatKhoService Xuat, ViTri O, LoHang Lo);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc
        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru);
        var lo = await db.LoHang.AsNoTracking().FirstAsync();
        var moiTruong = new MoiTruong(db, new XuatKhoService(db, new StockService(db), new NhatKyGia()), o, lo);
        await chay(moiTruong);
    }

    private static async Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon).FirstOrDefaultAsync();

    [CanSqlServerFact]
    public async Task PhieuThieu_HoanTatVaGiamDungTon() => await DungAsync(async e =>
    {
        const int nguoi = TonKhoSeeder.MaNguoiGhiTonMau;
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);
        var dong = new[] { new DongPhieu(e.Lo.MaBT, 2, e.Lo.MaLo, e.O.MaViTri), new DongPhieu(e.Lo.MaBT, 1, e.Lo.MaLo, e.O.MaViTri) };

        var kq = await e.Xuat.TaoPhieuXuatThieu(e.O.MaKho, MaPKThu, dong, nguoi);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = await e.Db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).SingleAsync(x => x.MaPX == kq.GiaTri!.MaPhieu);
        Assert.Equal(PhieuXuat.HoanTat, p.TrangThai);
        Assert.Equal(PhieuXuat.ThieuDoKiemKe, p.LoaiPhieu);
        Assert.Equal(MaPKThu, p.MaPK);
        Assert.Equal(nguoi, p.MaNguoiDuyet);
        Assert.Equal(kq.GiaTri!.SoPhieu, p.SoPhieu);
        Assert.StartsWith("XK", p.SoPhieu);
        Assert.Equal(2, p.ChiTiet.Count);
        Assert.Equal(truoc - 3, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));

        var lai = await e.Xuat.TaoPhieuXuatThieu(e.O.MaKho, MaPKThu, dong, nguoi);   // một phiếu kiểm chỉ có một phiếu Thiếu
        Assert.False(lai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, lai.MaLoi);
        Assert.Equal(truoc - 3, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });

    [CanSqlServerFact]
    public async Task PhieuThieu_VuotTonKhaDung_BiTuChoiVaKhongDoiTon() => await DungAsync(async e =>
    {
        const int nguoi = TonKhoSeeder.MaNguoiGhiTonMau;
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);

        var kq = await e.Xuat.TaoPhieuXuatThieu(e.O.MaKho, MaPKThu, new[] { new DongPhieu(e.Lo.MaBT, truoc + 1000, e.Lo.MaLo, e.O.MaViTri) }, nguoi);

        // Lỗi thiếu tồn chỉ lộ ra trong IStockService.GhiGiam, sau khi phiếu đã được ghi để lấy MaPX (như T1-8); vì DungAsync tự mở sẵn
        // giao dịch nên TrongGiaoDich không tự hoàn tác dòng phiếu đó (trách nhiệm hoàn tác thuộc về nơi gọi, ở đây là DungAsync khi kết thúc
        // bài kiểm thử). Chỉ khẳng định tồn không đổi — bất biến quan trọng mà GhiGiam phải giữ.
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotTon, kq.MaLoi);
        Assert.Equal(truoc, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });

    [CanSqlServerFact]
    public async Task PhieuThieu_LoSaiBienTheHoacOKhacKho_BiTuChoiVaKhongDoiTon() => await DungAsync(async e =>
    {
        const int nguoi = TonKhoSeeder.MaNguoiGhiTonMau;
        var truoc = await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri);
        var khoKhac = await e.Db.Kho.Where(k => k.MaKho != e.O.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong).Select(k => k.MaKho).FirstAsync();

        var saiKho = await e.Xuat.TaoPhieuXuatThieu(khoKhac, MaPKThu, new[] { new DongPhieu(e.Lo.MaBT, 1, e.Lo.MaLo, e.O.MaViTri) }, nguoi);
        var bienTheKhac = await e.Db.BienThe.Where(b => b.MaBT != e.Lo.MaBT).Select(b => b.MaBT).FirstAsync();
        var saiBienThe = await e.Xuat.TaoPhieuXuatThieu(e.O.MaKho, MaPKThu, new[] { new DongPhieu(bienTheKhac, 1, e.Lo.MaLo, e.O.MaViTri) }, nguoi);

        Assert.False(saiKho.ThanhCong);
        Assert.False(saiBienThe.ThanhCong);
        Assert.False(await e.Db.PhieuXuat.AnyAsync(x => x.MaPK == MaPKThu));
        Assert.Equal(truoc, await TonAsync(e.Db, e.Lo.MaLo, e.O.MaViTri));
    });
}
