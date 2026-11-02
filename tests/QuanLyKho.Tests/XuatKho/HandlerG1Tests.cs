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
/// Kiểm thử việc T2-2 (gợi ý G1 "lấy hàng từ lô, ô nào", issue #22) qua khung gợi ý AI dùng chung (T4-8): HandlerG1 chạy thật
/// trong một GoiYService thật (không giả) để kiểm đúng cả quy tắc của khung (một người thắng, hoàn tác khi handler lỗi,
/// người lập không tự duyệt) lẫn việc giữ chỗ/khóa phiếu của HandlerG1.
/// </summary>
public class HandlerG1Tests
{
    // Dữ liệu mẫu (SeedDataSet/TonKhoSeeder): biến thể MaBT=3 có hai lô ở kho 1 — lô 3 hạn 2027-03-31 tại ô 3 (còn 24),
    // lô 4 hạn 2027-09-30 tại ô 4 (còn 36). Lô 3 hết hạn sớm hơn nên gợi ý đúng phải ưu tiên lấy từ lô 3 trước.
    // Ô 7 là ô Tiếp nhận của kho 1; ô 10 thuộc kho 2 (khác kho xuất); lô 2 (ô 4, kho 1) thuộc biến thể MaBT=2, không phải MaBT=3.
    private const int MaKho = 1, MaBT = 3, MaLoSomHet = 3, MaOSomHet = 3, MaLoHetSau = 4, MaOHetSau = 4;
    private const int MaOTiepNhan = 7, MaOKhoKhac = 10, MaLoSaiBienThe = 2, MaOCuaLoSaiBienThe = 4;
    private const int MaThuKho = 3, MaQuanLyKho = 2;

    private sealed record MoiTruong(KhoDbContext Db, GoiYService GoiY, int MaPX, int MaCTX);

