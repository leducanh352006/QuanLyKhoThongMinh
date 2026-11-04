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
/// Kiểm thử việc T2-5 (issue #16, "Đối chiếu hàng đã lấy"): DoiChieuService.DoiChieu — khớp đủ thì chuyển Đã đối chiếu; lệch mà
/// còn tồn khả dụng thì gợi ý lại đúng phần thiếu (giữ chỗ ngay, không qua duyệt) và quay lại Đang lấy hàng; hết tồn thì xuất một
/// phần (chốt số lượng dòng bằng đúng số đã lấy); hết tồn mà chưa lấy được gì thì từ chối rõ ràng (không xuất được dòng 0).
/// Mỗi bài tự dựng biến thể, lô, ô, tồn riêng trong giao dịch của mình (không commit) để kiểm soát chính xác tồn khả dụng.
/// </summary>
public class DoiChieuServiceTests
{
    private const int MaKho = 1, MaNguoiLap = 3, MaNguoiDoiChieu = 2;

    private sealed record MoiTruong(KhoDbContext Db, DoiChieuService Service, IStockService Ton, int MaPX, int MaCTX, int MaBT, int MaDay);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var day = new ViTri { TenViTri = "Dãy test ĐC " + Guid.NewGuid().ToString("N")[..8], MaKho = MaKho, CapViTri = CapViTriGiaTri.Day };
        db.ViTri.Add(day);
        await db.SaveChangesAsync();

