using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T3-4 (issue #29, "Tạo phiếu kiểm và thành phần tham gia"). Không cần SQL Server: nghiệp vụ ở
/// <see cref="PhieuKiemService"/> chạy trên bản giả của kho dữ liệu, ILockService và IStockService, có ghi lại thứ tự gọi.
/// </summary>
public class PhieuKiemTests
{
    // ---- Quy tắc trên form (không cần CSDL) ----

    // Một form hợp lệ: kho 1, kiểm toàn bộ, một người tham gia.
    private static TaoPhieuKiemVm FormHopLe() => new()
    {
        LoaiKiemKe = PhieuKiem.ToanBo, MaKho = MoiTruong.MaKho,
        ThamGia = new() { new ThamGiaVm { MaND = MoiTruong.NguoiA } },
    };

    private static TaoPhieuKiemVm FormMotPhan(params int[] maBT)
    {
        var vm = FormHopLe();
        vm.LoaiKiemKe = PhieuKiem.MotPhan;
        vm.PhamViMaBT = maBT.ToList();
        return vm;
    }

    [Fact]
    public void FormHopLe_KhongCoLoi() => Assert.Null(FormHopLe().KiemTra());

    [Fact]
    public void ChuaChonKho_BiTuChoi()
    {
        var vm = FormHopLe(); vm.MaKho = 0;
        Assert.Equal("Chọn kho kiểm kê.", vm.KiemTra());
    }

    [Theory]
    [InlineData("Khác")]
    [InlineData(null)]
    public void LoaiKiemKeKhongHopLe_BiTuChoi(string? loai)
    {
        var vm = FormHopLe(); vm.LoaiKiemKe = loai;
        Assert.Equal("Loại kiểm kê chỉ được chọn Toàn bộ hoặc Một phần.", vm.KiemTra());
    }

    [Fact]
    public void MotPhan_ChuaChonPhamVi_BiTuChoi()
        => Assert.Equal("Kiểm kê một phần phải chọn phạm vi mặt hàng.", FormMotPhan().KiemTra());

    [Fact]
    public void MotPhan_CoPhamVi_KhongLoi() => Assert.Null(FormMotPhan(MoiTruong.AoThun).KiemTra());

    [Fact]
    public void ToanBo_MaCoPhamVi_BiTuChoi()
    {
        var vm = FormHopLe(); vm.PhamViMaBT = new() { MoiTruong.AoThun };
        Assert.Equal("Kiểm kê toàn bộ không chọn phạm vi mặt hàng.", vm.KiemTra());
    }

    [Fact]
    public void KhongCoNguoiThamGia_BiTuChoi()
    {
        var vm = FormHopLe(); vm.ThamGia!.Clear();
        Assert.Equal("Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.", vm.KiemTra());
    }

    [Fact]
    public void DongThamGiaChuaChonNguoi_BiTuChoi()
    {
        var vm = FormHopLe(); vm.ThamGia![0]!.MaND = 0;
        Assert.Equal("Chọn người cho mỗi dòng tham gia kiểm.", vm.KiemTra());
    }

    [Fact]
    public void MotNguoiThamGiaHaiLan_BiTuChoi()
    {
        var vm = FormHopLe(); vm.ThamGia!.Add(new ThamGiaVm { MaND = MoiTruong.NguoiA });
        Assert.Equal("Một người không khai báo tham gia hai lần trên cùng phiếu.", vm.KiemTra());
    }

    [Fact]
    public void ClientGuiNull_KhongNemLoi_VaBaoLoiRoRang()
    {
        // Client gửi sai (danh sách null, phần tử null) không được gây lỗi 500.
        var vm = new TaoPhieuKiemVm { MaKho = 1, LoaiKiemKe = PhieuKiem.MotPhan, PhamViMaBT = null, ThamGia = null };
        Assert.Equal("Kiểm kê một phần phải chọn phạm vi mặt hàng.", vm.KiemTra());

        vm = FormHopLe(); vm.ThamGia = new() { null, null };
        Assert.Equal("Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.", vm.KiemTra());
    }

    [Fact]
    public void ThamChieuVaDienGiai_CatKhoangTrangTruocKhiKiemDoDai()
    {
        var vm = FormHopLe();
        vm.ThamChieu = "  " + new string('a', 100) + "  ";   // đúng 100 ký tự sau khi cắt: hợp lệ
        vm.DienGiai = "   ";                                 // chỉ khoảng trắng: coi như để trống
        Assert.Null(vm.KiemTra());
        Assert.Equal(100, vm.ThamChieu!.Length);
        Assert.Null(vm.DienGiai);

        vm.ThamChieu = new string('a', 101);
        Assert.Equal("Tham chiếu tối đa 100 ký tự.", vm.KiemTra());

        vm.ThamChieu = null; vm.DienGiai = new string('a', 501);
        Assert.Equal("Diễn giải tối đa 500 ký tự.", vm.KiemTra());
    }

    // ---- Tạo phiếu: khóa ô, chụp số hệ thống, số phiếu ----

    [Fact]
    public async Task ToanBo_KhoaMoiOLuuTruKeCaOTrong_ChupDungSoHeThong_SoPhieuKK()
    {
        var e = new MoiTruong();
        var vm = FormHopLe();
        vm.ThamChieu = " BB-01 "; vm.ThamGia!.Add(new ThamGiaVm { MaND = MoiTruong.NguoiB, GhiChu = " Tổ trưởng " });

        var kq = await e.Service.Tao(vm, MoiTruong.NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var p = Assert.Single(e.Store.DaLuu);
        Assert.Equal("KK000001", p.SoPhieu);
        Assert.Matches("^KK[0-9]{6}$", p.SoPhieu);
        Assert.Equal(PhieuKiem.DangKiem, p.TrangThai);
        Assert.Equal(MoiTruong.NguoiLap, p.MaNguoiLap);
        Assert.Equal("BB-01", p.ThamChieu);
        Assert.InRange(p.ThoiGianBatDau, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        // Khóa cả ba ô lưu trữ (kể cả ô trống), không khóa khu Tiếp nhận.
        Assert.Equal(new[] { MoiTruong.O1, MoiTruong.O2, MoiTruong.OTrong }, e.Khoa.DangKhoaO.OrderBy(x => x).ToArray());
        Assert.DoesNotContain(MoiTruong.OTiepNhan, e.Khoa.DangKhoaO);

        // Số hệ thống đúng theo IStockService, không có hàng ở khu Tiếp nhận.
        Assert.Equal(
            new[] { (MoiTruong.AoThun, MoiTruong.LoAo, MoiTruong.O1, 15), (MoiTruong.TaiNghe, MoiTruong.LoTaiNghe, MoiTruong.O1, 5), (MoiTruong.AoThun, MoiTruong.LoAo, MoiTruong.O2, 8) },
            p.ChiTiet.Select(d => (d.MaBT, d.MaLo, d.MaViTri, d.SoLuongHeThong)).OrderBy(x => x.MaViTri).ThenBy(x => x.MaLo).ToArray());
        Assert.DoesNotContain(p.ChiTiet, d => d.MaViTri == MoiTruong.OTiepNhan);

        Assert.Equal(new[] { (MoiTruong.NguoiA, (string?)null), (MoiTruong.NguoiB, "Tổ trưởng") }, p.ThamGia.Select(t => (t.MaND, t.GhiChu)).ToArray());
        Assert.Contains(e.NhatKy.Dong, d => d.MaND == MoiTruong.NguoiLap && d.HanhDong == "Tạo phiếu kiểm");
    }

    [Fact]
    public async Task ToanBo_KhoaHetORoiMoiChupTon()
    {
        var e = new MoiTruong();

        Assert.True((await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap)).ThanhCong);

        var lanKhoaCuoi = e.NhatKyGoi.FindLastIndex(x => x.StartsWith("KhoaO:"));
        var lanDocTonDau = e.NhatKyGoi.FindIndex(x => x.StartsWith("LayTon:"));
        var lanLuu = e.NhatKyGoi.IndexOf("Luu");
        Assert.True(lanKhoaCuoi < lanDocTonDau, "Phải khóa xong mọi ô rồi mới đọc tồn: " + string.Join(", ", e.NhatKyGoi));
        Assert.True(lanDocTonDau < lanLuu);
    }

    [Fact]
    public async Task MotPhan_ChiKhoaOChuaHangDaChon_ChiGhiDongCuaHangDo()
    {
        var e = new MoiTruong();

        var kq = await e.Service.Tao(FormMotPhan(MoiTruong.TaiNghe), MoiTruong.NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(new[] { MoiTruong.O1 }, e.Khoa.DangKhoaO.ToArray());          // tai nghe chỉ có ở ô 1
        var d = Assert.Single(e.Store.DaLuu[0].ChiTiet);                             // áo thun cùng ô không vào phiếu
        Assert.Equal((MoiTruong.TaiNghe, MoiTruong.LoTaiNghe, MoiTruong.O1, 5), (d.MaBT, d.MaLo, d.MaViTri, d.SoLuongHeThong));
    }

    [Fact]
    public async Task MotPhan_SoHeThongLaSoSauKhiKhoa_KhongPhaiSoLucDoTruoc()
    {
        // Giữa lúc dò phạm vi và lúc khóa xong, một phiếu xuất kịp lấy 2 tai nghe: phiếu kiểm phải ghi 3, không ghi 5.
        var e = new MoiTruong();
        e.Khoa.KhiKhoa = maViTri => { if (maViTri == MoiTruong.O1) e.Ton.Dat(MoiTruong.LoTaiNghe, MoiTruong.O1, 3); };

        var kq = await e.Service.Tao(FormMotPhan(MoiTruong.TaiNghe), MoiTruong.NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(3, Assert.Single(e.Store.DaLuu[0].ChiTiet).SoLuongHeThong);
        var lanKhoa = e.NhatKyGoi.IndexOf($"KhoaO:{MoiTruong.O1}");
        Assert.Contains(e.NhatKyGoi.Skip(lanKhoa + 1), x => x == $"LayTon:{MoiTruong.O1}");
    }

    [Fact]
    public async Task MotPhan_SauKhiKhoaKhongConTon_MoLaiOVaBaoLoi()
    {
        var e = new MoiTruong();
        e.Khoa.KhiKhoa = _ => e.Ton.Dat(MoiTruong.LoTaiNghe, MoiTruong.O1, 0);

        var kq = await e.Service.Tao(FormMotPhan(MoiTruong.TaiNghe), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Empty(e.Khoa.DangKhoaO);
        Assert.Empty(e.Store.DaLuu);
    }

    [Fact]
    public async Task MotPhan_HangDaChonKhongCoTonTrongKho_TuChoi_KhongKhoaONao()
    {
        var e = new MoiTruong();

        var kq = await e.Service.Tao(FormMotPhan(MoiTruong.HangKhongTon), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.DoesNotContain(e.NhatKyGoi, x => x.StartsWith("KhoaO:"));
        Assert.Empty(e.Store.DaLuu);
    }

    [Fact]
    public async Task MotPhan_ChuaChonPhamVi_TuChoi_KhongKhoaONao()
    {
        var e = new MoiTruong();

        var kq = await e.Service.Tao(FormMotPhan(), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Kiểm kê một phần phải chọn phạm vi mặt hàng.", kq.ThongBao);
        Assert.Empty(e.NhatKyGoi);
        Assert.Empty(e.Store.DaLuu);
    }

    [Fact]
    public async Task ToanBo_KhoTrong_VanTaoDuocPhieu_VaKhoaO()
    {
        var e = new MoiTruong();
        e.Ton.XoaHet();

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Empty(Assert.Single(e.Store.DaLuu).ChiTiet);
        Assert.Equal(3, e.Khoa.DangKhoaO.Count);
    }

    // ---- Lỗi giữa chừng: không để lại ô khóa ----

    [Fact]
    public async Task OThuHaiKhoaLoi_OThuNhatDuocMoLai_KhongCoPhieu()
    {
        var e = new MoiTruong();
        e.Khoa.TuChoi[MoiTruong.O2] = KetQua.Loi("Ô 'O2' còn 4 hàng giữ chỗ cho phiếu xuất chưa hoàn tất, chưa khóa được.", MaLoiDichVu.SaiTrangThai);

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.SaiTrangThai, kq.MaLoi);
        Assert.Contains("giữ chỗ", kq.ThongBao);
        Assert.Contains($"MoO:{MoiTruong.O1}", e.NhatKyGoi);
        Assert.Empty(e.Khoa.DangKhoaO);
        Assert.Empty(e.Store.DaLuu);
        Assert.DoesNotContain("Luu", e.NhatKyGoi);
    }

    [Fact]
    public async Task LoiGhiCsdl_MoiODuocMoLai_KhongCoPhieu()
    {
        var e = new MoiTruong();
        e.Store.LoiKhiLuu = new DbUpdateException("Mất kết nối CSDL (giả lập).");

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains("đã được mở lại", kq.ThongBao);
        Assert.Empty(e.Khoa.DangKhoaO);
        Assert.Empty(e.Store.DaLuu);
        Assert.Equal(3, e.NhatKyGoi.Count(x => x.StartsWith("MoO:")));
        Assert.DoesNotContain(e.NhatKy.Dong, d => d.HanhDong == "Tạo phiếu kiểm");
    }

    [Fact]
    public async Task LoiKhiChupTonSauKhiKhoa_MoiODuocMoLai()
    {
        var e = new MoiTruong();
        e.Ton.LoiKhiDoc = new InvalidOperationException("Lỗi đọc tồn (giả lập).");

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Empty(e.Khoa.DangKhoaO);
        Assert.Empty(e.Store.DaLuu);
    }

    [Fact]
    public async Task MoLaiOThatBai_ThongBaoNeuRoOCanMoTay()
    {
        var e = new MoiTruong();
        e.Store.LoiKhiLuu = new DbUpdateException("Lỗi (giả lập).");
        e.Khoa.MoLoi.Add(MoiTruong.O2);

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Contains($"mã {MoiTruong.O2}", kq.ThongBao);
        Assert.Equal(new[] { MoiTruong.O2 }, e.Khoa.DangKhoaO.ToArray());
    }

    [Fact]
    public async Task NhatKyLoi_PhieuVanDuocTao_OVanKhoa()
    {
        var e = new MoiTruong(nhatKyLoi: true);

        var kq = await e.Service.Tao(FormHopLe(), MoiTruong.NguoiLap);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Single(e.Store.DaLuu);
        Assert.Equal(3, e.Khoa.DangKhoaO.Count);
    }

    // ---- Danh mục: kho và người tham gia ----

    [Fact]
    public async Task NguoiThamGiaKhongTonTai_TuChoi_KhongKhoaONao()
    {
        var e = new MoiTruong();
        var vm = FormHopLe(); vm.ThamGia!.Add(new ThamGiaVm { MaND = 999 });

        var kq = await e.Service.Tao(vm, MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Người tham gia kiểm phải là nhân viên có trong danh mục và còn hoạt động.", kq.ThongBao);
        Assert.Empty(e.NhatKyGoi);
    }

    [Fact]
    public async Task NguoiThamGiaNgungHoatDong_TuChoi()
    {
        var e = new MoiTruong();
        var vm = FormHopLe(); vm.ThamGia = new() { new ThamGiaVm { MaND = MoiTruong.NguoiNgung } };

        var kq = await e.Service.Tao(vm, MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Empty(e.NhatKyGoi);
        Assert.Empty(e.Store.DaLuu);
    }

    [Fact]
    public async Task KhoNgungHoatDong_TuChoi_KhongKhoaONao()
    {
        var e = new MoiTruong();
        var vm = FormHopLe(); vm.MaKho = MoiTruong.MaKhoNgung;

        var kq = await e.Service.Tao(vm, MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Kho kiểm kê không tồn tại hoặc đã ngừng hoạt động.", kq.ThongBao);
        Assert.Empty(e.NhatKyGoi);
    }

    [Fact]
    public async Task MatHangTrongPhamViKhongTonTai_TuChoi()
    {
        var e = new MoiTruong();

        var kq = await e.Service.Tao(FormMotPhan(MoiTruong.TaiNghe, 999), MoiTruong.NguoiLap);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Có mặt hàng trong phạm vi kiểm không tồn tại.", kq.ThongBao);
        Assert.Empty(e.NhatKyGoi);
    }

    // ---- Cấu hình bảng (Bảng A.7, Bảng 2.9) ----

    [Fact]
    public void CauHinhBang_DungTenCotKhoaNgoaiVaMienGiaTri()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);

        // Ràng buộc CHECK và giá trị mặc định chỉ có ở mô hình lúc thiết kế, không có ở mô hình tối ưu cho lúc chạy.
        var moHinh = db.GetService<IDesignTimeModel>().Model;
        var phieu = moHinh.FindEntityType(typeof(PhieuKiem))!;
        Assert.Equal("PhieuKiem", phieu.GetTableName());
        Assert.Equal(
            new[] { "DienGiai", "LoaiKiemKe", "MaKho", "MaNguoiLap", "MaPK", "SoPhieu", "ThamChieu", "ThoiGianBatDau", "TrangThai" },
            phieu.GetProperties().Select(c => c.GetColumnName()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal("varchar(20)", phieu.FindProperty("SoPhieu")!.GetColumnType());
        Assert.Equal(10, phieu.FindProperty("LoaiKiemKe")!.GetMaxLength());
        Assert.Equal("datetime2(0)", phieu.FindProperty("ThoiGianBatDau")!.GetColumnType());
        Assert.Equal(PhieuKiem.DangKiem, phieu.FindProperty("TrangThai")!.GetDefaultValue());
        Assert.Contains(phieu.GetIndexes(), i => i.IsUnique && i.Properties.Any(c => c.Name == "SoPhieu"));
        Assert.Equal(new[] { "Kho", "NguoiDung" },
            phieu.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).Distinct().OrderBy(x => x).ToArray());

        var trangThai = phieu.GetCheckConstraints().Single(c => c.Name == "CK_PhieuKiem_TrangThai").Sql;
        foreach (var gt in new[] { PhieuKiem.DangKiem, PhieuKiem.ChoDuyet, PhieuKiem.DaXuLy }) Assert.Contains($"N'{gt}'", trangThai);
        var loai = phieu.GetCheckConstraints().Single(c => c.Name == "CK_PhieuKiem_LoaiKiemKe").Sql;
        foreach (var gt in new[] { PhieuKiem.ToanBo, PhieuKiem.MotPhan }) Assert.Contains($"N'{gt}'", loai);

        var chiTiet = moHinh.FindEntityType(typeof(ChiTietPhieuKiem))!;
        Assert.Equal("ChiTietPhieuKiem", chiTiet.GetTableName());
        Assert.Equal(new[] { "BienThe", "LoHang", "PhieuKiem", "ViTri" },
            chiTiet.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());

        var thamGia = moHinh.FindEntityType(typeof(ThamGiaKiem))!;
        Assert.Equal("ThamGiaKiem", thamGia.GetTableName());
        Assert.Equal(new[] { "MaPK", "MaND" }, thamGia.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "GhiChu", "MaND", "MaPK" }, thamGia.GetProperties().Select(c => c.GetColumnName()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(255, thamGia.FindProperty("GhiChu")!.GetMaxLength());
        Assert.Equal(new[] { "NguoiDung", "PhieuKiem" },
            thamGia.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());
    }

    [Theory]
    [InlineData(PhieuKiem.DangKiem, "nhan-lam")]
    [InlineData(PhieuKiem.ChoDuyet, "nhan-chu-y")]
    [InlineData(PhieuKiem.DaXuLy, "nhan-tot")]
    public void HuyHieuTrangThai_DungLopGiaoDien(string trangThai, string lop) => Assert.Equal(lop, PhieuKiemHienThi.LopNhan(trangThai));

    // ---- Môi trường thử: kho 1 có 3 ô lưu trữ (ô 1, ô 2 có hàng; ô 3 trống) và 1 ô tiếp nhận có hàng ----

    private sealed class MoiTruong
    {
        public const int MaKho = 1, MaKhoNgung = 2;
        public const int O1 = 11, O2 = 12, OTrong = 13, OTiepNhan = 19;
        public const int AoThun = 101, TaiNghe = 102, HangKhongTon = 103;
        public const int LoAo = 201, LoTaiNghe = 202, LoChoXep = 203;
        public const int NguoiLap = 2, NguoiA = 3, NguoiB = 4, NguoiNgung = 5;

        /// <summary>Thứ tự các lần gọi KhoaO, MoO, LayTon, Luu để kiểm "khóa trước, chụp sau".</summary>
        public List<string> NhatKyGoi { get; } = new();
        public KhoGia Store { get; }
        public KhoaGia Khoa { get; }
        public TonGia Ton { get; }
        public NhatKyGiaCoLoi NhatKy { get; }
        public PhieuKiemService Service { get; }

        public MoiTruong(bool nhatKyLoi = false)
        {
            Store = new KhoGia(NhatKyGoi);
            Khoa = new KhoaGia(NhatKyGoi);
            Ton = new TonGia(NhatKyGoi);
            NhatKy = new NhatKyGiaCoLoi(nhatKyLoi);
            Ton.Dat(LoAo, O1, 15);
            Ton.Dat(LoTaiNghe, O1, 5);
            Ton.Dat(LoAo, O2, 8);
            Ton.Dat(LoChoXep, OTiepNhan, 30);   // hàng ở khu Tiếp nhận: không thuộc phạm vi kiểm
            Service = new PhieuKiemService(Store, Khoa, Ton, NhatKy);
        }
    }

    private sealed class KhoGia(List<string> nhatKyGoi) : IPhieuKiemStore
    {
        private static readonly Dictionary<int, int> BienTheCuaLo = new()
        {
            [MoiTruong.LoAo] = MoiTruong.AoThun, [MoiTruong.LoTaiNghe] = MoiTruong.TaiNghe, [MoiTruong.LoChoXep] = MoiTruong.AoThun,
        };

        public List<PhieuKiem> DaLuu { get; } = new();
        public Exception? LoiKhiLuu { get; set; }

        public Task<bool> KhoHoatDong(int maKho, CancellationToken ct = default) => Task.FromResult(maKho == MoiTruong.MaKho);

        // Chỉ ô loại Lưu trữ của kho, kể cả ô trống; ô Tiếp nhận (MoiTruong.OTiepNhan) không nằm trong danh sách.
        public Task<IReadOnlyList<int>> LayOLuuTru(int maKho, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<int>>(maKho == MoiTruong.MaKho ? new[] { MoiTruong.O1, MoiTruong.O2, MoiTruong.OTrong } : Array.Empty<int>());

        public Task<int> DemNguoiHoatDong(IReadOnlyCollection<int> maND, CancellationToken ct = default)
            => Task.FromResult(maND.Count(m => m is MoiTruong.NguoiLap or MoiTruong.NguoiA or MoiTruong.NguoiB));

        public Task<int> DemBienThe(IReadOnlyCollection<int> maBT, CancellationToken ct = default)
            => Task.FromResult(maBT.Count(m => m is MoiTruong.AoThun or MoiTruong.TaiNghe or MoiTruong.HangKhongTon));

        public Task<IReadOnlyDictionary<int, int>> LayBienTheCuaLo(IReadOnlyCollection<int> maLo, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<int, int>>(maLo.ToDictionary(m => m, m => BienTheCuaLo[m]));

        public Task Luu(PhieuKiem phieu, CancellationToken ct = default)
        {
            nhatKyGoi.Add("Luu");
            if (LoiKhiLuu is not null) throw LoiKhiLuu;
            phieu.MaPK = DaLuu.Count + 1;
            phieu.SoPhieu = $"KK{phieu.MaPK:D6}";
            DaLuu.Add(phieu);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PhieuKiemDong>> DanhSach(int toiDa, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PhieuKiemXem?> Xem(int maPK, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DanhMucChonPhieuKiem> LayDanhMucChon(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class KhoaGia(List<string> nhatKyGoi) : ILockService
    {
        public HashSet<int> DangKhoaO { get; } = new();
        public Dictionary<int, KetQua> TuChoi { get; } = new();
        public HashSet<int> MoLoi { get; } = new();
        public Action<int>? KhiKhoa { get; set; }

        public Task<KetQua> KhoaO(int maViTri, int maND, CancellationToken ct = default)
        {
            nhatKyGoi.Add($"KhoaO:{maViTri}");
            if (TuChoi.TryGetValue(maViTri, out var loi)) return Task.FromResult(loi);
            if (!DangKhoaO.Add(maViTri)) return Task.FromResult(KetQua.Loi("Ô đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa));
            KhiKhoa?.Invoke(maViTri);
            return Task.FromResult(KetQua.Ok());
        }

        public Task<KetQua> MoO(int maViTri, int maND, CancellationToken ct = default)
        {
            nhatKyGoi.Add($"MoO:{maViTri}");
            if (MoLoi.Contains(maViTri)) return Task.FromResult(KetQua.Loi("Không mở được ô (giả lập)."));
            DangKhoaO.Remove(maViTri);
            return Task.FromResult(KetQua.Ok());
        }

        public Task<bool> DangKhoa(int maViTri, CancellationToken ct = default) => Task.FromResult(DangKhoaO.Contains(maViTri));
    }

    /// <summary>Bản giả IStockService chỉ phục vụ đọc tồn; các thao tác ghi tồn không được gọi khi tạo phiếu kiểm.</summary>
    private sealed class TonGia(List<string> nhatKyGoi) : IStockService
    {
        private readonly Dictionary<(int Lo, int ViTri), int> _ton = new();
        public Exception? LoiKhiDoc { get; set; }

        public void Dat(int maLo, int maViTri, int soLuong) => _ton[(maLo, maViTri)] = soLuong;
        public void XoaHet() => _ton.Clear();

        public Task<IReadOnlyList<TonKhoDto>> LayTonTheoViTri(int maViTri, CancellationToken ct = default)
        {
            nhatKyGoi.Add($"LayTon:{maViTri}");
            if (LoiKhiDoc is not null) throw LoiKhiDoc;
            return Task.FromResult<IReadOnlyList<TonKhoDto>>(_ton.Where(x => x.Key.ViTri == maViTri && x.Value > 0)
                .Select(x => new TonKhoDto(x.Key.Lo, x.Key.ViTri, x.Value, 0)).OrderBy(x => x.MaLo).ToList());
        }

        public Task<TonKhoDto?> LayTon(int maLo, int maViTri, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> GiuCho(DongTon dong, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> TraCho(DongTon dong, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> GhiTang(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> GhiGiam(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, bool tuGiuCho = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<KetQua> ChuyenO(int maLo, int maViTriNguon, int maViTriDich, int soLuong, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ChenhLechSoCai>> DoiChieuSoCai(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class NhatKyGiaCoLoi(bool luonLoi) : IAuditService
    {
        public List<(int MaND, string HanhDong, int? MaCT)> Dong { get; } = new();

        public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
        {
            if (luonLoi) throw new InvalidOperationException("Không ghi được nhật ký (giả lập).");
            Dong.Add((maND, hanhDong, maCT));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NhatKyDto>>(Array.Empty<NhatKyDto>());
    }
}
