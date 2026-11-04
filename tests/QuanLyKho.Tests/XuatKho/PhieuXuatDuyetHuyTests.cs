using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.XuatKho.Controllers;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T2-3 (issue #21, "Duyệt phiếu và giữ chỗ"): gọi thẳng các action Duyet/Huy của PhieuXuatController (đường làm
/// tay trực tiếp, không qua gợi ý G1 — đường gợi ý G1 đã có HandlerG1Tests) để chứng minh đúng hai tiêu chí hoàn thành của issue:
/// "Duyệt thì SoLuongGiu tăng, hủy thì trả lại" và "Người lập không tự duyệt được". Không cần hạ tầng MVC đầy đủ (HttpContext,
/// Url...) vì các action chỉ dùng TempData (giả bằng Dictionary) và trả về IActionResult mà không thực thi nó.
/// </summary>
public class PhieuXuatDuyetHuyTests
{
    // Dữ liệu mẫu: biến thể MaBT=4, lô 5 (còn 60) tại ô 6 của kho 1. Dùng biến thể/lô/ô riêng (khác HandlerG1Tests dùng MaBT=3)
    // để các bài không tranh chấp cùng dòng TonKho khi chạy song song với lớp kiểm thử khác.
    private const int MaKho = 1, MaBT = 4, MaLo = 5, MaViTri = 6;
    private const int MaThuKho = 3, MaQuanLyKho = 2;

    /// <summary>TempData giả tối giản: action chỉ ghi/đọc, không cần ITempDataDictionaryFactory hay HttpContext thật.</summary>
    private sealed class GiaTempData : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public void Save() { }
        public object? Peek(string key) => TryGetValue(key, out var v) ? v : null;
    }

    private sealed class NguoiDungCoDinh(int maND) : INguoiDungHienTai
    {
        public NguoiDungHienTai? Lay() => new(maND, "u" + maND, "Người " + maND, 0, "");
    }

    private sealed record MoiTruong(KhoDbContext Db, PhieuXuatController Controller, int MaPX);

    private static async Task<int> TonAsync(KhoDbContext db, int maLo, int maViTri) =>
        await db.TonKho.AsNoTracking().Where(t => t.MaLo == maLo && t.MaViTri == maViTri).Select(t => t.SoLuongTon - t.SoLuongGiu).FirstOrDefaultAsync();

    // PhieuXuatController.Duyet/Huy tự mở giao dịch riêng của mình (không tham gia giao dịch có sẵn như các service khác), nên
    // không thể bọc cả bài trong một giao dịch ngoài rồi hoàn tác như các lớp kiểm thử khác — phải tự dọn dữ liệu bằng tay.
    private static async Task DungAsync(int soLuongDong, int maNguoiLap, int maNguoiDangDung, Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);

        var p = new PhieuXuat
        {
            SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = maNguoiLap,
            ChiTiet = { new ChiTietPhieuXuat { MaBT = MaBT, SoLuong = soLuongDong } },
        };
        db.PhieuXuat.Add(p);
        await db.SaveChangesAsync();
        var maCTX = p.ChiTiet[0].MaCTX;

        var ton = new StockService(db);
        try
        {
            var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
            var goiYG1 = new GoiYG1Service(TimeProvider.System, db);
            var goiY = new GoiYServiceStub(new GoiYStubStore(), Array.Empty<IGoiYHandler>());
            var controller = new PhieuXuatController(new NguoiDungCoDinh(maNguoiDangDung), audit, ton, goiY, goiYG1, db) { TempData = new GiaTempData() };

            await chay(new MoiTruong(db, controller, p.MaPX));
        }
        finally
        {
            // Trả lại mọi phần còn giữ chỗ (ví dụ bài Hủy không gọi đến, hoặc bài tự-duyệt-bị-chặn không giữ gì) rồi xóa sạch dữ liệu tạo riêng cho bài.
            var giu = await db.LayHang.Where(l => l.MaCTX == maCTX).ToListAsync();
            foreach (var g in giu) await ton.TraCho(new DongTon(g.MaLo, g.MaViTri, g.SLGiu));
            if (giu.Count > 0) { db.LayHang.RemoveRange(giu); await db.SaveChangesAsync(); }
            await db.ChiTietPhieuXuat.Where(c => c.MaPX == p.MaPX).ExecuteDeleteAsync();
            await db.PhieuXuat.Where(x => x.MaPX == p.MaPX).ExecuteDeleteAsync();
        }
    }

    [CanSqlServerFact]
    public async Task Duyet_HopLe_GiuChoDungTheoFefoVaKhoaPhieu() => await DungAsync(10, maNguoiLap: MaThuKho, maNguoiDangDung: MaQuanLyKho, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLo, MaViTri);

        await e.Controller.Duyet(e.MaPX);

        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaDuyet, p.TrangThai);
        Assert.Equal(MaQuanLyKho, p.MaNguoiDuyet);
        Assert.Equal(truoc - 10, await TonAsync(e.Db, MaLo, MaViTri));

        var layHang = await e.Db.LayHang.AsNoTracking().Where(l => l.MaLo == MaLo && l.MaViTri == MaViTri && l.SLGiu == 10).ToListAsync();
        Assert.Single(layHang);
    });

    [CanSqlServerFact]
    public async Task Huy_PhieuDaDuyet_TraLaiGiuChoVaXoaLayHang() => await DungAsync(10, maNguoiLap: MaThuKho, maNguoiDangDung: MaQuanLyKho, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLo, MaViTri);
        await e.Controller.Duyet(e.MaPX);
        Assert.Equal(truoc - 10, await TonAsync(e.Db, MaLo, MaViTri));   // đã giữ chỗ, kiểm trước khi hủy để chắc chắn phép trừ sau đó là trả lại

        await e.Controller.Huy(e.MaPX);

        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaHuy, p.TrangThai);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLo, MaViTri));   // trả lại đúng số đã giữ

        var conLayHang = await e.Db.LayHang.AsNoTracking().AnyAsync(l => l.MaLo == MaLo && l.MaViTri == MaViTri && l.SLGiu == 10);
        Assert.False(conLayHang);
    });

    [CanSqlServerFact]
    public async Task Huy_PhieuNhapChuaDuyet_KhongCoGiTraLaiVanKhoaDuocPhieu() => await DungAsync(10, maNguoiLap: MaThuKho, maNguoiDangDung: MaThuKho, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLo, MaViTri);

        await e.Controller.Huy(e.MaPX);

        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.DaHuy, p.TrangThai);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLo, MaViTri));   // Nháp chưa giữ chỗ gì nên tồn không đổi
    });

    [CanSqlServerFact]
    public async Task Duyet_NguoiLapTuDuyet_BiChanKhongDoiTrangThaiKhongGiuCho() => await DungAsync(10, maNguoiLap: MaQuanLyKho, maNguoiDangDung: MaQuanLyKho, async e =>
    {
        var truoc = await TonAsync(e.Db, MaLo, MaViTri);

        await e.Controller.Duyet(e.MaPX);

        var p = await e.Db.PhieuXuat.AsNoTracking().SingleAsync(x => x.MaPX == e.MaPX);
        Assert.Equal(PhieuXuat.Nhap, p.TrangThai);
        Assert.Null(p.MaNguoiDuyet);
        Assert.Equal(truoc, await TonAsync(e.Db, MaLo, MaViTri));
        Assert.False(await e.Db.LayHang.AsNoTracking().AnyAsync(l => l.MaLo == MaLo && l.MaViTri == MaViTri));
    });
}