        var maBT = await TaoBienTheAsync(db);
        var p = new PhieuXuat
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = MaNguoiLap,
            TrangThai = PhieuXuat.DangLayHang, MaNguoiDuyet = MaNguoiDoiChieu,
            ChiTiet = { new ChiTietPhieuXuat { MaBT = maBT, SoLuong = 10 } },
        };
        db.PhieuXuat.Add(p);
        await db.SaveChangesAsync();

        var ton = new StockService(db);
        var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
        var goiYG1 = new GoiYG1Service(TimeProvider.System, db);
        var service = new DoiChieuService(db, ton, audit, goiYG1);

        await chay(new MoiTruong(db, service, ton, p.MaPX, p.ChiTiet[0].MaCTX, maBT, day.MaViTri));
    }

    private static ViTri OLuuTru(int maDay, string ten) => new()
    {
        TenViTri = ten, MaKho = MaKho, MaViTriCha = maDay, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
        MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 10, Rong = 10, Cao = 10, KhoiLuongToiDa = 100,
    };

    private static async Task<int> TaoBienTheAsync(KhoDbContext db)
    {
        var maDM = await db.DanhMuc.Select(d => d.MaDM).FirstAsync();
        var sp = new SanPham { TenSP = "SP test ĐC " + Guid.NewGuid().ToString("N")[..8], MaDM = maDM, DonViTinh = "cái" };
        db.SanPham.Add(sp);
        await db.SaveChangesAsync();
        var bt = new BienThe { MaSP = sp.MaSP, MaVach = "BT" + Guid.NewGuid().ToString("N")[..12], ThuocTinh = "Test", Dai = 1, Rong = 1, Cao = 1, KhoiLuong = 1 };
        db.BienThe.Add(bt);
        await db.SaveChangesAsync();
        return bt.MaBT;
    }

    private static async Task<int> LoHangAsync(KhoDbContext db, int maBT, DateOnly? hanDung = null)
    {
        var lo = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = maBT, HanDung = hanDung };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();
        return lo.MaLo;
    }

    private static async Task TonAsync(KhoDbContext db, int maLo, int maViTri, int soLuongTon, int soLuongGiu)
    {
        db.TonKho.Add(new TonKho { MaLo = maLo, MaViTri = maViTri, SoLuongTon = soLuongTon, SoLuongGiu = soLuongGiu });
        await db.SaveChangesAsync();
    }

    private static async Task<int> KhaDungAsync(KhoDbContext db, int maLo, int maViTri) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon - t.SoLuongGiu).FirstAsync();

    [CanSqlServerFact]
    public async Task DoiChieu_KhopDu_ChuyenDaDoiChieu() => await DungAsync(async e =>
    {
        var o = OLuuTru(e.MaDay, "O-Du");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 10, soLuongGiu: 10);
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 10, SLThucLay = 10 });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.DoiChieu(e.MaPX, MaNguoiDoiChieu);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.True(kq.GiaTri);
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaDoiChieu, p.TrangThai);
        Assert.Equal(0, await e.Db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == o.MaViTri).Select(t => t.SoLuongTon - t.SoLuongGiu).FirstAsync());   // khớp đủ nên không trả lại gì, vẫn giữ hết 10
    });

    [CanSqlServerFact]
    public async Task DoiChieu_LechConLoKhac_GoiYLaiGiuChoMoiQuayLaiDangLayHang() => await DungAsync(async e =>
    {
        var oA = OLuuTru(e.MaDay, "O-A");
        var oB = OLuuTru(e.MaDay, "O-B");
        e.Db.ViTri.AddRange(oA, oB);
        await e.Db.SaveChangesAsync();

        var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
        var loA = await LoHangAsync(e.Db, e.MaBT, hanDung: homNay.AddDays(30));   // hạn xa hơn — FEFO không ưu tiên
        var loB = await LoHangAsync(e.Db, e.MaBT, hanDung: homNay.AddDays(5));    // hạn gần hơn — FEFO ưu tiên gợi ý lại ở đây
        await TonAsync(e.Db, loA, oA.MaViTri, soLuongTon: 10, soLuongGiu: 10);   // đã giữ 10, chỉ lấy được 6 (thiếu 4)
        await TonAsync(e.Db, loB, oB.MaViTri, soLuongTon: 20, soLuongGiu: 0);    // còn tồn để gợi ý lại
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = loA, MaViTri = oA.MaViTri, SLGiu = 10, SLThucLay = 6, LyDoLech = "Kệ chỉ còn 6." });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.DoiChieu(e.MaPX, MaNguoiDoiChieu);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.False(kq.GiaTri);   // còn dòng vừa gợi ý lại, chưa đối chiếu xong
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DangLayHang, p.TrangThai);   // chưa chuyển Đã đối chiếu
        Assert.Equal(10, (await e.Db.ChiTietPhieuXuat.AsNoTracking().SingleAsync(c => c.MaCTX == e.MaCTX)).SoLuong);   // yêu cầu không đổi

        // Lô A: trả lại đúng phần dư (10 - 6 = 4) nên khả dụng trở lại 4.
        Assert.Equal(4, await KhaDungAsync(e.Db, loA, oA.MaViTri));
        // Lô B: gợi ý lại giữ đúng 4 còn thiếu.
        Assert.Equal(16, await KhaDungAsync(e.Db, loB, oB.MaViTri));
        var moiLayHang = await e.Db.LayHang.AsNoTracking().SingleAsync(l => l.MaLo == loB && l.MaViTri == oB.MaViTri);
        Assert.Equal(4, moiLayHang.SLGiu);
        Assert.Null(moiLayHang.SLThucLay);
    });

    [CanSqlServerFact]
    public async Task DoiChieu_LechHetTon_XuatMotPhan() => await DungAsync(async e =>
    {
        var o = OLuuTru(e.MaDay, "O-HetTon");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 10, soLuongGiu: 10);   // chỉ một lô duy nhất, lấy được 6/10
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 10, SLThucLay = 6, LyDoLech = "Hết hàng ở ô." });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.DoiChieu(e.MaPX, MaNguoiDoiChieu);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.True(kq.GiaTri);   // không gợi ý lại được (hết tồn) nhưng đã lấy > 0 nên xuất một phần, xong đối chiếu
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaDoiChieu, p.TrangThai);
        var dong = await e.Db.ChiTietPhieuXuat.AsNoTracking().SingleAsync(c => c.MaCTX == e.MaCTX);
        Assert.Equal(6, dong.SoLuong);   // chốt yêu cầu bằng đúng số đã lấy

        // Giữ chỗ còn lại đúng bằng số đã lấy (phần dư đã được trả lại).
        var ton = await e.Db.TonKho.AsNoTracking().SingleAsync(t => t.MaLo == maLo && t.MaViTri == o.MaViTri);
        Assert.Equal(6, ton.SoLuongGiu);
    });

    [CanSqlServerFact]
    public async Task DoiChieu_HetTonVaChuaLayDuocGi_BiTuChoiKhongXuatDongRong() => await DungAsync(async e =>
    {
        var o = OLuuTru(e.MaDay, "O-KhongLayDuoc");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 10, soLuongGiu: 10);
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 10, SLThucLay = 0, LyDoLech = "Không còn hàng." });
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.DoiChieu(e.MaPX, MaNguoiDoiChieu);

        Assert.False(kq.ThanhCong);
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DangLayHang, p.TrangThai);   // không đổi gì, vẫn Đang lấy hàng
        var dong = await e.Db.ChiTietPhieuXuat.AsNoTracking().SingleAsync(c => c.MaCTX == e.MaCTX);
        Assert.Equal(10, dong.SoLuong);   // yêu cầu không bị đổi (không xuất được dòng 0)
    });

    [CanSqlServerFact]
    public async Task DoiChieu_ConDongChuaLayHang_BiTuChoi() => await DungAsync(async e =>
    {
        var o = OLuuTru(e.MaDay, "O-ChuaQuet");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, e.MaBT);
        await TonAsync(e.Db, maLo, o.MaViTri, soLuongTon: 10, soLuongGiu: 10);
        e.Db.LayHang.Add(new LayHang { MaCTX = e.MaCTX, MaLo = maLo, MaViTri = o.MaViTri, SLGiu = 10, SLThucLay = null });   // chưa quét
        await e.Db.SaveChangesAsync();

        var kq = await e.Service.DoiChieu(e.MaPX, MaNguoiDoiChieu);

        Assert.False(kq.ThanhCong);
        Assert.Equal(PhieuXuat.DangLayHang, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });
}