    private static async Task DungAsync(int soLuongDong, Func<MoiTruong, Task> chay, int maNguoiLap = MaThuKho)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var p = new PhieuXuat
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = maNguoiLap,
            ChiTiet = { new ChiTietPhieuXuat { MaBT = MaBT, SoLuong = soLuongDong } },
        };
        db.PhieuXuat.Add(p);
        await db.SaveChangesAsync();

        var handler = new HandlerG1(new StockService(db), TimeProvider.System, db);
        var goiY = new GoiYService(new EfGoiYStore(db), new[] { (IGoiYHandler)handler }, new EfHeThongStore(db),
            new AuditService(new EfNhatKyStore(db), TimeProvider.System), TimeProvider.System);
        await chay(new MoiTruong(db, goiY, p.MaPX, p.ChiTiet[0].MaCTX));
    }

    private static async Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon - t.SoLuongGiu).FirstOrDefaultAsync();

    [CanSqlServerFact]
    public async Task Duyet_GoiYHopLeNhieuLo_GiuChoDungTheoLoHetHanSomHonVaKhoaPhieu() => await DungAsync(30, async e =>
    {
        var truocSom = await TonAsync(e.Db, MaLoSomHet, MaOSomHet);
        var truocSau = await TonAsync(e.Db, MaLoHetSau, MaOHetSau);

        // Gợi ý đúng (như PhanBoFifo sẽ tính): ưu tiên hết lô hết hạn sớm (24, chỉ còn đúng 24) rồi mới sang lô hết hạn sau (6).
        var chiTiet = new[]
        {
            new ChiTietGoiYDto(MaBT, 24, "Hết hạn 2027-03-31, ưu tiên lấy trước.", MaLoSomHet, MaOSomHet),
            new ChiTietGoiYDto(MaBT, 6, "Lô hết hạn sớm không còn đủ, lấy tiếp lô hết hạn 2027-09-30.", MaLoHetSau, MaOHetSau),
        };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);
        Assert.True(duyet.ThanhCong, duyet.ThongBao);

        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaDuyet, p.TrangThai);
        Assert.Equal(MaQuanLyKho, p.MaNguoiDuyet);

        var layHang = await e.Db.LayHang.AsNoTracking().Where(l => l.MaCTX == e.MaCTX).OrderBy(l => l.MaLo).ToListAsync();
        Assert.Equal(2, layHang.Count);
        Assert.Equal((MaLoSomHet, MaOSomHet, 24), (layHang[0].MaLo, layHang[0].MaViTri, layHang[0].SLGiu));
        Assert.Equal((MaLoHetSau, MaOHetSau, 6), (layHang[1].MaLo, layHang[1].MaViTri, layHang[1].SLGiu));

        Assert.Equal(truocSom - 24, await TonAsync(e.Db, MaLoSomHet, MaOSomHet));
        Assert.Equal(truocSau - 6, await TonAsync(e.Db, MaLoHetSau, MaOHetSau));

        var g = await e.GoiY.Lay(tao.GiaTri);
        Assert.Equal(TrangThaiGoiY.DaDuyet, g!.TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_GoiYVuotTonKhaDung_BiTuChoiKhongDoiTonVaGoiYVanChoDuyet() => await DungAsync(24, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLoSomHet, MaOSomHet);

        // Vi phạm ràng buộc: đề xuất lấy nhiều hơn tồn khả dụng hiện có của lô (IStockService.GiuCho phải tự chặn).
        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, truoc + 1000, "Vượt tồn khả dụng.", MaLoSomHet, MaOSomHet) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLoSomHet, MaOSomHet));
        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.Nhap, p.TrangThai);
        var g = await e.GoiY.Lay(tao.GiaTri);
        Assert.Equal(TrangThaiGoiY.ChoDuyet, g!.TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_GoiYKhongKhopDongPhieu_BiTuChoiVaKhongGiuCho() => await DungAsync(24, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLoSomHet, MaOSomHet);

        // Gợi ý ghi biến thể có thật (MaBT=2, khác MaBT=3 của dòng phiếu) nhưng không khớp dòng nào của phiếu
        // (ví dụ phiếu đã bị sửa sau khi tạo gợi ý).
        var chiTiet = new[] { new ChiTietGoiYDto(2, 24, "Biến thể không khớp phiếu.", 2, 4) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLoSomHet, MaOSomHet));
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_NguoiLapTuDuyet_BiChan() => await DungAsync(24, async e =>
    {
        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, 24, "Hợp lệ.", MaLoSomHet, MaOSomHet) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        // Phiếu do chính Quản lý kho lập (maNguoiLap: MaQuanLyKho) nên không được tự duyệt (QuyTacDuyet), dù có đủ thẩm quyền vai trò.
        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    }, maNguoiLap: MaQuanLyKho);

    // ---- HandlerG1 không tin hoàn toàn vào dòng gợi ý (kể cả dòng do người duyệt "sửa rồi duyệt"): kiểm lại ràng buộc thật
    // trước khi giữ chỗ, vì IStockService.GiuCho không còn chặn ô Tiếp nhận/ngừng hoạt động và không kiểm hạn dùng, biến thể, kho. ----

    [CanSqlServerFact]
    public async Task Duyet_DongTroToOTiepNhan_BiTuChoiKhongGiuCho() => await DungAsync(10, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLoSomHet, MaOTiepNhan);

        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, 10, "Trỏ sai sang ô Tiếp nhận.", MaLoSomHet, MaOTiepNhan) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLoSomHet, MaOTiepNhan));
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_DongTroToOKhoKhac_BiTuChoiKhongGiuCho() => await DungAsync(10, async e =>
    {
        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, 10, "Trỏ sai sang ô của kho khác.", MaLoSomHet, MaOKhoKhac) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_DongTroToLoSaiBienThe_BiTuChoiKhongGiuCho() => await DungAsync(10, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLoSaiBienThe, MaOCuaLoSaiBienThe);

        // Gợi ý ghi MaBT đúng của dòng phiếu (để qua được KhopVoiDongPhieu) nhưng trỏ MaLo thực ra thuộc biến thể khác.
        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, 10, "Lô trỏ sai, thực ra thuộc biến thể khác.", MaLoSaiBienThe, MaOCuaLoSaiBienThe) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLoSaiBienThe, MaOCuaLoSaiBienThe));
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_DongTroToLoDaHetHan_BiTuChoiKhongGiuCho() => await DungAsync(10, async e =>
    {
        // Lô hết hạn hôm qua, tạo riêng cho bài này (dữ liệu mẫu chưa có lô hết hạn), còn tồn tại ô hợp lệ của phiếu.
        var loHetHan = new LoHang
        {
            MaVachLo = "LO-HETHAN-" + Guid.NewGuid().ToString("N")[..10], MaBT = MaBT, HanDung = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
        };
        e.Db.LoHang.Add(loHetHan);
        await e.Db.SaveChangesAsync();
        e.Db.TonKho.Add(new TonKho { MaLo = loHetHan.MaLo, MaViTri = MaOSomHet, SoLuongTon = 50, SoLuongGiu = 0 });
        await e.Db.SaveChangesAsync();

        var chiTiet = new[] { new ChiTietGoiYDto(MaBT, 10, "Lô đã hết hạn nhưng gợi ý cũ chưa biết.", loHetHan.MaLo, MaOSomHet) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G1, new ThamChieuGoiY(MaPX: e.MaPX), chiTiet);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaQuanLyKho);

        Assert.False(duyet.ThanhCong);
        Assert.Equal(PhieuXuat.Nhap, (await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX)).TrangThai);
    });
}
