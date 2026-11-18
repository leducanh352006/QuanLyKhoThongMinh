using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử phiếu chuyển kho (việc T5-2 đến T5-6) trên SQL Server: mỗi bài chạy trong một giao dịch không commit.
/// Dữ liệu mẫu: lô 1 (áo M, mã vạch 8930000000011) ở ô 3 (Kho 1, O-K1-A1-01, tồn 50); ô nhận 10 (Kho 2, O-K2-A1-01).
/// </summary>
public class PhieuChuyenServiceTests
{
    private const int KhoXuat = 1, KhoNhap = 2, MaLo = 1, OXuat = 3, ONhan = 10, SoLuong = 5;
    private const int NguoiLap = 1, QuanLyKho = 2, ThuKho = 3;
    private const string MaVachBT = "8930000000011";

    private sealed record MoiTruong(KhoDbContext Db, PhieuChuyenService Dv, IStockService Ton, string MaVachLo, string MaVachOXuat, string MaVachONhan);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        if (!await db.TuyenVanChuyen.AnyAsync(t => t.MaKhoXuat == KhoXuat && t.MaKhoNhap == KhoNhap))
        {
            db.TuyenVanChuyen.Add(new TuyenVanChuyen { MaKhoXuat = KhoXuat, MaKhoNhap = KhoNhap, DonGiaVC = 5000, PhiChuyen = 150000 });
            await db.SaveChangesAsync();
        }
        var ton = new StockService(db);
        var dv = new PhieuChuyenService(db, ton, new CapacityService(db, new KhongCoHangChoXep()), new AuditService(new EfNhatKyStore(db), TimeProvider.System));
        var lo = await db.LoHang.AsNoTracking().FirstAsync(l => l.MaLo == MaLo);
        var oX = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == OXuat);
        var oN = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == ONhan);
        await chay(new MoiTruong(db, dv, ton, lo.MaVachLo, oX.MaVachO!, oN.MaVachO!));
    }

    private static PhieuChuyen PhieuMau(int soLuong = SoLuong, int oNhan = ONhan) => new()
    {
        MaKhoXuat = KhoXuat, MaKhoNhap = KhoNhap, NguoiVanChuyen = "Nguyễn Văn An", PhuongTien = "Xe tải 29C-123.45",
        ChiTiet = { new ChiTietChuyen { MaLo = MaLo, MaViTriXuat = OXuat, MaViTriNhap = oNhan, SoLuong = soLuong } },
    };

    private static async Task<int> LapVaDuyet(MoiTruong e)
    {
        var lap = await e.Dv.Luu(PhieuMau(), NguoiLap);
        Assert.True(lap.ThanhCong, lap.ThongBao);
        var duyet = await e.Dv.Duyet(lap.GiaTri, QuanLyKho);
        Assert.True(duyet.ThanhCong, duyet.ThongBao);
        return lap.GiaTri;
    }

    private static Task<KetQua> XuatDung(MoiTruong e, int maPC, int maCTC) =>
        e.Dv.Xuat(maPC, new[] { new MaQuetDong(maCTC, e.MaVachOXuat, e.MaVachLo, MaVachBT) }, ThuKho);

    private static async Task<(int Ton, int Giu)> TonTai(MoiTruong e, int maViTri)
    {
        var t = await e.Ton.LayTon(MaLo, maViTri);
        return (t?.SoLuongTon ?? 0, t?.SoLuongGiu ?? 0);
    }

    // ---- Lập phiếu (T5-2) ----

    [CanSqlServerFact]
    public async Task Luu_HopLe_TaoPhieuNhapSoCK() => await DungAsync(async e =>
    {
        var kq = await e.Dv.Luu(PhieuMau(), NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == kq.GiaTri);
        Assert.Equal($"CK{p.MaPC:D6}", p.SoPhieu);
        Assert.Equal(PhieuChuyen.Nhap, p.TrangThai);
    });

    [CanSqlServerFact]
    public async Task Luu_KhoXuatTrungKhoNhap_BiTuChoi() => await DungAsync(async e =>
    {
        var p = PhieuMau();
        p.MaKhoNhap = KhoXuat;

        var kq = await e.Dv.Luu(p, NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains("khác kho đến", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Luu_VuotTonKhaDung_BiTuChoiNeuRoSoConLai() => await DungAsync(async e =>
    {
        var (ton, giu) = await TonTai(e, OXuat);

        var kq = await e.Dv.Luu(PhieuMau(soLuong: ton - giu + 1), NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains($"chỉ còn {ton - giu}", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Luu_ONhanKhongVua_BaoRoLyDo() => await DungAsync(async e =>
    {
        // Ô lưu trữ rất nhỏ ở kho nhập: hộp áo 30×25×3 cm không lọt lòng 10×10×10 cm.
        var nho = new ViTri
        {
            TenViTri = "Ô thử nhỏ", MaKho = KhoNhap, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
            MaVachO = "O-THU-" + Guid.NewGuid().ToString("N")[..8], Dai = 10, Rong = 10, Cao = 10, KhoiLuongToiDa = 1,
        };
        e.Db.ViTri.Add(nho);
        await e.Db.SaveChangesAsync();

        var kq = await e.Dv.Luu(PhieuMau(oNhan: nho.MaViTri), NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains("Ô xếp hàng 'Ô thử nhỏ'", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Luu_ChuaCoTuyen_BiTuChoi() => await DungAsync(async e =>
    {
        await e.Db.TuyenVanChuyen.Where(t => t.MaKhoXuat == KhoNhap && t.MaKhoNhap == KhoXuat).ExecuteDeleteAsync();
        var p = new PhieuChuyen
        {
            MaKhoXuat = KhoNhap, MaKhoNhap = KhoXuat,
            ChiTiet = { new ChiTietChuyen { MaLo = MaLo, MaViTriXuat = ONhan, MaViTriNhap = OXuat, SoLuong = 1 } },
        };

        var kq = await e.Dv.Luu(p, NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains("Chưa có tuyến vận chuyển", kq.ThongBao);
    });

    // ---- Duyệt, hủy (T5-3, T5-6) ----

    [CanSqlServerFact]
    public async Task Duyet_GiuChoOKhoXuat_HuyThiTraLai() => await DungAsync(async e =>
    {
        var (_, giuTruoc) = await TonTai(e, OXuat);

        var maPC = await LapVaDuyet(e);
        Assert.Equal(giuTruoc + SoLuong, (await TonTai(e, OXuat)).Giu);
        Assert.Equal(PhieuChuyen.DaDuyet, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);

        var huy = await e.Dv.Huy(maPC, QuanLyKho);
        Assert.True(huy.ThanhCong, huy.ThongBao);
        Assert.Equal(giuTruoc, (await TonTai(e, OXuat)).Giu);
        Assert.Equal(PhieuChuyen.DaHuy, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Duyet_NguoiLapTuDuyet_BiChan() => await DungAsync(async e =>
    {
        var lap = await e.Dv.Luu(PhieuMau(), QuanLyKho);

        var kq = await e.Dv.Duyet(lap.GiaTri, QuanLyKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(PhieuChuyen.Nhap, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == lap.GiaTri)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Sua_PhieuDaDuyet_BiTuChoi() => await DungAsync(async e =>
    {
        var maPC = await LapVaDuyet(e);
        var p = PhieuMau(soLuong: 1);
        p.MaPC = maPC;

        var kq = await e.Dv.Luu(p, NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains("khóa", kq.ThongBao);
    });

    // ---- Xuất khỏi kho xuất (T5-4) ----

    [CanSqlServerFact]
    public async Task Xuat_QuetSaiLo_BiTuChoiKhongDoiGi() => await DungAsync(async e =>
    {
        var maPC = await LapVaDuyet(e);
        var maCTC = (await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaPC == maPC)).MaCTC;
        var truoc = await TonTai(e, OXuat);

        var kq = await e.Dv.Xuat(maPC, new[] { new MaQuetDong(maCTC, e.MaVachOXuat, "LO-SAI", MaVachBT) }, ThuKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(truoc, await TonTai(e, OXuat));
        Assert.Equal(PhieuChuyen.DaDuyet, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task Xuat_QuetDung_TruTonKhoXuatVaChuaCongKhoNhap() => await DungAsync(async e =>
    {
        var (tonXTruoc, giuXTruoc) = await TonTai(e, OXuat);
        var (tonNTruoc, _) = await TonTai(e, ONhan);
        var maPC = await LapVaDuyet(e);
        var maCTC = (await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaPC == maPC)).MaCTC;

        var kq = await XuatDung(e, maPC, maCTC);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal((tonXTruoc - SoLuong, giuXTruoc), await TonTai(e, OXuat));
        Assert.Equal(tonNTruoc, (await TonTai(e, ONhan)).Ton);   // đang trên đường: chưa thuộc kho nhập
        Assert.Equal(PhieuChuyen.DangVanChuyen, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
        Assert.True(await e.Db.BienDongTon.AnyAsync(b => b.LoaiCT == "CK" && b.MaCT == maPC && b.SoLuong == -SoLuong));
    });

    [CanSqlServerFact]
    public async Task Xuat_GoiLaiLanHai_KhongGhiSoHaiLan() => await DungAsync(async e =>
    {
        var maPC = await LapVaDuyet(e);
        var maCTC = (await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaPC == maPC)).MaCTC;
        Assert.True((await XuatDung(e, maPC, maCTC)).ThanhCong);

        var lan2 = await XuatDung(e, maPC, maCTC);

        Assert.False(lan2.ThanhCong);
        Assert.Equal(1, await e.Db.BienDongTon.CountAsync(b => b.LoaiCT == "CK" && b.MaCT == maPC));
    });

    [CanSqlServerFact]
    public async Task Huy_SauKhiXuat_BiTuChoi() => await DungAsync(async e =>
    {
        var maPC = await LapVaDuyet(e);
        var maCTC = (await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaPC == maPC)).MaCTC;
        Assert.True((await XuatDung(e, maPC, maCTC)).ThanhCong);

        var kq = await e.Dv.Huy(maPC, QuanLyKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(PhieuChuyen.DangVanChuyen, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    // ---- Nhận tại kho nhập (T5-5) ----

    private static async Task<(int MaPC, int MaCTC)> DenLucNhan(MoiTruong e)
    {
        var maPC = await LapVaDuyet(e);
        var maCTC = (await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaPC == maPC)).MaCTC;
        Assert.True((await XuatDung(e, maPC, maCTC)).ThanhCong);
        return (maPC, maCTC);
    }

    [CanSqlServerFact]
    public async Task Nhan_VuotSoGui_BiTuChoi() => await DungAsync(async e =>
    {
        var (maPC, maCTC) = await DenLucNhan(e);
        var truoc = await TonTai(e, ONhan);

        var kq = await e.Dv.Nhan(maCTC, e.MaVachONhan, e.MaVachLo, SoLuong + 1, null, ThuKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(truoc, await TonTai(e, ONhan));
        Assert.Null((await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaCTC == maCTC)).SLNhan);
    });

    [CanSqlServerFact]
    public async Task Nhan_QuetSaiO_BiTuChoi() => await DungAsync(async e =>
    {
        var (_, maCTC) = await DenLucNhan(e);

        var kq = await e.Dv.Nhan(maCTC, e.MaVachOXuat, e.MaVachLo, SoLuong, null, ThuKho);

        Assert.False(kq.ThanhCong);
        Assert.Null((await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(d => d.MaCTC == maCTC)).SLNhan);
    });

    [CanSqlServerFact]
    public async Task Nhan_Du_CongTonKhoNhapVaPhieuDaNhan_SoCaiKhop() => await DungAsync(async e =>
    {
        var (tonNTruoc, _) = await TonTai(e, ONhan);
        var (maPC, maCTC) = await DenLucNhan(e);

        var kq = await e.Dv.Nhan(maCTC, e.MaVachONhan, e.MaVachLo, SoLuong, null, ThuKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(tonNTruoc + SoLuong, (await TonTai(e, ONhan)).Ton);
        Assert.Equal(PhieuChuyen.DaNhan, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
        Assert.Empty(await e.Ton.DoiChieuSoCai());
    });

    [CanSqlServerFact]
    public async Task Nhan_ThieuKhongGhiLyDo_BiTuChoi_CoLyDoThiGhiBienBan() => await DungAsync(async e =>
    {
        var (tonNTruoc, _) = await TonTai(e, ONhan);
        var (maPC, maCTC) = await DenLucNhan(e);

        var khongLyDo = await e.Dv.Nhan(maCTC, e.MaVachONhan, e.MaVachLo, SoLuong - 2, null, ThuKho);
        Assert.False(khongLyDo.ThanhCong);

        var coLyDo = await e.Dv.Nhan(maCTC, e.MaVachONhan, e.MaVachLo, SoLuong - 2, "2 hộp rách khi vận chuyển", ThuKho);
        Assert.True(coLyDo.ThanhCong, coLyDo.ThongBao);
        var d = await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(x => x.MaCTC == maCTC);
        Assert.Equal(SoLuong - 2, d.SLNhan);
        Assert.Equal("2 hộp rách khi vận chuyển", d.LyDoLech);
        Assert.Equal(tonNTruoc + SoLuong - 2, (await TonTai(e, ONhan)).Ton);
        Assert.Equal(PhieuChuyen.NhanMotPhan, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    // ---- Xử lý chênh lệch (báo cáo mục 2.1.5, Bảng 1.5) ----

    private static async Task<(int MaPC, int MaCTC)> NhanThieu(MoiTruong e, int slNhan)
    {
        var (maPC, maCTC) = await DenLucNhan(e);
        var kq = await e.Dv.Nhan(maCTC, e.MaVachONhan, e.MaVachLo, slNhan, "Thiếu khi đếm", ThuKho);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        return (maPC, maCTC);
    }

    [CanSqlServerFact]
    public async Task XuLyLech_ThieuNguyenNhan_BiTuChoi() => await DungAsync(async e =>
    {
        var (maPC, maCTC) = await NhanThieu(e, SoLuong - 1);

        var kq = await e.Dv.XuLyLech(maCTC, ChiTietChuyen.NhapTaiKhoDen, "  ", QuanLyKho);

        Assert.False(kq.ThanhCong);
        Assert.Equal(PhieuChuyen.NhanMotPhan, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task XuLyLech_NhapTaiKhoDen_CongPhanThieuVaoONhanVaDaNhan() => await DungAsync(async e =>
    {
        var (tonNTruoc, _) = await TonTai(e, ONhan);
        var (maPC, maCTC) = await NhanThieu(e, SoLuong - 2);

        var kq = await e.Dv.XuLyLech(maCTC, ChiTietChuyen.NhapTaiKhoDen, "Tìm thấy 2 hộp kẹt dưới sàn xe", QuanLyKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(tonNTruoc + SoLuong, (await TonTai(e, ONhan)).Ton);
        var d = await e.Db.ChiTietChuyen.AsNoTracking().SingleAsync(x => x.MaCTC == maCTC);
        Assert.Equal(ChiTietChuyen.NhapTaiKhoDen, d.XuLyLech);
        Assert.Equal(PhieuChuyen.DaNhan, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
        Assert.Empty(await e.Ton.DoiChieuSoCai());
    });

    [CanSqlServerFact]
    public async Task XuLyLech_TraVeKhoDi_CongPhanThieuVaoOLay() => await DungAsync(async e =>
    {
        var (tonXTruoc, _) = await TonTai(e, OXuat);
        var (maPC, maCTC) = await NhanThieu(e, SoLuong - 2);

        var kq = await e.Dv.XuLyLech(maCTC, ChiTietChuyen.TraVeKhoDi, "Xuất nhầm số, 2 hộp còn ở kệ", QuanLyKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(tonXTruoc - SoLuong + 2, (await TonTai(e, OXuat)).Ton);
        Assert.Equal(PhieuChuyen.DaNhan, (await e.Db.PhieuChuyen.AsNoTracking().SingleAsync(x => x.MaPC == maPC)).TrangThai);
    });

    [CanSqlServerFact]
    public async Task XuLyLech_SoThieuBangSoDaNhan_VanGhiDuSoCai() => await DungAsync(async e =>
    {
        // Gửi 5, nhận 0 rồi nhập tại kho đến 5: số ghi lần sau bằng số gửi, khác loại biến động nên sổ cái không coi là ghi lặp.
        var (tonNTruoc, _) = await TonTai(e, ONhan);
        var (_, maCTC) = await NhanThieu(e, 0);

        var kq = await e.Dv.XuLyLech(maCTC, ChiTietChuyen.NhapTaiKhoDen, "Hàng đến muộn chuyến sau", QuanLyKho);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(tonNTruoc + SoLuong, (await TonTai(e, ONhan)).Ton);
    });

    [CanSqlServerFact]
    public async Task XuLyLech_GoiLaiLanHai_BiTuChoi() => await DungAsync(async e =>
    {
        var (_, maCTC) = await NhanThieu(e, SoLuong - 1);
        Assert.True((await e.Dv.XuLyLech(maCTC, ChiTietChuyen.NhapTaiKhoDen, "Tìm thấy", QuanLyKho)).ThanhCong);
        var (tonN, _) = await TonTai(e, ONhan);

        var lan2 = await e.Dv.XuLyLech(maCTC, ChiTietChuyen.TraVeKhoDi, "Bấm lại", QuanLyKho);

        Assert.False(lan2.ThanhCong);
        Assert.Equal(tonN, (await TonTai(e, ONhan)).Ton);
    });
}

/// <summary>Quy tắc lập phiếu chuyển không cần CSDL.</summary>
public class PhieuChuyenQuyTacTests
{
    private static PhieuChuyen Mau() => new()
    {
        MaKhoXuat = 1, MaKhoNhap = 2,
        ChiTiet = { new ChiTietChuyen { MaLo = 1, MaViTriXuat = 3, MaViTriNhap = 10, SoLuong = 5 } },
    };

    [Fact]
    public void HopLe_KhongLoi() => Assert.Null(Mau().KiemTra());

    [Fact]
    public void KhoTrung_BaoLoi()
    {
        var p = Mau();
        p.MaKhoNhap = 1;
        Assert.Equal("Kho đi phải khác kho đến.", p.KiemTra());
    }

    [Fact]
    public void NguoiVanChuyenVaPhuongTien_KhongBatBuoc()
    {
        var p = Mau();
        p.NguoiVanChuyen = null;
        p.PhuongTien = null;
        Assert.Null(p.KiemTra());
    }

    [Fact]
    public void KhongCoDong_BaoLoi()
    {
        var p = Mau();
        p.ChiTiet.Clear();
        Assert.Equal("Phiếu phải có ít nhất một dòng hàng.", p.KiemTra());
    }

    [Fact]
    public void HaiDongCungLoCungOXuat_BaoGop()
    {
        var p = Mau();
        p.ChiTiet.Add(new ChiTietChuyen { MaLo = 1, MaViTriXuat = 3, MaViTriNhap = 11, SoLuong = 2 });
        Assert.Contains("gộp lại", p.KiemTra());
    }

    [Fact]
    public void HuyDuoc_ChiKhiNhapHoacDaDuyet()
    {
        var p = Mau();
        foreach (var (tt, mong) in new[] { (PhieuChuyen.Nhap, true), (PhieuChuyen.DaDuyet, true), (PhieuChuyen.DangVanChuyen, false), (PhieuChuyen.NhanMotPhan, false), (PhieuChuyen.DaNhan, false), (PhieuChuyen.DaHuy, false) })
        {
            p.TrangThai = tt;
            Assert.Equal(mong, p.HuyDuoc);
        }
    }
}
