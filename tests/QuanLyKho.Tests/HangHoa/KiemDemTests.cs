using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T3-5 (issue #19, "Khóa ô và kiểm đếm quét mã"). Nghiệp vụ ở <see cref="KiemDemService"/> chạy trên bản giả của
/// kho dữ liệu nên không cần SQL Server; riêng tiêu chí "ô đang kiểm không nhập, xuất được" kiểm trên StockService thật (cần QLK_TEST_DB).
/// </summary>
public class KiemDemTests
{
    private static GhiNhanDemVm Dem(string maO, string maHang, int? sl, bool ngoaiSo = false)
        => new() { MaVachO = maO, MaHang = maHang, SLThucTe = sl, NgoaiSo = ngoaiSo };

    // ---- Chênh lệch và tự chia tab ----

    [Theory]
    [InlineData(15, 15, 0, TabKiemDem.Khop, DongKiem.Khop, null)]
    [InlineData(15, 18, 3, TabKiemDem.Thua, DongKiem.ChoDuyet, DongKiem.ThieuHoacThua)]
    [InlineData(15, 11, -4, TabKiemDem.Thieu, DongKiem.ChoDuyet, DongKiem.ThieuHoacThua)]
    [InlineData(15, 0, -15, TabKiemDem.Thieu, DongKiem.ChoDuyet, DongKiem.ThieuHoacThua)]
    public async Task GhiSoDem_ChenhLechBangThucTeTruHeThong_TuXepTab(int heThong, int thucTe, int chenhLech, TabKiemDem tab, string trangThai, string? loaiPhatSinh)
    {
        var e = new MoiTruong();
        e.Store.Dong.Single(d => d.MaDK == MoiTruong.DongAoO1).SLHeThong = heThong;

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", thucTe), MoiTruong.NguoiDem);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = e.Store.Dong.Single(x => x.MaDK == MoiTruong.DongAoO1);
        Assert.Equal((thucTe, chenhLech, trangThai, loaiPhatSinh, (int?)MoiTruong.NguoiDem), (d.SLThucTe!.Value, d.ChenhLech!.Value, d.TrangThaiXuLy, d.LoaiPhatSinh, d.MaNguoiDem));
        var xem = (await e.Service.LayTrang(MoiTruong.MaPK, TabKiemDem.TatCa))!.Dong.Single(x => x.MaDK == MoiTruong.DongAoO1);
        Assert.Equal((chenhLech, tab), (xem.ChenhLech!.Value, xem.Tab));
        Assert.Contains(e.NhatKy, n => n.HanhDong == "Kiểm đếm" && n.MaCT == MoiTruong.MaPK);
    }

    [Fact]
    public async Task CacTab_DemDungSoDong_VaLocDungDong()
    {
        var e = new MoiTruong();
        Assert.True((await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiDem)).ThanhCong);   // khớp
        Assert.True((await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-TN", 7), MoiTruong.NguoiDem)).ThanhCong);    // thừa 2
        Assert.True((await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-02", "LO-AO", 6), MoiTruong.NguoiDem)).ThanhCong);    // thiếu 2
        // Dòng lô áo đợt 2 ở ô 2 chưa đếm.

        var trang = (await e.Service.LayTrang(MoiTruong.MaPK, TabKiemDem.Thieu))!;

        Assert.Equal((4, 1, 1, 1, 1), (trang.Dem(TabKiemDem.TatCa), trang.Dem(TabKiemDem.Khop), trang.Dem(TabKiemDem.Thua), trang.Dem(TabKiemDem.Thieu), trang.Dem(TabKiemDem.ChuaKiem)));
        Assert.Equal(MoiTruong.DongAoO2, Assert.Single(trang.DongCuaTab).MaDK);
    }

    [Fact]
    public async Task SoHeThong_LaSoDaChupLucBatDau_KhongDoiKhiDem()
    {
        var e = new MoiTruong();

        await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 99), MoiTruong.NguoiDem);

        Assert.Equal(15, e.Store.Dong.Single(d => d.MaDK == MoiTruong.DongAoO1).SLHeThong);
    }

    [Fact]
    public async Task DemLai_GhiDeSoDem_VeKhopThiXoaPhatSinh()
    {
        var e = new MoiTruong();
        await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 10), MoiTruong.NguoiDem);

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiKhac);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = e.Store.Dong.Single(x => x.MaDK == MoiTruong.DongAoO1);
        Assert.Equal((15, DongKiem.Khop, (string?)null, (int?)MoiTruong.NguoiKhac), (d.SLThucTe!.Value, d.TrangThaiXuLy, d.LoaiPhatSinh, d.MaNguoiDem));
        Assert.Equal(4, e.Store.Dong.Count);   // không sinh dòng trùng
    }

    // ---- Quét mã ----

    [Fact]
    public async Task QuetMaVachHang_OChiCoMotLoCuaHangDo_NhanRaDong()
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem(" O-01 ", " BT-TAINGHE ", 5), MoiTruong.NguoiDem);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(5, e.Store.Dong.Single(d => d.MaDK == MoiTruong.DongTaiNgheO1).SLThucTe);
    }

    [Fact]
    public async Task QuetMaVachHang_OCoNhieuLoCuaHangDo_YeuCauQuetMaLo()
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-02", "BT-AO", 5), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Contains("Hãy quét mã lô", kq.ThongBao);
        Assert.All(e.Store.Dong, d => Assert.Null(d.SLThucTe));
    }

    [Theory]
    [InlineData("O-KHONG-CO", "LO-AO", "Không tìm thấy ô")]
    [InlineData("O-01", "MA-LA", "Không tìm thấy lô hoặc mặt hàng")]
    public async Task MaKhongTonTai_BaoLoi(string maO, string maHang, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem(maO, maHang, 1), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Theory]
    [InlineData(null, "LO-AO", 1, "mã ô")]
    [InlineData("O-01", " ", 1, "mã lô")]
    [InlineData("O-01", "LO-AO", null, "Nhập số lượng thực tế")]
    [InlineData("O-01", "LO-AO", -1, "Số lượng thực tế phải từ 0")]
    public async Task ThieuHoacSaiDuLieuNhap_BaoLoi_KhongGhi(string? maO, string? maHang, int? sl, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem(maO!, maHang!, sl), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    // ---- Quét sai ô báo ngay ----

    [Theory]
    [InlineData("O-KHO-KHAC", "không thuộc kho")]
    [InlineData("O-TIEP-NHAN", "khu Tiếp nhận")]
    public async Task QuetOKhongThuocPhamVi_BaoSaiO_KhongGhi(string maO, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem(maO, "LO-AO", 3, ngoaiSo: true), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.StartsWith("Quét sai ô", kq.ThongBao);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task MotPhan_QuetONgoaiCacOCuaPhieu_BaoSaiO()
    {
        var e = new MoiTruong(loaiKiemKe: PhieuKiem.MotPhan);

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-03", "LO-AO", 3, ngoaiSo: true), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.StartsWith("Quét sai ô", kq.ThongBao);
    }

    [Fact]
    public async Task LoKhongCoOOVuaQuet_BaoNgayKemODangChuaLo_ChuaGhi()
    {
        // Lô tai nghe theo số hệ thống nằm ở ô 1; người đếm quét ô 2.
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-02", "LO-TN", 5), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.StartsWith("Quét sai ô?", kq.ThongBao);
        Assert.Contains("'Ô 01'", kq.ThongBao);
        Assert.Equal(4, e.Store.Dong.Count);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    // ---- Hàng ngoài sổ ----

    [Fact]
    public async Task XacNhanNgoaiSo_ThemDongMoiSoHeThongKhong_LaThua()
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-03", "LO-TN", 2, ngoaiSo: true), MoiTruong.NguoiDem);   // ô 3 đang trống theo sổ

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var moi = e.Store.Dong.Single(d => d.MaViTri == MoiTruong.O3);
        Assert.Equal((MoiTruong.LoTaiNghe, 0, 2, 2, DongKiem.ChoDuyet), (moi.MaLo, moi.SLHeThong, moi.SLThucTe!.Value, moi.ChenhLech!.Value, moi.TrangThaiXuLy));
        Assert.Equal(TabKiemDem.Thua, (await e.Service.LayTrang(MoiTruong.MaPK, TabKiemDem.TatCa))!.Dong.Single(d => d.MaViTri == MoiTruong.O3).Tab);
    }

    [Fact]
    public async Task NgoaiSo_SoLuongKhong_BiTuChoi()
    {
        var e = new MoiTruong();

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-03", "LO-TN", 0, ngoaiSo: true), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Equal(4, e.Store.Dong.Count);
    }

    [Fact]
    public async Task MotPhan_HangNgoaiPhamViMatHang_BiTuChoi()
    {
        var e = new MoiTruong(loaiKiemKe: PhieuKiem.MotPhan);

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-KEM", 2, ngoaiSo: true), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Contains("không nằm trong phạm vi", kq.ThongBao);
    }

    // ---- Trạng thái phiếu và dòng ----

    [Theory]
    [InlineData(PhieuKiem.ChoDuyet)]
    [InlineData(PhieuKiem.DaXuLy)]
    public async Task PhieuKhongConDangKiem_KhongDemDuoc(string trangThai)
    {
        var e = new MoiTruong(trangThai: trangThai);

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, kq.MaLoi);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task PhieuKhongTonTai_BaoKhongTimThay()
    {
        var kq = await new MoiTruong().Service.GhiNhan(999, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiDem);

        Assert.Equal((false, MaLoiDichVu.KhongTimThay), (kq.ThanhCong, kq.MaLoi));
    }

    [Fact]
    public async Task DongDaDuyet_KhongDemLaiVaKhongBoDuoc()
    {
        var e = new MoiTruong();
        var d = e.Store.Dong.Single(x => x.MaDK == MoiTruong.DongAoO1);
        d.GhiSoDem(10, MoiTruong.NguoiDem);
        d.TrangThaiXuLy = DongKiem.DaDuyet;

        var demLai = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiDem);
        var bo = await e.Service.BoDem(MoiTruong.MaPK, MoiTruong.DongAoO1, MoiTruong.NguoiDem);

        Assert.Equal((false, MaLoiDichVu.SaiTrangThai), (demLai.ThanhCong, demLai.MaLoi));
        Assert.Equal((false, MaLoiDichVu.SaiTrangThai), (bo.ThanhCong, bo.MaLoi));
        Assert.Equal(10, d.SLThucTe);
    }

    [Fact]
    public async Task BoDem_DongCoSoHeThong_VeChuaKiem_DongNgoaiSo_BiXoa()
    {
        var e = new MoiTruong();
        await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 10), MoiTruong.NguoiDem);
        await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-03", "LO-TN", 2, ngoaiSo: true), MoiTruong.NguoiDem);
        var maDongNgoaiSo = e.Store.Dong.Single(d => d.MaViTri == MoiTruong.O3).MaDK;

        Assert.True((await e.Service.BoDem(MoiTruong.MaPK, MoiTruong.DongAoO1, MoiTruong.NguoiDem)).ThanhCong);
        Assert.True((await e.Service.BoDem(MoiTruong.MaPK, maDongNgoaiSo, MoiTruong.NguoiDem)).ThanhCong);

        var d = e.Store.Dong.Single(x => x.MaDK == MoiTruong.DongAoO1);
        Assert.Equal((null, DongKiem.ChuaKiem, null, null), (d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, d.MaNguoiDem));
        Assert.DoesNotContain(e.Store.Dong, x => x.MaViTri == MoiTruong.O3);
    }

    [Fact]
    public async Task LoiGhiCsdl_TraLoiThanThien_KhongNemNgoaiLe()
    {
        var e = new MoiTruong();
        e.Store.LoiKhiLuu = true;

        var kq = await e.Service.GhiNhan(MoiTruong.MaPK, Dem("O-01", "LO-AO", 15), MoiTruong.NguoiDem);

        Assert.False(kq.ThanhCong);
        Assert.DoesNotContain(e.NhatKy, n => n.HanhDong == "Kiểm đếm");
    }

    // ---- Cấu hình bảng DongKiem (Bảng A.7) ----

    [Fact]
    public void CauHinhBang_DungCotKhoaNgoaiVaRangBuoc()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var bang = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DongKiem))!;

        Assert.Equal("DongKiem", bang.GetTableName());
        Assert.Equal(
            new[] { "LoaiPhatSinh", "MaBTDung", "MaDK", "MaLo", "MaNguoiDem", "MaNguoiDuyet", "MaPK", "MaViTri", "MaViTriDung", "NguyenNhan", "SLHeThong", "SLThucTe", "ThoiGianDuyet", "TrangThaiXuLy" },
            bang.GetProperties().Select(c => c.GetColumnName()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(new[] { "MaDK" }, bang.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        foreach (var cot in new[] { "SLThucTe", "LoaiPhatSinh", "NguyenNhan", "MaViTriDung", "MaBTDung", "MaNguoiDem", "MaNguoiDuyet", "ThoiGianDuyet" })
            Assert.True(bang.FindProperty(cot)!.IsNullable, cot);
        Assert.Equal((25, 255, 20), (bang.FindProperty("LoaiPhatSinh")!.GetMaxLength(), bang.FindProperty("NguyenNhan")!.GetMaxLength(), bang.FindProperty("TrangThaiXuLy")!.GetMaxLength()));
        Assert.Equal(DongKiem.ChuaKiem, bang.FindProperty("TrangThaiXuLy")!.GetDefaultValue());
        Assert.Equal("datetime2(0)", bang.FindProperty("ThoiGianDuyet")!.GetColumnType());
        Assert.Contains(bang.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "MaPK", "MaViTri", "MaLo" }));
        Assert.Equal(
            new[] { "BienThe:MaBTDung", "LoHang:MaLo", "NguoiDung:MaNguoiDem", "NguoiDung:MaNguoiDuyet", "PhieuKiem:MaPK", "ViTri:MaViTri", "ViTri:MaViTriDung" },
            bang.GetForeignKeys().Select(f => $"{f.PrincipalEntityType.GetTableName()}:{f.Properties[0].Name}").OrderBy(x => x, StringComparer.Ordinal).ToArray());

        var rangBuoc = bang.GetCheckConstraints().ToDictionary(c => c.Name!, c => c.Sql);
        foreach (var gt in new[] { DongKiem.ChuaKiem, DongKiem.Khop, DongKiem.ChoDuyet, DongKiem.DaDuyet, DongKiem.TuChoi })
            Assert.Contains($"N'{gt}'", rangBuoc["CK_DongKiem_TrangThaiXuLy"]);
        foreach (var gt in new[] { DongKiem.ThieuHoacThua, DongKiem.HuHongHoacHetHan, DongKiem.SaiViTri, DongKiem.SaiMaHang })
        {
            Assert.Contains($"N'{gt}'", rangBuoc["CK_DongKiem_LoaiPhatSinh"]);
            Assert.True(gt.Length <= 25);
        }
        foreach (var ten in new[] { "CK_DongKiem_SLHeThong", "CK_DongKiem_SLThucTe", "CK_DongKiem_ChuaKiem", "CK_DongKiem_PhatSinh", "CK_DongKiem_MaViTriDung", "CK_DongKiem_MaBTDung" })
            Assert.Contains(ten, rangBuoc.Keys);
    }

    // ---- Tiêu chí "Ô đang kiểm không nhập, xuất được" trên StockService và LockService thật (cần SQL Server) ----

    [CanSqlServerFact]
    public async Task ODangKiem_KhongNhapKhongXuatDuoc_ChiChungTuKiemKeDuocGhi()
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        const int maKho = 1, maND = 2;
        var o = new ViTri
        {
            TenViTri = "O-KD-" + Guid.NewGuid().ToString("N")[..8], MaKho = maKho, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
            MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 100, Rong = 100, Cao = 100, KhoiLuongToiDa = 1000,
        };
        db.ViTri.Add(o);
        var lo = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = await db.BienThe.Select(b => b.MaBT).FirstAsync() };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();

        var ton = new StockService(db);
        var dong = new DongTon(lo.MaLo, o.MaViTri, 5);
        Assert.True((await ton.GhiTang(dong, LoaiBienDong.SapXep, new ThamChieuChungTu(LoaiChungTu.NK, 1), maND)).ThanhCong);

        db.Entry(o).Property(x => x.DangKhoa).CurrentValue = true;   // trạng thái ILockService.KhoaO ghi khi bắt đầu kiểm
        await db.SaveChangesAsync();

        var nhap = await ton.GhiTang(dong, LoaiBienDong.SapXep, new ThamChieuChungTu(LoaiChungTu.NK, 2), maND);
        var xuat = await ton.GhiGiam(new DongTon(lo.MaLo, o.MaViTri, 1), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.XK, 1), maND);
        Assert.Equal((false, MaLoiDichVu.OBiKhoa), (nhap.ThanhCong, nhap.MaLoi));
        Assert.Equal((false, MaLoiDichVu.OBiKhoa), (xuat.ThanhCong, xuat.MaLoi));
        Assert.Equal(5, (await ton.LayTon(lo.MaLo, o.MaViTri))!.SoLuongTon);

        // Chứng từ kiểm kê (điều chỉnh sau kiểm, T3-7) vẫn ghi được vào ô đang khóa.
        var dieuChinh = await ton.GhiGiam(new DongTon(lo.MaLo, o.MaViTri, 1), LoaiBienDong.XuatKho, new ThamChieuChungTu(LoaiChungTu.KK, 1), maND);
        Assert.True(dieuChinh.ThanhCong, dieuChinh.ThongBao);
    }

    // ---- Môi trường thử: phiếu 1 của kho 1; ô 1 có áo (15) và tai nghe (5); ô 2 có hai lô áo (8 và 4); ô 3 trống ----

    private sealed class MoiTruong
    {
        public const int MaPK = 1, MaKho = 1;
        public const int O1 = 11, O2 = 12, O3 = 13, OTiepNhan = 19, OKhoKhac = 21;
        public const int AoThun = 101, TaiNghe = 102, Kem = 103;
        public const int LoAo = 201, LoTaiNghe = 202, LoAoDot2 = 203, LoKem = 204;
        public const int DongAoO1 = 1, DongTaiNgheO1 = 2, DongAoO2 = 3, DongAoDot2O2 = 4;
        public const int NguoiDem = 3, NguoiKhac = 4;

        public KhoGia Store { get; }
        public List<(string HanhDong, int? MaCT)> NhatKy { get; } = new();
        public KiemDemService Service { get; }

        public MoiTruong(string loaiKiemKe = PhieuKiem.ToanBo, string trangThai = PhieuKiem.DangKiem)
        {
            Store = new KhoGia(new PhieuKiemTomTat(MaPK, "KK000001", MaKho, "Kho tổng", loaiKiemKe, trangThai));
            Service = new KiemDemService(Store, new NhatKyGhiLai(NhatKy));
        }
    }

    private sealed class NhatKyGhiLai(List<(string HanhDong, int? MaCT)> dong) : IAuditService
    {
        public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
        {
            dong.Add((hanhDong, maCT));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NhatKyDto>>(Array.Empty<NhatKyDto>());
    }

    private sealed class KhoGia(PhieuKiemTomTat phieu) : IKiemDemStore
    {
        private static readonly Dictionary<string, OQuet> O = new()
        {
            ["O-01"] = new(MoiTruong.O1, "Ô 01", MoiTruong.MaKho, true), ["O-02"] = new(MoiTruong.O2, "Ô 02", MoiTruong.MaKho, true),
            ["O-03"] = new(MoiTruong.O3, "Ô 03", MoiTruong.MaKho, true), ["O-TIEP-NHAN"] = new(MoiTruong.OTiepNhan, "Khu tiếp nhận", MoiTruong.MaKho, false),
            ["O-KHO-KHAC"] = new(MoiTruong.OKhoKhac, "Ô kho khác", 2, true),
        };
        private static readonly Dictionary<string, LoQuet> Lo = new()
        {
            ["LO-AO"] = new(MoiTruong.LoAo, "LO-AO", MoiTruong.AoThun), ["LO-TN"] = new(MoiTruong.LoTaiNghe, "LO-TN", MoiTruong.TaiNghe),
            ["LO-AO-2"] = new(MoiTruong.LoAoDot2, "LO-AO-2", MoiTruong.AoThun), ["LO-KEM"] = new(MoiTruong.LoKem, "LO-KEM", MoiTruong.Kem),
        };
        private static readonly Dictionary<string, int> BienThe = new() { ["BT-AO"] = MoiTruong.AoThun, ["BT-TAINGHE"] = MoiTruong.TaiNghe, ["BT-KEM"] = MoiTruong.Kem };

        private readonly List<DongKiem> _choThem = new(), _choXoa = new();
        private int _maDK = 4;

        public List<DongKiem> Dong { get; } = new()
        {
            new() { MaDK = MoiTruong.DongAoO1, MaPK = MoiTruong.MaPK, MaViTri = MoiTruong.O1, MaLo = MoiTruong.LoAo, SLHeThong = 15 },
            new() { MaDK = MoiTruong.DongTaiNgheO1, MaPK = MoiTruong.MaPK, MaViTri = MoiTruong.O1, MaLo = MoiTruong.LoTaiNghe, SLHeThong = 5 },
            new() { MaDK = MoiTruong.DongAoO2, MaPK = MoiTruong.MaPK, MaViTri = MoiTruong.O2, MaLo = MoiTruong.LoAo, SLHeThong = 8 },
            new() { MaDK = MoiTruong.DongAoDot2O2, MaPK = MoiTruong.MaPK, MaViTri = MoiTruong.O2, MaLo = MoiTruong.LoAoDot2, SLHeThong = 4 },
        };
        public bool LoiKhiLuu { get; set; }
        public int SoLanLuu { get; private set; }

        public Task<PhieuKiemTomTat?> LayPhieu(int maPK, CancellationToken ct = default) => Task.FromResult(maPK == phieu.MaPK ? phieu : null);

        public Task<IReadOnlyList<DongKiemXem>> LayDong(int maPK, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DongKiemXem>>(Dong.Where(d => d.MaPK == maPK).Select(d =>
            {
                var o = O.Single(x => x.Value.MaViTri == d.MaViTri);
                var lo = Lo.Values.Single(x => x.MaLo == d.MaLo);
                return new DongKiemXem(d.MaDK, d.MaViTri, o.Value.TenViTri, o.Key, d.MaLo, lo.MaVachLo, lo.MaBT, $"Hàng {lo.MaBT}",
                    BienThe.Single(x => x.Value == lo.MaBT).Key, d.SLHeThong, d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, d.MaNguoiDem?.ToString());
            }).ToList());

        public Task<OQuet?> TimO(string maVachO, CancellationToken ct = default) => Task.FromResult(O.GetValueOrDefault(maVachO));
        public Task<LoQuet?> TimLo(string maVachLo, CancellationToken ct = default) => Task.FromResult(Lo.GetValueOrDefault(maVachLo));
        public Task<int?> TimBienThe(string maVach, CancellationToken ct = default) => Task.FromResult(BienThe.TryGetValue(maVach, out var m) ? m : (int?)null);
        public Task<DongKiem?> TimDong(int maPK, int maDK, CancellationToken ct = default) => Task.FromResult(Dong.FirstOrDefault(d => d.MaPK == maPK && d.MaDK == maDK));
        public void Them(DongKiem dong) => _choThem.Add(dong);
        public void Xoa(DongKiem dong) => _choXoa.Add(dong);

        public Task LuuThayDoi(CancellationToken ct = default)
        {
            SoLanLuu++;
            if (LoiKhiLuu) { _choThem.Clear(); _choXoa.Clear(); throw new InvalidOperationException("Không ghi được dòng kiểm (giả lập)."); }
            foreach (var d in _choThem) { d.MaDK = ++_maDK; Dong.Add(d); }
            foreach (var d in _choXoa) Dong.Remove(d);
            _choThem.Clear(); _choXoa.Clear();
            return Task.CompletedTask;
        }
    }
}
