using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-5 (Hiển thị vị trí và tồn): tra cứu vị trí, báo cáo tổng hợp tồn từng kho.</summary>
public class TraCuuTonTests
{
    // ---- Không cần SQL Server ----

    private static Dictionary<int, ViTri> Cay() => new[]
    {
        new ViTri { MaViTri = 1, TenViTri = "Dãy A" },
        new ViTri { MaViTri = 2, TenViTri = "Kệ A1", MaViTriCha = 1 },
        new ViTri { MaViTri = 3, TenViTri = "Ô A1-01", MaViTriCha = 2 },
        new ViTri { MaViTri = 7, TenViTri = "Khu tiếp nhận" },   // ô gốc, không có cha
    }.ToDictionary(v => v.MaViTri);

    [Fact]
    public void DuongDan_DuTuKhoDenO() =>
        Assert.Equal("Kho tổng › Dãy A › Kệ A1 › Ô A1-01", TraCuuTonService.DuongDan("Kho tổng", 3, Cay()));

    [Fact]
    public void DuongDan_OGocChiCoKhoVaO() =>
        Assert.Equal("Kho tổng › Khu tiếp nhận", TraCuuTonService.DuongDan("Kho tổng", 7, Cay()));

    [Fact]
    public void DuongDan_ChaThieuHoacVongLapThiDungKhongLoi()
    {
        Assert.Equal("Kho tổng", TraCuuTonService.DuongDan("Kho tổng", 99, Cay()));

        var vong = new[]
        {
            new ViTri { MaViTri = 1, TenViTri = "A", MaViTriCha = 2 },
            new ViTri { MaViTri = 2, TenViTri = "B", MaViTriCha = 1 },
        }.ToDictionary(v => v.MaViTri);
        Assert.Equal("Kho tổng › B › A", TraCuuTonService.DuongDan("Kho tổng", 1, vong));
    }

    [Theory]
    [InlineData(5, 3, 0, 5, 3)]      // đổi ô trong kho cộng lại bằng 0: không đổi
    [InlineData(5, 3, 4, 9, 3)]      // dồn dư vào kho: tính là nhập
    [InlineData(5, 3, -2, 5, 5)]     // dồn dư ra khỏi kho: tính là xuất
    public void GopDoiO_PhanLechTinhLaNhapHoacXuat(int nhap, int xuat, int doiO, int nhapMong, int xuatMong) =>
        Assert.Equal((nhapMong, xuatMong), TraCuuTonService.GopDoiO(nhap, xuat, doiO));

    [Fact]
    public void Menu_CoTraCuuVaBaoCaoChoThuKhoVaQuanLyKho()
    {
        var muc = new NhapKhoMenu().LayNhom().Muc;
        foreach (var hanhDong in new[] { "Index", "BaoCao" })
        {
            var m = Assert.Single(muc, x => x.Controller == "TraCuu" && x.Action == hanhDong);
            Assert.Contains(VaiTroTen.ThuKho, m.VaiTro);
            Assert.Contains(VaiTroTen.QuanLyKho, m.VaiTro);
        }
    }

    // ---- Cần SQL Server đã migrate và seed: mỗi bài chạy trong một giao dịch rồi hoàn tác, không để lại dữ liệu ----

    private const int Nguoi = TonKhoSeeder.MaNguoiGhiTonMau;

    private static async Task DungAsync(Func<KhoDbContext, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc
        await chay(db);
    }

    /// <summary>Ô lưu trữ thứ nhất, thứ hai của kho thứ nhất; ô lưu trữ của kho thứ hai; ô tiếp nhận của kho thứ nhất (đúng dữ liệu mẫu: Kho tổng có 3 ô lưu trữ, Kho quầy có 2).</summary>
    private sealed record Oc(ViTri O1, ViTri O2, ViTri OKho2, ViTri TiepNhan);

