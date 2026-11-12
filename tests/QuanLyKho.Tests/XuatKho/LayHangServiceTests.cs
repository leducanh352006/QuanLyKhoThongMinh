using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T2-4 (issue #20, "Lấy hàng quét mã"): LayHangService.XacNhan — quét sai ô, lô hoặc mã vạch biến thể bị từ chối,
/// không ghi gì; lấy thiếu so với số đã giữ (SLGiu) phải kèm lý do lệch; đúng thì ghi SLThucLay/LyDoLech và lần đầu của phiếu
/// chuyển Đã duyệt sang Đang lấy hàng.
/// </summary>
public class LayHangServiceTests
{
    // Dữ liệu mẫu: biến thể MaBT=4 (Tai nghe Bluetooth, mã vạch 8930000000042), lô 5 (LO-2610-004) tại ô 6 (kho 1).
    private const int MaKho = 1, MaBT = 4, MaLo = 5, MaViTri = 6, SLGiu = 10;
    private const string MaVachBTDung = "8930000000042";
    private const int MaNguoiDuyet = 2;

    private sealed record MoiTruong(KhoDbContext Db, LayHangService Service, int MaPX, int MaLH, string MaVachO, string MaVachLo);

    private static async Task DungAsync(Func<MoiTruong, Task> chay, string trangThaiPhieu = PhieuXuat.DaDuyet)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var p = new PhieuXuat
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = 3,
            TrangThai = trangThaiPhieu, MaNguoiDuyet = trangThaiPhieu == PhieuXuat.Nhap ? null : MaNguoiDuyet,
            ChiTiet = { new ChiTietPhieuXuat { MaBT = MaBT, SoLuong = SLGiu } },
        };
        db.PhieuXuat.Add(p);
        await db.SaveChangesAsync();

        var layHang = new LayHang { MaCTX = p.ChiTiet[0].MaCTX, MaLo = MaLo, MaViTri = MaViTri, SLGiu = SLGiu };
        db.LayHang.Add(layHang);
        await db.SaveChangesAsync();

        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == MaViTri);
        var lo = await db.LoHang.AsNoTracking().FirstAsync(l => l.MaLo == MaLo);
        var service = new LayHangService(db, new AuditService(new EfNhatKyStore(db), TimeProvider.System));
        await chay(new MoiTruong(db, service, p.MaPX, layHang.MaLH, o.MaVachO!, lo.MaVachLo));
    }

    [CanSqlServerFact]
    public async Task XacNhan_DungCaBaMaVaDuSoLuong_GhiNhanVaChuyenDangLayHang() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu, null, MaNguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var l = await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH);
        Assert.Equal(SLGiu, l.SLThucLay);
        Assert.Null(l.LyDoLech);
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DangLayHang, p.TrangThai);
    });

    [CanSqlServerFact]
    public async Task XacNhan_SaiMaO_BiTuChoiKhongGhiGi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, "O-SAI", e.MaVachLo, MaVachBTDung, SLGiu, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        var l = await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH);
        Assert.Null(l.SLThucLay);
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaDuyet, p.TrangThai);
    });

    [CanSqlServerFact]
    public async Task XacNhan_SaiMaLo_BiTuChoiKhongGhiGi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, "LO-SAI", MaVachBTDung, SLGiu, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH)).SLThucLay);
    });

    [CanSqlServerFact]
    public async Task XacNhan_SaiMaVachBienThe_BiTuChoiKhongGhiGi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, "0000000000000", SLGiu, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH)).SLThucLay);
    });

    [CanSqlServerFact]
    public async Task XacNhan_LayThieuKhongGhiLyDo_BiTuChoiKhongGhiGi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu - 3, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH)).SLThucLay);
    });

    [CanSqlServerFact]
    public async Task XacNhan_LayThieuCoLyDo_GhiDungSLThucLayVaLyDoLech() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu - 3, "Kệ chỉ còn vậy.", MaNguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var l = await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH);
        Assert.Equal(SLGiu - 3, l.SLThucLay);
        Assert.Equal("Kệ chỉ còn vậy.", l.LyDoLech);
    });

    [CanSqlServerFact]
    public async Task XacNhan_VuotSoDaGiuCho_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu + 1, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH)).SLThucLay);
    });

    [CanSqlServerFact]
    public async Task XacNhan_DaXacNhanRoi_BiTuChoiKhongGhiLai() => await DungAsync(async e =>
    {
        var lan1 = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu, null, MaNguoiDuyet);
        Assert.True(lan1.ThanhCong, lan1.ThongBao);

        var lan2 = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu, null, MaNguoiDuyet);

        Assert.False(lan2.ThanhCong);
    });

    [CanSqlServerFact]
    public async Task XacNhan_PhieuChuaDuyet_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.XacNhan(e.MaLH, e.MaVachO, e.MaVachLo, MaVachBTDung, SLGiu, null, MaNguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.LayHang.AsNoTracking().SingleAsync(x => x.MaLH == e.MaLH)).SLThucLay);
    }, trangThaiPhieu: PhieuXuat.Nhap);
}
