using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T1-3 (gợi ý G2, issue #9) cho GoiYG2Service và HandlerG2 chạy thật trong một GoiYService thật (như HandlerG1Tests):
/// ô được gợi ý, chặn gợi ý trùng, lý do theo dung tích còn lại, duyệt và "sửa rồi duyệt" kiểm lại mọi ràng buộc, hoàn tác trọn vẹn khi lỗi.
/// Mỗi bài tự dựng kho, ô, biến thể, lô, phiếu riêng trong giao dịch của mình (không commit) nên không phụ thuộc hay làm bẩn dữ liệu mẫu.
/// </summary>
public class GoiYG2DichVuTests
{
    private const int MaQuanLyKho = 2, MaThuKho = 3;   // Thủ kho và Quản trị viên mới được xử lý gợi ý G2 (QuyTacGoiY)

    private sealed record HangTest(int MaBT, int MaLo);

    // Mỗi ô lưu trữ 10×10×10 cm (1000 cm³), tối đa 100 kg; mỗi đơn vị hàng thử 10×10×1 cm (100 cm³), 1 kg nên một ô nhận tối đa 10 đơn vị.
    private sealed record MoiTruong(
        KhoDbContext Db, GoiYG2Service DichVu, GoiYService GoiY, int MaKho,
        ViTri OTot, ViTri OTiepNhan, ViTri OKhoa, ViTri ONgung, ViTri OKhoKhac, HangTest A, HangTest B);

    private static string Ma() => Guid.NewGuid().ToString("N")[..8];

    private static ViTri TaoO(int maKho, string ten, bool khoa = false, string trangThai = TrangThaiHoatDong.HoatDong) => new()
    {
        TenViTri = ten, MaKho = maKho, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
        MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 10, Rong = 10, Cao = 10, KhoiLuongToiDa = 100,
        DangKhoa = khoa, TrangThai = trangThai,
    };

    private static async Task<HangTest> LoMoiAsync(KhoDbContext db, int maBT)
    {
        var lo = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = maBT };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();
        return new HangTest(maBT, lo.MaLo);
    }

    private static async Task<HangTest> TaoHangAsync(KhoDbContext db)
    {
        var maDM = await db.DanhMuc.Select(d => d.MaDM).FirstAsync();
        var sp = new SanPham { TenSP = "SP test G2 " + Ma(), MaDM = maDM, DonViTinh = "cái" };
        db.SanPham.Add(sp);
        await db.SaveChangesAsync();
        var bt = new BienThe { MaSP = sp.MaSP, MaVach = "BT" + Guid.NewGuid().ToString("N")[..12], ThuocTinh = "Test", Dai = 10, Rong = 10, Cao = 1, KhoiLuong = 1 };
        db.BienThe.Add(bt);
        await db.SaveChangesAsync();
        return await LoMoiAsync(db, bt.MaBT);
    }

    /// <summary>Phiếu nhập Đã kiểm tra: mỗi dòng có lô và toàn bộ số lượng là hàng đạt, chưa lập lệnh xếp.</summary>
    private static async Task<PhieuNhap> TaoPhieuAsync(KhoDbContext db, int maKho, params (HangTest Hang, int SoLuong)[] dong)
    {
        var p = new PhieuNhap
        {
            SoPhieu = "T3G" + Guid.NewGuid().ToString("N")[..10], LoaiPhieu = "Mua hàng", ThoiGian = DateTime.UtcNow, MaKho = maKho,
            MaNguoiLap = MaQuanLyKho, TrangThai = PhieuNhap.DaKiemTra,
        };
        foreach (var (hang, soLuong) in dong)
            p.ChiTiet.Add(new ChiTietPhieuNhap { MaBT = hang.MaBT, SLDuKien = soLuong, MaLo = hang.MaLo, SLThucNhan = soLuong, SLDat = soLuong });
        db.PhieuNhap.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var kho = new Kho { TenKho = "Kho test G2 " + Ma(), LoaiKho = LoaiKhoGiaTri.KhoLuuTru, MaQuanLy = MaQuanLyKho };
        var khoKhac = new Kho { TenKho = "Kho khác test G2 " + Ma(), LoaiKho = LoaiKhoGiaTri.KhoLuuTru, MaQuanLy = MaQuanLyKho };
        db.Kho.AddRange(kho, khoKhac);
        await db.SaveChangesAsync();

        // Các ô không được gợi ý tạo trước ô tốt để nếu bộ lọc sai thì chúng thắng khi điểm bằng nhau (mã nhỏ hơn).
        var oTiepNhan = new ViTri
        {
            TenViTri = "O-TiepNhan " + Ma(), MaKho = kho.MaKho, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.TiepNhan,
            MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12],
        };
        var oKhoa = TaoO(kho.MaKho, "O-Khoa " + Ma(), khoa: true);
        var oNgung = TaoO(kho.MaKho, "O-Ngung " + Ma(), trangThai: TrangThaiHoatDong.NgungHoatDong);
        var oKhoKhac = TaoO(khoKhac.MaKho, "O-KhoKhac " + Ma());
        var oTot = TaoO(kho.MaKho, "O-Tot " + Ma());
        db.ViTri.AddRange(oTiepNhan, oKhoa, oNgung, oKhoKhac, oTot);
        await db.SaveChangesAsync();

        var a = await TaoHangAsync(db);
        var b = await TaoHangAsync(db);

        var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
        var sucChua = new CapacityService(db, new HangChoXepTuSapXep(db));
        var handler = new HandlerG2(new StockService(db), sucChua, audit, db);
        var goiY = new GoiYService(new EfGoiYStore(db), new[] { (IGoiYHandler)handler }, new EfHeThongStore(db), audit, TimeProvider.System);
        await chay(new MoiTruong(db, new GoiYG2Service(db, sucChua, goiY, audit), goiY, kho.MaKho, oTot, oTiepNhan, oKhoa, oNgung, oKhoKhac, a, b));
    }

    // ---- GoiYG2Service.TaoGoiY ----

    [CanSqlServerFact]
    public async Task TaoGoiY_ChiGoiYOLuuTruDungDuoc_KhongTiepNhanKhoaNgungHayKhoKhac() => await DungAsync(async e =>
    {
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, 5), (e.B, 3));

        var kq = await e.DichVu.TaoGoiY(p.MaPN, MaThuKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var g = await e.GoiY.Lay(kq.GiaTri);
        Assert.NotNull(g);
        Assert.Equal(8, g!.ChiTiet.Sum(c => c.SoLuong));
        Assert.All(g.ChiTiet, c => Assert.Equal((int?)e.OTot.MaViTri, c.MaViTri));   // ô Tiếp nhận, khóa, ngừng hoạt động, kho khác đều bị loại
    });

    [CanSqlServerFact]
    public async Task TaoGoiY_PhieuDaCoGoiYChoDuyet_BiChanVaCoGhiNhatKy() => await DungAsync(async e =>
    {
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, 4));

        var dau = await e.DichVu.TaoGoiY(p.MaPN, MaThuKho);
        Assert.True(dau.ThanhCong, dau.ThongBao);
        var lai = await e.DichVu.TaoGoiY(p.MaPN, MaThuKho);

        Assert.False(lai.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, lai.MaLoi);
        Assert.Equal(1, (await e.GoiY.DanhSachChoDuyet(LoaiGoiY.G2)).Count(g => g.ThamChieu.MaPN == p.MaPN));
        Assert.True(await e.Db.NhatKyThaoTac.AsNoTracking().AnyAsync(n => n.HanhDong == "Tạo gợi ý G2" && n.MaCT == p.MaPN));
    });

    [CanSqlServerFact]
    public async Task TaoGoiY_HaiDongVaoMotO_TongKhongVuotSucChuaVaLyDoDong2TinhConLaiSauDong1() => await DungAsync(async e =>
    {
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, 6), (e.B, 3));   // 600 cm³ rồi 300 cm³ vào ô 1000 cm³

        var kq = await e.DichVu.TaoGoiY(p.MaPN, MaThuKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var g = await e.GoiY.Lay(kq.GiaTri);
        var dongA = Assert.Single(g!.ChiTiet, c => c.MaBT == e.A.MaBT);
        var dongB = Assert.Single(g.ChiTiet, c => c.MaBT == e.B.MaBT);
        Assert.Equal((int?)e.OTot.MaViTri, dongA.MaViTri);
        Assert.Equal((int?)e.OTot.MaViTri, dongB.MaViTri);
        Assert.True(dongA.SoLuong + dongB.SoLuong <= 10, "Tổng hai dòng không được vượt sức chứa 10 đơn vị của ô.");
        Assert.Contains("còn 1000 cm³", dongA.LyDo);
        Assert.Contains("còn 400 cm³", dongB.LyDo);   // 1000 - 6 × 100, không phải số dư ban đầu
        Assert.DoesNotContain("còn 1000 cm³", dongB.LyDo);
    });

    // ---- HandlerG2 qua GoiYService ----

    [CanSqlServerFact]
    public async Task Duyet_GoiYHopLe_SinhLenhChoXacNhanVaKhongDoiTon() => await DungAsync(async e =>
    {
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, 5), (e.B, 3));
        var tao = await e.DichVu.TaoGoiY(p.MaPN, MaThuKho);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaThuKho);

        Assert.True(duyet.ThanhCong, duyet.ThongBao);
        var ma = p.ChiTiet.Select(d => d.MaCTN).ToList();
        var lenh = await e.Db.SapXep.AsNoTracking().Where(s => ma.Contains(s.MaCTN)).OrderBy(s => s.MaCTN).ToListAsync();
        Assert.Equal(new[] { 5, 3 }, lenh.Select(s => s.SoLuong).ToArray());
        Assert.All(lenh, s =>
        {
            Assert.Equal(SapXep.ChoXacNhan, s.TrangThai);
            Assert.Equal(e.OTot.MaViTri, s.MaViTri);
        });
        Assert.False(await e.Db.TonKho.AnyAsync(t => t.MaLo == e.A.MaLo || t.MaLo == e.B.MaLo));   // chưa chạm tồn: thủ kho còn phải quét mã
        Assert.Equal(TrangThaiGoiY.DaDuyet, (await e.GoiY.Lay(tao.GiaTri))!.TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_HaiDongCungBienTheKhacLo_LenhGanDungDongTheoLo() => await DungAsync(async e =>
    {
        var lo2 = await LoMoiAsync(e.Db, e.A.MaBT);   // lô thứ hai của cùng biến thể
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, 4), (lo2, 3));
        var goc = new[]
        {
            new ChiTietGoiYDto(e.A.MaBT, 3, "Lô thứ hai.", lo2.MaLo, e.OTot.MaViTri),
            new ChiTietGoiYDto(e.A.MaBT, 4, "Lô thứ nhất.", e.A.MaLo, e.OTot.MaViTri),
        };
        var tao = await e.GoiY.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: p.MaPN), goc);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var duyet = await e.GoiY.Duyet(tao.GiaTri, MaThuKho);

        Assert.True(duyet.ThanhCong, duyet.ThongBao);
        var dongLo1 = p.ChiTiet.Single(d => d.MaLo == e.A.MaLo);
        var dongLo2 = p.ChiTiet.Single(d => d.MaLo == lo2.MaLo);
        var lenh = await e.Db.SapXep.AsNoTracking().Where(s => s.MaCTN == dongLo1.MaCTN || s.MaCTN == dongLo2.MaCTN).ToListAsync();
        Assert.Equal(4, lenh.Single(s => s.MaCTN == dongLo1.MaCTN).SoLuong);
        Assert.Equal(3, lenh.Single(s => s.MaCTN == dongLo2.MaCTN).SoLuong);
    });

    /// <summary>Gợi ý gốc hợp lệ, người duyệt "sửa rồi duyệt" thành dòng sai: bị từ chối, không phát sinh lệnh xếp nào, gợi ý vẫn Chờ duyệt.</summary>
    private static Task SuaSaiBiTuChoiAsync(Func<MoiTruong, ChiTietGoiYDto> dongDaSua, int soLuongDat = 4) => DungAsync(async e =>
    {
        var p = await TaoPhieuAsync(e.Db, e.MaKho, (e.A, soLuongDat));
        var goc = new[] { new ChiTietGoiYDto(e.A.MaBT, 4, "Gợi ý gốc hợp lệ.", e.A.MaLo, e.OTot.MaViTri) };
        var tao = await e.GoiY.Tao(LoaiGoiY.G2, new ThamChieuGoiY(MaPN: p.MaPN), goc);
        Assert.True(tao.ThanhCong, tao.ThongBao);

        var kq = await e.GoiY.SuaVaDuyet(tao.GiaTri, new[] { dongDaSua(e) }, MaThuKho);

        Assert.False(kq.ThanhCong);
        var ma = p.ChiTiet.Select(d => d.MaCTN).ToList();
        Assert.False(await e.Db.SapXep.AsNoTracking().AnyAsync(s => ma.Contains(s.MaCTN)));
        Assert.Equal(TrangThaiGoiY.ChoDuyet, (await e.GoiY.Lay(tao.GiaTri))!.TrangThai);
    });

    [CanSqlServerFact]
    public Task SuaRoiDuyet_TroToOTiepNhan_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Sửa sang ô Tiếp nhận.", e.A.MaLo, e.OTiepNhan.MaViTri));

    [CanSqlServerFact]
    public Task SuaRoiDuyet_TroToODangKhoa_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Sửa sang ô đang khóa kiểm kê.", e.A.MaLo, e.OKhoa.MaViTri));

    [CanSqlServerFact]
    public Task SuaRoiDuyet_TroToONgungHoatDong_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Sửa sang ô ngừng hoạt động.", e.A.MaLo, e.ONgung.MaViTri));

    [CanSqlServerFact]
    public Task SuaRoiDuyet_TroToOKhoKhac_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Sửa sang ô của kho khác.", e.A.MaLo, e.OKhoKhac.MaViTri));

    [CanSqlServerFact]
    public Task SuaRoiDuyet_VuotSucChuaCuaO_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 12, "Sửa thành 12 đơn vị, ô chỉ nhận tối đa 10.", e.A.MaLo, e.OTot.MaViTri), soLuongDat: 12);

    [CanSqlServerFact]
    public Task SuaRoiDuyet_SangLoCuaBienTheKhac_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Sửa sai lô, lô này không thuộc phiếu.", e.B.MaLo, e.OTot.MaViTri));

    [CanSqlServerFact]
    public Task SuaRoiDuyet_BoTrongLo_BiTuChoiHoanTac() =>
        SuaSaiBiTuChoiAsync(e => new ChiTietGoiYDto(e.A.MaBT, 4, "Xóa trống lô.", null, e.OTot.MaViTri));
}