    private static async Task<Oc> LayOAsync(KhoDbContext db)
    {
        var luuTru = await db.ViTri.AsNoTracking().Where(v => v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.LuuTru)
            .OrderBy(v => v.MaKho).ThenBy(v => v.MaViTri).ToListAsync();
        var maKho1 = luuTru[0].MaKho;
        var ofKho1 = luuTru.Where(v => v.MaKho == maKho1).ToList();
        var tiepNhan = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaKho == maKho1 && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan);
        return new Oc(ofKho1[0], ofKho1[1], luuTru.First(v => v.MaKho != maKho1), tiepNhan);
    }

    /// <summary>Lô mới có mã vạch bắt đầu bằng <paramref name="tienTo"/>, cùng biến thể với một lô mẫu không có hạn dùng.</summary>
    private static async Task<LoHang> TaoLoAsync(KhoDbContext db, string tienTo, string hau)
    {
        var mau = await db.LoHang.AsNoTracking().OrderBy(l => l.MaLo).FirstAsync(l => l.HanDung == null);
        var lo = new LoHang { MaVachLo = tienTo + hau, MaBT = mau.MaBT };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();
        return lo;
    }

    [CanSqlServerFact]
    public Task Tra_ChiHangODaSapXep_DungOVaSoLuong_TheoKhoLoO() => DungAsync(async db =>
    {
        var e = await LayOAsync(db);
        var tienTo = "T15" + Guid.NewGuid().ToString("N")[..8];
        var loA = await TaoLoAsync(db, tienTo, "-A");
        var loB = await TaoLoAsync(db, tienTo, "-B");
        db.TonKho.AddRange(
            new TonKho { MaLo = loA.MaLo, MaViTri = e.O1.MaViTri, SoLuongTon = 7, SoLuongGiu = 2 },
            new TonKho { MaLo = loA.MaLo, MaViTri = e.O2.MaViTri, SoLuongTon = 3 },
            new TonKho { MaLo = loA.MaLo, MaViTri = e.OKho2.MaViTri, SoLuongTon = 4 },
            new TonKho { MaLo = loA.MaLo, MaViTri = e.TiepNhan.MaViTri, SoLuongTon = 5 },    // chưa sắp xếp: không hiện
            new TonKho { MaLo = loB.MaLo, MaViTri = e.O1.MaViTri, SoLuongTon = 0 });         // hết hàng: không hiện
        await db.SaveChangesAsync();
        var ts = new TraCuuTonService(db);

        // Tra theo lô: ba ô lưu trữ của hai kho, đúng số lượng, không có ô tiếp nhận và dòng tồn 0.
        var theoLo = await ts.Tra(new BoLocTra(Lo: tienTo));
        Assert.False(theoLo.BiCat);
        Assert.Equal(new[] { e.O1.MaViTri, e.O2.MaViTri, e.OKho2.MaViTri }.OrderBy(x => x), theoLo.Dong.Select(d => d.MaViTri).OrderBy(x => x));
        var d1 = theoLo.Dong.Single(d => d.MaViTri == e.O1.MaViTri);
        Assert.Equal((7, 2, 5), (d1.SoLuongTon, d1.SoLuongGiu, d1.KhaDung));
        Assert.Equal(loA.MaVachLo, d1.MaVachLo);
        Assert.Equal(e.O1.MaVachO, d1.MaVachO);

        // Đường dẫn bắt đầu bằng tên kho, kết thúc bằng tên ô, có cả cấp giữa (dữ liệu mẫu: Dãy, Kệ).
        var phan = d1.DuongDan.Split(" › ");
        Assert.Equal(d1.TenKho, phan[0]);
        Assert.Equal(e.O1.TenViTri, phan[^1]);
        Assert.True(phan.Length >= 3);

        // Tồn tính riêng từng kho.
        var kho1 = await ts.Tra(new BoLocTra(MaKho: e.O1.MaKho, Lo: tienTo));
        Assert.Equal(10, kho1.Dong.Sum(d => d.SoLuongTon));
        Assert.All(kho1.Dong, d => Assert.Equal(e.O1.MaKho, d.MaKho));
        var kho2 = await ts.Tra(new BoLocTra(MaKho: e.OKho2.MaKho, Lo: tienTo));
        Assert.Equal(4, kho2.Dong.Sum(d => d.SoLuongTon));

        // Tra theo ô (mã vạch ô), theo biến thể (mã vạch biến thể).
        var theoO = await ts.Tra(new BoLocTra(Lo: tienTo, O: e.O1.MaVachO));
        Assert.Equal(e.O1.MaViTri, Assert.Single(theoO.Dong).MaViTri);
        var maVachBT = await db.BienThe.AsNoTracking().Where(b => b.MaBT == loA.MaBT).Select(b => b.MaVach).SingleAsync();
        Assert.Contains(loA.MaLo, (await ts.Tra(new BoLocTra(BienThe: maVachBT))).Dong.Select(d => d.MaLo));

        // Không khớp thì rỗng.
        Assert.Empty((await ts.Tra(new BoLocTra(Lo: tienTo + "-KHONG-CO"))).Dong);
    });

    [CanSqlServerFact]
    public Task BaoCao_DauKyNhapXuatCuoiKy_RiengTungKho_DoiOKhongTinh() => DungAsync(async db =>
    {
        var e = await LayOAsync(db);
        var lo = await TaoLoAsync(db, "T15", Guid.NewGuid().ToString("N")[..8]);
        void Ghi(int maViTri, int soLuong, string loai, DateTime utc) => db.BienDongTon.Add(new BienDongTon
        {
            MaLo = lo.MaLo, MaViTri = maViTri, SoLuong = soLuong, LoaiBienDong = loai, LoaiCT = "NK", MaCT = 1, MaND = Nguoi, ThoiGianUtc = utc,
        });
        Ghi(e.O1.MaViTri, 100, LoaiBienDongGiaTri.NhanHang, new DateTime(2001, 1, 10, 5, 0, 0, DateTimeKind.Utc));
        Ghi(e.O1.MaViTri, -30, LoaiBienDongGiaTri.XuatKho, new DateTime(2001, 2, 5, 5, 0, 0, DateTimeKind.Utc));
        Ghi(e.O1.MaViTri, -10, LoaiBienDongGiaTri.DoiO, new DateTime(2001, 2, 6, 5, 0, 0, DateTimeKind.Utc));
        Ghi(e.O2.MaViTri, 10, LoaiBienDongGiaTri.DoiO, new DateTime(2001, 2, 6, 5, 0, 0, DateTimeKind.Utc));
        Ghi(e.OKho2.MaViTri, 7, LoaiBienDongGiaTri.NhanChuyen, new DateTime(2001, 2, 10, 5, 0, 0, DateTimeKind.Utc));
        Ghi(e.O1.MaViTri, 5, LoaiBienDongGiaTri.NhanChuyen, new DateTime(2001, 3, 1, 12, 0, 0, DateTimeKind.Utc));   // sau kỳ: không tính
        await db.SaveChangesAsync();
        var ts = new TraCuuTonService(db);
        async Task<List<DongBaoCao>> Chay(DateTime tu, DateTime den, int? maKho = null) =>
            (await ts.BaoCao(tu, den, maKho)).Where(d => d.MaBT == lo.MaBT).ToList();

        // Tháng 2/2001: kho thứ nhất đầu kỳ 100, xuất 30, cuối kỳ 70 (đổi ô không tính); kho thứ hai nhập 7.
        var thang2 = await Chay(new DateTime(2001, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2001, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var k1 = thang2.Single(d => d.MaKho == e.O1.MaKho);
        Assert.Equal((100, 0, 30, 70), (k1.DauKy, k1.Nhap, k1.Xuat, k1.CuoiKy));
        var k2 = thang2.Single(d => d.MaKho == e.OKho2.MaKho);
        Assert.Equal((0, 7, 0, 7), (k2.DauKy, k2.Nhap, k2.Xuat, k2.CuoiKy));
        Assert.All(thang2, d => Assert.Equal(d.CuoiKy, d.DauKy + d.Nhap - d.Xuat));

        // Tháng 1/2001: kho thứ nhất nhập 100; kho thứ hai chưa có gì nên không có dòng.
        var thang1 = await Chay(new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2001, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var t1 = Assert.Single(thang1);
        Assert.Equal((0, 100, 0, 100), (t1.DauKy, t1.Nhap, t1.Xuat, t1.CuoiKy));

        // Lọc một kho chỉ trả kho đó.
        var chiKho2 = await Chay(new DateTime(2001, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2001, 3, 1, 0, 0, 0, DateTimeKind.Utc), e.OKho2.MaKho);
        Assert.Equal(e.OKho2.MaKho, Assert.Single(chiKho2).MaKho);

        // Kỳ bao cả ngày 1/3: tính thêm 5 nhận chuyển vào kho thứ nhất.
        var denThang3 = await Chay(new DateTime(2001, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2001, 3, 2, 0, 0, 0, DateTimeKind.Utc), e.O1.MaKho);
        var d3 = Assert.Single(denThang3);
        Assert.Equal((70, 5, 0, 75), (d3.DauKy, d3.Nhap, d3.Xuat, d3.CuoiKy));
    });
}
