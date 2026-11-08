using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T2-6 (issue #15, "Xác nhận xuất"): XacNhanXuatService.XacNhan — ghi giảm tồn đúng theo số đã lấy (LayHang.
/// SLThucLay) qua IStockService.GhiGiam(tuGiuCho: true) rồi chuyển Hoàn tất; bấm xác nhận lần hai (phiếu đã Hoàn tất) bị từ chối
/// ngay, không ghi giảm thêm lần nữa; phiếu chưa Đã đối chiếu thì từ chối. Mỗi bài tự dựng biến thể, lô, ô, tồn riêng trong giao
/// dịch của mình (không commit) để kiểm soát chính xác tồn.
/// </summary>
public class XacNhanXuatServiceTests
{
    private const int MaKho = 1, MaNguoiLap = 3, MaNguoiXacNhan = 2;

    private sealed record MoiTruong(KhoDbContext Db, XacNhanXuatService Service, int MaPX, int MaCTX, int MaBT, int MaDay);

    private static async Task DungAsync(string trangThaiBanDau, Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var day = new ViTri { TenViTri = "Dãy test XNX " + Guid.NewGuid().ToString("N")[..8], MaKho = MaKho, CapViTri = CapViTriGiaTri.Day };
        db.ViTri.Add(day);
        await db.SaveChangesAsync();

        var maDM = await db.DanhMuc.Select(d => d.MaDM).FirstAsync();
        var sp = new SanPham { TenSP = "SP test XNX " + Guid.NewGuid().ToString("N")[..8], MaDM = maDM, DonViTinh = "cái" };
        db.SanPham.Add(sp);
        await db.SaveChangesAsync();
        var bt = new BienThe { MaSP = sp.MaSP, MaVach = "BT" + Guid.NewGuid().ToString("N")[..12], ThuocTinh = "Test", Dai = 1, Rong = 1, Cao = 1, KhoiLuong = 1 };
        db.BienThe.Add(bt);
        await db.SaveChangesAsync();

        var p = new PhieuXuat
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = MaNguoiLap,
            TrangThai = trangThaiBanDau, MaNguoiDuyet = MaNguoiXacNhan,
            ChiTiet = { new ChiTietPhieuXuat { MaBT = bt.MaBT, SoLuong = 7 } },
        };
        db.PhieuXuat.Add(p);
        await db.SaveChangesAsync();

        var ton = new StockService(db);
        var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
        var service = new XacNhanXuatService(db, ton, audit);

        await chay(new MoiTruong(db, service, p.MaPX, p.ChiTiet[0].MaCTX, bt.MaBT, day.MaViTri));
    }

    private static ViTri OLuuTru(int maDay, string ten) => new()
    {
        TenViTri = ten, MaKho = MaKho, MaViTriCha = maDay, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
        MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 10, Rong = 10, Cao = 10, KhoiLuongToiDa = 100,
    };

    private static async Task<int> LoHangAsync(KhoDbContext db, int maBT)
    {
        var lo = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = maBT };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();
        return lo.MaLo;
    }

    private static async Task TonAsync(KhoDbContext db, int maLo, int maViTri, int soLuongTon, int soLuongGiu)
    {
        db.TonKho.Add(new TonKho { MaLo = maLo, MaViTri = maViTri, SoLuongTon = soLuongTon, SoLuongGiu = soLuongGiu });
        await db.SaveChangesAsync();
    }

    [CanSqlServerFact]
    public async Task XacNhan_DaDoiChieu_GhiGiamTonVaHoanTat() => await DungAsync(PhieuXuat.DaDoiChieu, async e =>
    {
        var o = OLuuTru(e.MaDay, "O-XacNhan");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 20, soLuongGiu: 7);   // đã giữ đúng 7 từ khi duyệt (T2-3)
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 7, SLThucLay = 7 });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.XacNhan(e.MaPX, MaNguoiXacNhan);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.HoanTat, p.TrangThai);
        var ton = await e.Db.TonKho.AsNoTracking().SingleAsync(t => t.MaLo == maLo && t.MaViTri == o.MaViTri);
        Assert.Equal(13, ton.SoLuongTon);   // 20 - 7: ghi giảm tồn thật
        Assert.Equal(0, ton.SoLuongGiu);    // tuGiuCho: true nên giảm luôn phần giữ
        var soCai = await e.Db.BienDongTon.AsNoTracking().Where(b => b.MaLo == maLo && b.MaViTri == o.MaViTri).ToListAsync();
        var dong = Assert.Single(soCai);
        Assert.Equal(-7, dong.SoLuong);
    });

    [CanSqlServerFact]
    public async Task XacNhan_HaiDongHang_GhiGiamTungDongDung() => await DungAsync(PhieuXuat.DaDoiChieu, async e =>
    {
        var oA = OLuuTru(e.MaDay, "O-A");
        var oB = OLuuTru(e.MaDay, "O-B");
        e.Db.ViTri.AddRange(oA, oB);
        await e.Db.SaveChangesAsync();
        var loA = await LoHangAsync(e.Db, e.MaBT);
        var loB = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, loA, oA.MaViTri, soLuongTon: 10, soLuongGiu: 5);
        await TonAsync(e.Db, loB, oB.MaViTri, soLuongTon: 10, soLuongGiu: 2);
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = loA, MaViTri = oA.MaViTri, SLGiu = 5, SLThucLay = 5 });
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = loB, MaViTri = oB.MaViTri, SLGiu = 2, SLThucLay = 2 });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.XacNhan(e.MaPX, MaNguoiXacNhan);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var tonA = await e.Db.TonKho.AsNoTracking().SingleAsync(t => t.MaLo == loA && t.MaViTri == oA.MaViTri);
        var tonB = await e.Db.TonKho.AsNoTracking().SingleAsync(t => t.MaLo == loB && t.MaViTri == oB.MaViTri);
        Assert.Equal((5, 0), (tonA.SoLuongTon, tonA.SoLuongGiu));
        Assert.Equal((8, 0), (tonB.SoLuongTon, tonB.SoLuongGiu));
    });

    [CanSqlServerFact]
    public async Task XacNhan_GoiLaiLanHai_KhongGhiGiamThemLan2() => await DungAsync(PhieuXuat.DaDoiChieu, async e =>
    {
        var o = OLuuTru(e.MaDay, "O-GoiLaiLan2");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 20, soLuongGiu: 7);
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 7, SLThucLay = 7 });
        await e.Db.SaveChangesAsync();

        var kq1 = await e.Service.XacNhan(e.MaPX, MaNguoiXacNhan);
        Assert.True(kq1.ThanhCong, kq1.ThongBao);

        var kq2 = await e.Service.XacNhan(e.MaPX, MaNguoiXacNhan);   // bấm lại (phiếu đã Hoàn tất)

        Assert.False(kq2.ThanhCong);   // bị từ chối ngay, không đụng tới IStockService lần nữa
        var ton = await e.Db.TonKho.AsNoTracking().SingleAsync(t => t.MaLo == maLo && t.MaViTri == o.MaViTri);
        Assert.Equal(13, ton.SoLuongTon);   // vẫn chỉ giảm đúng một lần (20 - 7)
        Assert.Equal(0, ton.SoLuongGiu);
        var soCai = await e.Db.BienDongTon.AsNoTracking().Where(b => b.MaLo == maLo && b.MaViTri == o.MaViTri).ToListAsync();
        Assert.Single(soCai);   // sổ cái chỉ có một dòng, không ghi lặp
    });

    [CanSqlServerFact]
    public async Task XacNhan_ChuaDoiChieu_BiTuChoi() => await DungAsync(PhieuXuat.DangLayHang, async e =>
    {
        var kq = await e.Service.XacNhan(e.MaPX, MaNguoiXacNhan);

        Assert.False(kq.ThanhCong);
        Assert.Equal(PhieuXuat.DangLayHang, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });
}
