using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T2-2 (gợi ý G1, issue #22) cho GoiYG1Service — tách khỏi PhieuXuatController để kiểm trực tiếp tiêu chí hoàn
/// thành "gợi ý không vi phạm ràng buộc" (lô hết hạn, ô khóa, ô ngừng hoạt động, khu tiếp nhận đều phải bị loại) và "lô hết hạn
/// sớm được ưu tiên", không cần dựng cả MVC controller. Mỗi bài tự dựng biến thể, lô, ô, tồn riêng trong giao dịch của mình
/// (không commit) để không phụ thuộc hay làm bẩn dữ liệu mẫu.
/// </summary>
public class GoiYG1ServiceTests
{
    private const int MaKho = 1;   // Kho tổng (dữ liệu mẫu)

    private sealed record MoiTruong(KhoDbContext Db, GoiYG1Service Service, int MaDay, DateOnly HomNay);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var day = new ViTri { TenViTri = "Dãy test G1 " + Guid.NewGuid().ToString("N")[..8], MaKho = MaKho, CapViTri = CapViTriGiaTri.Day };
        db.ViTri.Add(day);
        await db.SaveChangesAsync();

        var service = new GoiYG1Service(TimeProvider.System, db);
        await chay(new MoiTruong(db, service, day.MaViTri, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    private static ViTri OLuuTru(int maDay, string ten, bool khoa = false, string trangThai = TrangThaiHoatDong.HoatDong) => new()
    {
        TenViTri = ten, MaKho = MaKho, MaViTriCha = maDay, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
        MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 10, Rong = 10, Cao = 10, KhoiLuongToiDa = 100,
        DangKhoa = khoa, TrangThai = trangThai,
    };

    private static async Task<int> TaoBienTheAsync(KhoDbContext db)
    {
        var maDM = await db.DanhMuc.Select(d => d.MaDM).FirstAsync();
        var sp = new SanPham { TenSP = "SP test G1 " + Guid.NewGuid().ToString("N")[..8], MaDM = maDM, DonViTinh = "cái", CoHanDung = true };
        db.SanPham.Add(sp);
        await db.SaveChangesAsync();
        var bt = new BienThe { MaSP = sp.MaSP, MaVach = "BT" + Guid.NewGuid().ToString("N")[..12], ThuocTinh = "Test", Dai = 1, Rong = 1, Cao = 1, KhoiLuong = 1 };
        db.BienThe.Add(bt);
        await db.SaveChangesAsync();
        return bt.MaBT;
    }

    private static async Task<int> LoHangAsync(KhoDbContext db, int maBT, DateOnly? hanDung)
    {
        var l = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = maBT, HanDung = hanDung };
        db.LoHang.Add(l);
        await db.SaveChangesAsync();
        return l.MaLo;
    }

    private static async Task TonAsync(KhoDbContext db, int maLo, int maViTri, int soLuong)
    {
        db.TonKho.Add(new TonKho { MaLo = maLo, MaViTri = maViTri, SoLuongTon = soLuong, SoLuongGiu = 0 });
        await db.SaveChangesAsync();
    }

    private static PhieuXuat Phieu(int maBT, int soLuong) => new()
    {
        SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], LoaiPhieu = PhieuXuat.Khac, MaKho = MaKho, MaNguoiLap = 3,
        ChiTiet = { new ChiTietPhieuXuat { MaCTX = 1, MaBT = maBT, SoLuong = soLuong } },
    };

    [CanSqlServerFact]
    public async Task PhanBo_LoHetHan_BiLoaiKhongDuocGoiY() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = OLuuTru(e.MaDay, "O-HetHan");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(-1));   // đã hết hạn hôm qua
        await TonAsync(e.Db, maLo, o.MaViTri, 100);

        var (phan, loi) = await e.Service.PhanBo(Phieu(maBT, 10));

        Assert.NotNull(loi);
        Assert.Empty(phan);
    });

    [CanSqlServerFact]
    public async Task PhanBo_OBiKhoaKiemKe_BiLoaiKhongDuocGoiY() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = OLuuTru(e.MaDay, "O-Khoa", khoa: true);
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(30));
        await TonAsync(e.Db, maLo, o.MaViTri, 100);

        var (phan, loi) = await e.Service.PhanBo(Phieu(maBT, 10));

        Assert.NotNull(loi);
        Assert.Empty(phan);
    });

    [CanSqlServerFact]
    public async Task PhanBo_ONgungHoatDong_BiLoaiKhongDuocGoiY() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = OLuuTru(e.MaDay, "O-Ngung", trangThai: TrangThaiHoatDong.NgungHoatDong);
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(30));
        await TonAsync(e.Db, maLo, o.MaViTri, 100);

        var (phan, loi) = await e.Service.PhanBo(Phieu(maBT, 10));

        Assert.NotNull(loi);
        Assert.Empty(phan);
    });

    [CanSqlServerFact]
    public async Task PhanBo_OTiepNhan_BiLoaiKhongDuocGoiY() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = new ViTri
        {
            TenViTri = "O-TiepNhan test", MaKho = MaKho, MaViTriCha = e.MaDay, CapViTri = CapViTriGiaTri.O,
            LoaiViTri = LoaiViTriGiaTri.TiepNhan, MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12],
        };
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(30));
        await TonAsync(e.Db, maLo, o.MaViTri, 100);

        var (phan, loi) = await e.Service.PhanBo(Phieu(maBT, 10));

        Assert.NotNull(loi);
        Assert.Empty(phan);
    });

    [CanSqlServerFact]
    public async Task PhanBo_UuTienHetHanSomTruocKhongHanXepSauCungTheoMaLo_TongDungVaKhongVuotTungLo() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = OLuuTru(e.MaDay, "O-Thuong");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();

        var maLoE = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(3));    // hết hạn sớm nhất: phải lấy trước
        var maLoF = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(20));   // hết hạn sau: lấy thứ hai
        var maLoG = await LoHangAsync(e.Db, maBT, null);                  // không hạn, mã lô nhỏ hơn H: lấy thứ ba
        var maLoH = await LoHangAsync(e.Db, maBT, null);                  // không hạn, mã lô lớn hơn G: lấy cuối (một phần)
        await TonAsync(e.Db, maLoE, o.MaViTri, 10);
        await TonAsync(e.Db, maLoF, o.MaViTri, 10);
        await TonAsync(e.Db, maLoG, o.MaViTri, 5);
        await TonAsync(e.Db, maLoH, o.MaViTri, 100);

        var (phan, loi) = await e.Service.PhanBo(Phieu(maBT, 28));   // 10 (E) + 10 (F) + 5 (G) + 3 (một phần của H)

        Assert.Null(loi);
        Assert.Equal(28, phan.Sum(x => x.SoLuong));
        Assert.Equal(new[] { maLoE, maLoF, maLoG, maLoH }, phan.Select(x => x.MaLo).ToArray());
        Assert.Equal(new[] { 10, 10, 5, 3 }, phan.Select(x => x.SoLuong).ToArray());
    });

    [CanSqlServerFact]
    public async Task TaoDeXuat_SapTheoDuongDiODiGanTruoc_KhongTheoThuTuFefoDaChonLo() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        // Tên ô quyết định đường dẫn ("Dãy/ Tên ô"): A-gan đứng trước Z-xa theo thứ tự chữ cái, ngược lại thứ tự FEFO chọn lô
        // (lô hết hạn sớm hơn — được FEFO chọn trước — lại nằm ở ô tên sau, Z-xa).
        var oZ = OLuuTru(e.MaDay, "Z-xa");
        var oA = OLuuTru(e.MaDay, "A-gan");
        e.Db.ViTri.AddRange(oZ, oA);
        await e.Db.SaveChangesAsync();

        var maLoSom = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(3));     // FEFO chọn trước, ở ô Z (xa)
        var maLoMuon = await LoHangAsync(e.Db, maBT, e.HomNay.AddDays(20));   // FEFO chọn sau, ở ô A (gần)
        await TonAsync(e.Db, maLoSom, oZ.MaViTri, 5);
        await TonAsync(e.Db, maLoMuon, oA.MaViTri, 5);

        var (chiTiet, loi) = await e.Service.TaoDeXuat(Phieu(maBT, 10));

        Assert.Null(loi);
        Assert.Equal(2, chiTiet.Count);
        Assert.Equal(10, chiTiet.Sum(c => c.SoLuong));
        // Thứ tự đi lấy theo đường dẫn ô (A trước Z), không theo thứ tự FEFO đã dùng để CHỌN lô.
        Assert.Equal(maLoMuon, chiTiet[0].MaLo);
        Assert.Equal(maLoSom, chiTiet[1].MaLo);
        Assert.Contains("Thứ tự lấy 1", chiTiet[0].LyDo);
        Assert.Contains("FEFO", chiTiet[0].LyDo);
        Assert.Contains("Thứ tự lấy 2", chiTiet[1].LyDo);
    });

    [CanSqlServerFact]
    public async Task TaoDeXuat_LoKhongCoHanDung_LyDoGhiRoKhongCoHanKhongGhiFefo() => await DungAsync(async e =>
    {
        var maBT = await TaoBienTheAsync(e.Db);
        var o = OLuuTru(e.MaDay, "O-KhongHan");
        e.Db.ViTri.Add(o);
        await e.Db.SaveChangesAsync();
        var maLo = await LoHangAsync(e.Db, maBT, null);
        await TonAsync(e.Db, maLo, o.MaViTri, 10);

        var (chiTiet, loi) = await e.Service.TaoDeXuat(Phieu(maBT, 10));

        Assert.Null(loi);
        var dong = Assert.Single(chiTiet);
        Assert.DoesNotContain("FEFO", dong.LyDo);
        Assert.Contains("không có hạn dùng", dong.LyDo);
    });
}
