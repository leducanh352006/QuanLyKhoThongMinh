using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T3-6 (issue #12, "Ghi nhận phát sinh"). Nghiệp vụ ở <see cref="PhatSinhService"/> chạy trên bản giả của kho dữ liệu
/// và ICapacityService nên không cần SQL Server; riêng bài đọc ghi qua EF thật và các CHECK của DongKiem cần QLK_TEST_DB.
/// </summary>
public class PhatSinhTests
{
    private static PhanLoaiVm Vm(int maDK, string? loai, string? nguyenNhan = "Nhân viên lấy nhầm", int? oDung = null, int? btDung = null)
        => new() { MaDK = maDK, LoaiPhatSinh = loai, NguyenNhan = nguyenNhan, MaViTriDung = oDung, MaBTDung = btDung };

    // ---- Phân loại từng loại ----

    [Fact]
    public async Task ThieuHoacThua_GhiLoaiVaNguyenNhan_DongThanhDaPhanLoai()
    {
        var e = new MoiTruong();
        Assert.False(e.Store.Dong(MoiTruong.DongThieu).DaPhanLoai);   // vừa đếm: loại mặc định nhưng chưa có nguyên nhân

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua, "  Kiểm đếm sót, tìm thấy ở kệ bên  "), MoiTruong.NguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = e.Store.Dong(MoiTruong.DongThieu);
        Assert.Equal((DongKiem.ThieuHoacThua, "Kiểm đếm sót, tìm thấy ở kệ bên", (int?)null, (int?)null, true),
            (d.LoaiPhatSinh, d.NguyenNhan, d.MaViTriDung, d.MaBTDung, d.DaPhanLoai));
        Assert.Equal(DongKiem.ChoDuyet, d.TrangThaiXuLy);
        Assert.Contains(e.NhatKy, n => n.HanhDong == "Ghi nhận phát sinh" && n.MaCT == MoiTruong.MaPK);
    }

    [Fact]
    public async Task DongThua_GhiThieuHoacThua_DuocPhanLoai()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThua, DongKiem.ThieuHoacThua, "Nhập thiếu phiếu"), MoiTruong.NguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.True(e.Store.Dong(MoiTruong.DongThua).DaPhanLoai);
    }

    [Fact]
    public async Task HuHongHoacHetHan_ChiChoDongThieu()
    {
        var e = new MoiTruong();

        var thieu = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.HuHongHoacHetHan, "4 chiếc hỏng do ẩm"), MoiTruong.NguoiDuyet);
        var thua = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThua, DongKiem.HuHongHoacHetHan, "x"), MoiTruong.NguoiDuyet);

        Assert.True(thieu.ThanhCong, thieu.ThongBao);
        Assert.False(thua.ThanhCong);
        Assert.Contains("chỉ dành cho dòng thiếu", thua.ThongBao);
        Assert.False(e.Store.Dong(MoiTruong.DongThua).DaPhanLoai);
    }

    [Fact]
    public async Task SaiViTri_GoiSucChuaVoiSoHangThieu_VaLuuODung()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "Xếp nhầm sang ô bên cạnh", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = e.Store.Dong(MoiTruong.DongThieu);
        Assert.Equal((DongKiem.SaiViTri, (int?)MoiTruong.O13, (int?)null, true), (d.LoaiPhatSinh, d.MaViTriDung, d.MaBTDung, d.DaPhanLoai));
        var goi = Assert.Single(e.SucChua.Goi);
        Assert.Equal((MoiTruong.O13, MoiTruong.AoThun, 4), goi);   // thiếu 4 đơn vị của áo thun
    }

    [Fact]
    public async Task SaiViTri_DongThua_BiTuChoi()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThua, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("chỉ dành cho dòng thiếu", kq.ThongBao);
        Assert.Empty(e.SucChua.Goi);
    }

    [Theory]
    [InlineData(null, "Chọn ô đúng")]
    [InlineData(MoiTruong.O11, "khác ô đang kiểm")]
    [InlineData(999, "không tồn tại")]
    [InlineData(MoiTruong.OKhoKhac, "không thuộc kho")]
    [InlineData(MoiTruong.OTiepNhan, "không phải ô Lưu trữ")]
    [InlineData(MoiTruong.ONgung, "ngừng hoạt động")]
    public async Task SaiViTri_ODungKhongHopLe_BiTuChoi_KhongGhi(int? oDung, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: oDung), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
        Assert.False(e.Store.Dong(MoiTruong.DongThieu).DaPhanLoai);
    }

    [Fact]
    public async Task SaiViTri_OKhongDuSucChua_BiTuChoiKemLyDoCuaICapacityService()
    {
        var e = new MoiTruong();
        e.SucChua.TraVe[MoiTruong.O13] = new KetQuaSucChua(false, 1, 100, 5, "Ô 'Ô 13' chỉ còn chỗ cho 1 đơn vị.", MaLoiDichVu.VuotSucChua);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.VuotSucChua, kq.MaLoi);
        Assert.Contains("chỉ còn chỗ cho 1 đơn vị", kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task SaiViTri_OKhongLotLong3Chieu_BiTuChoi()
    {
        var e = new MoiTruong();
        e.SucChua.TraVe[MoiTruong.O13] = new KetQuaSucChua(false, 0, 100, 5, "Kích thước hàng (30×20×10 cm) vượt lòng ô 'Ô 13' (10×10×10 cm), không đặt vừa.", MaLoiDichVu.VuotSucChua);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("vượt lòng ô", kq.ThongBao);
    }

    [Fact]
    public async Task SaiViTri_OKhoaBoiPhieuKhac_BiTuChoi_VoiMotPhanOngoaiPhamVi()
    {
        // Phiếu Một phần chỉ có dòng ở ô 11 và 12; ô 13 khóa vì phiếu khác nên không dùng làm ô đúng được.
        var e = new MoiTruong(loaiKiemKe: PhieuKiem.MotPhan);
        e.Store.ODangKhoa.Add(MoiTruong.O13);
        e.SucChua.TraVe[MoiTruong.O13] = new KetQuaSucChua(false, 0, 0, 0, "Ô 'Ô 13' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.OBiKhoa, kq.MaLoi);
    }

    [Fact]
    public async Task SaiViTri_OKhoaBoiChinhPhieuToanBo_VanChoPhep_VaKiemLotLongTrucTiep()
    {
        // Kiểm Toàn bộ khóa mọi ô Lưu trữ của kho, kể cả ô đúng. ICapacityService từ chối ô khóa, nên chỉ kiểm lọt lòng 3 chiều tại đây.
        var e = new MoiTruong();
        e.Store.ODangKhoa.Add(MoiTruong.O13);
        e.SucChua.TraVe[MoiTruong.O13] = new KetQuaSucChua(false, 0, 0, 0, "Ô 'Ô 13' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "Xếp nhầm", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.Equal(MoiTruong.O13, e.Store.Dong(MoiTruong.DongThieu).MaViTriDung);

        // Áo thun 20×20×5 không lọt ô nhỏ 10×10×10 dù ô đang khóa bởi chính phiếu này: vẫn bị chặn.
        e.Store.ODangKhoa.Add(MoiTruong.ONho);
        e.SucChua.TraVe[MoiTruong.ONho] = new KetQuaSucChua(false, 0, 0, 0, "Ô 'Ô nhỏ' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);
        var nho = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "Xếp nhầm", oDung: MoiTruong.ONho), MoiTruong.NguoiDuyet);
        Assert.False(nho.ThanhCong);
        Assert.Contains("vượt lòng ô", nho.ThongBao);
    }

    [Fact]
    public async Task SaiMaHang_LuuMaHangDung_VaBoOTrongDung()
    {
        var e = new MoiTruong();
        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiMaHang, "Gắn nhầm nhãn lô", btDung: MoiTruong.TaiNghe), MoiTruong.NguoiDuyet);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = e.Store.Dong(MoiTruong.DongThieu);
        Assert.Equal((DongKiem.SaiMaHang, (int?)null, (int?)MoiTruong.TaiNghe, true), (d.LoaiPhatSinh, d.MaViTriDung, d.MaBTDung, d.DaPhanLoai));
    }

    [Theory]
    [InlineData(null, "Chọn mã hàng đúng")]
    [InlineData(MoiTruong.AoThun, "khác mã hàng")]
    [InlineData(999, "không tồn tại")]
    public async Task SaiMaHang_MaHangDungKhongHopLe_BiTuChoi(int? btDung, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiMaHang, "x", btDung: btDung), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task DoiLoaiTuSaiViTriSangThieuHoacThua_XoaOTrongDung()
    {
        var e = new MoiTruong();
        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.SaiViTri, "x", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua, "Hóa ra mất thật", oDung: MoiTruong.O13), MoiTruong.NguoiDuyet);

        var d = e.Store.Dong(MoiTruong.DongThieu);
        Assert.Equal((DongKiem.ThieuHoacThua, (int?)null, (int?)null), (d.LoaiPhatSinh, d.MaViTriDung, d.MaBTDung));
    }

    // ---- Nhập thiếu, dòng không phân loại được ----

    [Theory]
    [InlineData(null, "Nhập nguyên nhân")]
    [InlineData("", "Nhập nguyên nhân")]
    [InlineData("   ", "Nhập nguyên nhân")]
    public async Task NguyenNhanTrong_BiTuChoi_DongVanChuaPhanLoai(string? nguyenNhan, string thongBao)
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua, nguyenNhan), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains(thongBao, kq.ThongBao);
        Assert.False(e.Store.Dong(MoiTruong.DongThieu).DaPhanLoai);
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task NguyenNhanQua255KyTu_BiTuChoi()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua, new string('a', 256)), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("tối đa 255", kq.ThongBao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Khác")]
    public async Task LoaiKhongHopLe_BiTuChoi(string? loai)
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, loai), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("Chọn loại phát sinh", kq.ThongBao);
    }

    [Fact]
    public async Task DongKhop_KhongCanPhanLoai()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongKhop, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("khớp", kq.ThongBao);
    }

    [Fact]
    public async Task DongChuaDem_ChuaCoChenhLech_BiTuChoi()
    {
        var e = new MoiTruong();

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongChuaDem, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.Contains("chưa đếm", kq.ThongBao);
    }

    [Fact]
    public async Task DongDaDuyet_KhongPhanLoaiLai()
    {
        var e = new MoiTruong();
        e.Store.Dong(MoiTruong.DongThieu).TrangThaiXuLy = DongKiem.DaDuyet;

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.Equal((false, MaLoiDichVu.SaiTrangThai), (kq.ThanhCong, kq.MaLoi));
    }

    [Theory]
    [InlineData(PhieuKiem.ChoDuyet)]
    [InlineData(PhieuKiem.DaXuLy)]
    public async Task PhieuKhongConDangKiem_KhongGhiNhanDuoc(string trangThai)
    {
        var e = new MoiTruong(trangThai: trangThai);

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.Equal((false, MaLoiDichVu.SaiTrangThai), (kq.ThanhCong, kq.MaLoi));
        Assert.Equal(0, e.Store.SoLanLuu);
    }

    [Fact]
    public async Task PhieuHoacDongKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruong();

        var phieu = await e.Service.PhanLoai(999, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);
        var dong = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(999, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.Equal((false, MaLoiDichVu.KhongTimThay), (phieu.ThanhCong, phieu.MaLoi));
        Assert.Equal((false, MaLoiDichVu.KhongTimThay), (dong.ThanhCong, dong.MaLoi));
    }

    [Fact]
    public async Task LoiGhiCsdl_TraLoiThanThien_KhongGhiNhatKy()
    {
        var e = new MoiTruong();
        e.Store.LoiKhiLuu = true;

        var kq = await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        Assert.False(kq.ThanhCong);
        Assert.DoesNotContain(e.NhatKy, n => n.HanhDong == "Ghi nhận phát sinh");
    }

    // ---- Tiêu chí: dòng lệch chưa phân loại không qua được bước duyệt ----

    [Fact]
    public async Task PhieuConDongLechChuaPhanLoai_ChuaSanSangDuyet_PhanLoaiHetMoiDuoc()
    {
        var e = new MoiTruong();

        var truoc = await e.Service.KiemTraSanSangDuyet(MoiTruong.MaPK);
        Assert.False(truoc.ThanhCong);
        Assert.Contains("Còn 2 dòng lệch chưa phân loại", truoc.ThongBao);   // dòng khớp và dòng chưa đếm không tính

        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);
        var giua = await e.Service.KiemTraSanSangDuyet(MoiTruong.MaPK);
        Assert.False(giua.ThanhCong);
        Assert.Contains("Còn 1 dòng", giua.ThongBao);

        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThua, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);
        Assert.True((await e.Service.KiemTraSanSangDuyet(MoiTruong.MaPK)).ThanhCong);
    }

    [Fact]
    public void KiemTraDongDuyetDuoc_DongLechChuaPhanLoaiBiChan_DongKhopVaDongDaPhanLoaiQua()
    {
        DongKiem Dong(int heThong, int? thucTe)
        {
            var d = new DongKiem { SLHeThong = heThong };
            if (thucTe is int t) d.GhiSoDem(t, 3);
            return d;
        }

        var chuaDem = PhatSinhService.KiemTraDongDuyetDuoc(Dong(10, null));
        var lechChuaPhanLoai = PhatSinhService.KiemTraDongDuyetDuoc(Dong(10, 7));
        var khop = PhatSinhService.KiemTraDongDuyetDuoc(Dong(10, 10));
        var daPhanLoai = Dong(10, 7);
        daPhanLoai.PhanLoai(DongKiem.ThieuHoacThua, "Hao hụt", null, null);

        Assert.False(chuaDem.ThanhCong);
        Assert.False(lechChuaPhanLoai.ThanhCong);
        Assert.Contains("chưa phân loại", lechChuaPhanLoai.ThongBao);
        Assert.True(khop.ThanhCong);
        Assert.True(PhatSinhService.KiemTraDongDuyetDuoc(daPhanLoai).ThanhCong);
    }

    [Fact]
    public void DaPhanLoai_CanLoaiHopLe_NguyenNhan_VaThongTinDiKemTheoLoai()
    {
        Assert.False(DongKiem.DuThongTinPhatSinh(DongKiem.ThieuHoacThua, null, null, null));
        Assert.False(DongKiem.DuThongTinPhatSinh(DongKiem.ThieuHoacThua, "  ", null, null));
        Assert.False(DongKiem.DuThongTinPhatSinh("Loại lạ", "x", null, null));
        Assert.False(DongKiem.DuThongTinPhatSinh(DongKiem.SaiViTri, "x", null, null));
        Assert.False(DongKiem.DuThongTinPhatSinh(DongKiem.SaiMaHang, "x", 5, null));
        Assert.True(DongKiem.DuThongTinPhatSinh(DongKiem.SaiViTri, "x", 5, null));
        Assert.True(DongKiem.DuThongTinPhatSinh(DongKiem.SaiMaHang, "x", null, 7));
        Assert.True(DongKiem.DuThongTinPhatSinh(DongKiem.HuHongHoacHetHan, "x", null, null));
    }

    // ---- Đếm lại sau khi đã phân loại ----

    [Fact]
    public void DemLai_CungSoGiuPhanLoai_SoKhacXoaPhanLoai_VeKhopXoaHet()
    {
        var d = new DongKiem { SLHeThong = 10 };
        d.GhiSoDem(6, 3);
        d.PhanLoai(DongKiem.SaiViTri, "Xếp nhầm", 55, null);

        d.GhiSoDem(6, 4);   // đếm lại ra cùng số: giữ phân loại
        Assert.True(d.DaPhanLoai);
        Assert.Equal(55, d.MaViTriDung);

        d.GhiSoDem(5, 4);   // số khác: số hàng cần chuyển và kiểm sức chứa đã đổi nên phải phân loại lại
        Assert.False(d.DaPhanLoai);
        Assert.Equal((DongKiem.ThieuHoacThua, (string?)null, (int?)null), (d.LoaiPhatSinh, d.NguyenNhan, d.MaViTriDung));
        Assert.Equal(DongKiem.ChoDuyet, d.TrangThaiXuLy);

        d.PhanLoai(DongKiem.ThieuHoacThua, "Hao hụt", null, null);
        d.GhiSoDem(10, 4);  // đếm lại ra khớp: xóa mọi phân loại
        Assert.Equal((DongKiem.Khop, (string?)null, (string?)null), (d.TrangThaiXuLy, d.LoaiPhatSinh, d.NguyenNhan));
    }

    // ---- Màn hình ----

    [Fact]
    public async Task LayTrang_ChiLayDongLech_DemDaVaChuaPhanLoai_LocDung()
    {
        var e = new MoiTruong();
        await e.Service.PhanLoai(MoiTruong.MaPK, Vm(MoiTruong.DongThieu, DongKiem.ThieuHoacThua), MoiTruong.NguoiDuyet);

        var tatCa = (await e.Service.LayTrang(MoiTruong.MaPK, LocPhatSinh.TatCa))!;
        var chua = (await e.Service.LayTrang(MoiTruong.MaPK, LocPhatSinh.ChuaPhanLoai))!;
        var da = (await e.Service.LayTrang(MoiTruong.MaPK, LocPhatSinh.DaPhanLoai))!;

        Assert.Equal((2, 1, 1, false), (tatCa.SoDongLech, tatCa.SoDaPhanLoai, tatCa.SoChuaPhanLoai, tatCa.SanSangDuyet));
        Assert.DoesNotContain(tatCa.DongLech, d => d.MaDK is MoiTruong.DongKhop or MoiTruong.DongChuaDem);
        Assert.Equal(MoiTruong.DongThua, Assert.Single(chua.DongCuaLoc).MaDK);
        Assert.Equal(MoiTruong.DongThieu, Assert.Single(da.DongCuaLoc).MaDK);
        Assert.Equal(new[] { MoiTruong.O11, MoiTruong.O12, MoiTruong.O13, MoiTruong.ONho }, tatCa.OLuuTru.Select(o => o.MaViTri).ToArray());
        Assert.Null(await e.Service.LayTrang(999, LocPhatSinh.TatCa));
    }

    // ---- Đọc ghi qua EF thật và các CHECK của DongKiem (cần SQL Server) ----

    [CanSqlServerFact]
    public async Task SqlServer_PhanLoaiGhiDuocVaDocLaiDungTen_CheckCoSoDuLieuChapNhan()
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        const int maKho = 1, maNguoiLap = 2, maNguoiDem = 3;
        var sau = Guid.NewGuid().ToString("N")[..8];
        ViTri O(string ten) => new()
        {
            TenViTri = ten + sau, MaKho = maKho, CapViTri = CapViTriGiaTri.O, LoaiViTri = LoaiViTriGiaTri.LuuTru,
            MaVachO = "MV-" + Guid.NewGuid().ToString("N")[..12], Dai = 100, Rong = 100, Cao = 100, KhoiLuongToiDa = 1000,
        };
        var o1 = O("O-PS1-"); var o2 = O("O-PS2-");
        db.ViTri.AddRange(o1, o2);
        var maDM = await db.DanhMuc.Select(d => d.MaDM).FirstAsync();
        var sp = new SanPham { TenSP = "SP PS " + sau, MaDM = maDM, DonViTinh = "cái" };
        db.SanPham.Add(sp);
        await db.SaveChangesAsync();
        BienThe BT(string ten) => new() { MaSP = sp.MaSP, MaVach = "BT" + Guid.NewGuid().ToString("N")[..12], ThuocTinh = ten, Dai = 1, Rong = 1, Cao = 1, KhoiLuong = 1 };
        var bt1 = BT("A"); var bt2 = BT("B");
        db.BienThe.AddRange(bt1, bt2);
        await db.SaveChangesAsync();
        var lo = new LoHang { MaVachLo = "LO-" + Guid.NewGuid().ToString("N")[..14], MaBT = bt1.MaBT };
        db.LoHang.Add(lo);
        await db.SaveChangesAsync();
        var phieu = new PhieuKiem { SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12], MaKho = maKho, LoaiKiemKe = PhieuKiem.ToanBo, MaNguoiLap = maNguoiLap, ThoiGianBatDau = DateTime.UtcNow };
        db.PhieuKiem.Add(phieu);
        await db.SaveChangesAsync();
        var dong = new DongKiem { MaPK = phieu.MaPK, MaViTri = o1.MaViTri, MaLo = lo.MaLo, SLHeThong = 10 };
        dong.GhiSoDem(6, maNguoiDem);
        db.DongKiem.Add(dong);
        await db.SaveChangesAsync();

        var sucChua = new SucChuaGia();
        var service = new PhatSinhService(new EfKiemDemStore(db), sucChua, new NhatKyGiaGhiLai());

        // Sai vị trí: ô đúng ghi vào cột MaViTriDung, đọc lại có tên ô.
        var kq = await service.PhanLoai(phieu.MaPK, Vm(dong.MaDK, DongKiem.SaiViTri, "Xếp nhầm sang ô bên", oDung: o2.MaViTri), maNguoiDem);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        var trang = (await service.LayTrang(phieu.MaPK, LocPhatSinh.TatCa))!;
        var d1 = Assert.Single(trang.DongLech);
        Assert.Equal((true, DongKiem.SaiViTri, o2.MaViTri, o2.TenViTri), (d1.DaPhanLoai, d1.LoaiPhatSinh, d1.MaViTriDung!.Value, d1.TenODung));
        Assert.Equal(-4, d1.ChenhLech);

        // Sai mã hàng: chuyển loại, ô đúng bị xóa (CHECK MaViTriDung chỉ khi Sai vị trí), mã hàng đúng có tên.
        kq = await service.PhanLoai(phieu.MaPK, Vm(dong.MaDK, DongKiem.SaiMaHang, "Nhãn lô dán nhầm", btDung: bt2.MaBT), maNguoiDem);
        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d2 = Assert.Single((await service.LayTrang(phieu.MaPK, LocPhatSinh.TatCa))!.DongLech);
        Assert.Equal((DongKiem.SaiMaHang, (int?)null, bt2.MaBT), (d2.LoaiPhatSinh, d2.MaViTriDung, d2.MaBTDung));
        Assert.Contains("B", d2.TenHangDung);

        // CSDL tự chặn dữ liệu sai dù lớp nghiệp vụ bị bỏ qua: Sai vị trí mà thiếu ô đúng.
        db.ChangeTracker.Clear();
        var tho = await db.DongKiem.FirstAsync(d => d.MaDK == dong.MaDK);
        tho.LoaiPhatSinh = DongKiem.SaiViTri; tho.MaViTriDung = null; tho.MaBTDung = null;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // ---- Môi trường thử: phiếu 1 của kho 1, bốn dòng: thiếu 4 (ô 11), thừa 2 (ô 11), khớp (ô 12), chưa đếm (ô 12) ----

    private sealed class MoiTruong
    {
        public const int MaPK = 1, MaKho = 1, MaKhoNgoai = 2;
        public const int O11 = 11, O12 = 12, O13 = 13, ONho = 14, OTiepNhan = 19, ONgung = 20, OKhoKhac = 21;
        public const int AoThun = 101, TaiNghe = 102;
        public const int DongThieu = 1, DongThua = 2, DongKhop = 3, DongChuaDem = 4;
        public const int NguoiDuyet = 2, NguoiDem = 3;

        public KhoGia Store { get; }
        public SucChuaGia SucChua { get; } = new();
        public List<(string HanhDong, int? MaCT)> NhatKy { get; } = new();
        public PhatSinhService Service { get; }

        public MoiTruong(string loaiKiemKe = PhieuKiem.ToanBo, string trangThai = PhieuKiem.DangKiem)
        {
            Store = new KhoGia(new PhieuKiemTomTat(MaPK, "KK000001", MaKho, "Kho tổng", loaiKiemKe, trangThai));
            Service = new PhatSinhService(Store, SucChua, new NhatKyGiaGhiLai(NhatKy));
        }
    }

    private sealed class NhatKyGiaGhiLai(List<(string HanhDong, int? MaCT)>? dong = null) : IAuditService
    {
        public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
        {
            dong?.Add((hanhDong, maCT));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NhatKyDto>>(Array.Empty<NhatKyDto>());
    }

    /// <summary>ICapacityService giả: mặc định ô nào cũng đủ chỗ; ghi lại các lần gọi và cho đặt kết quả riêng từng ô.</summary>
    private sealed class SucChuaGia : ICapacityService
    {
        public List<(int MaViTri, int MaBT, int SoLuong)> Goi { get; } = new();
        public Dictionary<int, KetQuaSucChua> TraVe { get; } = new();

        public Task<KetQuaSucChua> KiemTra(int maViTri, int maBT, int soLuong, CancellationToken ct = default)
        {
            Goi.Add((maViTri, maBT, soLuong));
            return Task.FromResult(TraVe.TryGetValue(maViTri, out var kq) ? kq : new KetQuaSucChua(true, soLuong + 100, 1000, 1000));
        }
    }

    private sealed class KhoGia(PhieuKiemTomTat phieu) : IKiemDemStore
    {
        private static readonly Dictionary<int, ViTriChon> ViTri = new()
        {
            [MoiTruong.O11] = new(MoiTruong.O11, "Ô 11", MoiTruong.MaKho, true, true, true, 100, 100, 100),
            [MoiTruong.O12] = new(MoiTruong.O12, "Ô 12", MoiTruong.MaKho, true, true, true, 100, 100, 100),
            [MoiTruong.O13] = new(MoiTruong.O13, "Ô 13", MoiTruong.MaKho, true, true, false, 100, 100, 100),
            [MoiTruong.ONho] = new(MoiTruong.ONho, "Ô nhỏ", MoiTruong.MaKho, true, true, false, 10, 10, 10),
            [MoiTruong.OTiepNhan] = new(MoiTruong.OTiepNhan, "Khu tiếp nhận", MoiTruong.MaKho, false, true, false),
            [MoiTruong.ONgung] = new(MoiTruong.ONgung, "Ô ngừng", MoiTruong.MaKho, true, false, false, 100, 100, 100),
            [MoiTruong.OKhoKhac] = new(MoiTruong.OKhoKhac, "Ô kho khác", MoiTruong.MaKhoNgoai, true, true, false, 100, 100, 100),
        };
        private static readonly Dictionary<int, string> TenHang = new() { [MoiTruong.AoThun] = "Áo thun (M)", [MoiTruong.TaiNghe] = "Tai nghe (đen)" };

        private readonly List<DongKiem> _dong = new()
        {
            Dem(MoiTruong.DongThieu, MoiTruong.O11, 1, 15, 11),
            Dem(MoiTruong.DongThua, MoiTruong.O11, 2, 5, 7),
            Dem(MoiTruong.DongKhop, MoiTruong.O12, 1, 8, 8),
            new() { MaDK = MoiTruong.DongChuaDem, MaPK = MoiTruong.MaPK, MaViTri = MoiTruong.O12, MaLo = 3, SLHeThong = 4 },
        };

        // Lô 1 và 3 của áo thun, lô 2 của tai nghe.
        private static int BienTheCuaLo(int maLo) => maLo == 2 ? MoiTruong.TaiNghe : MoiTruong.AoThun;

        private static DongKiem Dem(int maDK, int maViTri, int maLo, int heThong, int thucTe)
        {
            var d = new DongKiem { MaDK = maDK, MaPK = MoiTruong.MaPK, MaViTri = maViTri, MaLo = maLo, SLHeThong = heThong };
            d.GhiSoDem(thucTe, MoiTruong.NguoiDem);
            return d;
        }

        public HashSet<int> ODangKhoa { get; } = new();
        public bool LoiKhiLuu { get; set; }
        public int SoLanLuu { get; private set; }

        public DongKiem Dong(int maDK) => _dong.Single(d => d.MaDK == maDK);

        public Task<PhieuKiemTomTat?> LayPhieu(int maPK, CancellationToken ct = default) => Task.FromResult(maPK == phieu.MaPK ? phieu : null);

        public Task<IReadOnlyList<DongKiemXem>> LayDong(int maPK, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DongKiemXem>>(_dong.Where(d => d.MaPK == maPK).Select(d =>
            {
                var bt = BienTheCuaLo(d.MaLo);
                return new DongKiemXem(d.MaDK, d.MaViTri, ViTri[d.MaViTri].TenViTri, $"MV-{d.MaViTri}", d.MaLo, $"LO-{d.MaLo}", bt, TenHang[bt], $"BT-{bt}",
                    d.SLHeThong, d.SLThucTe, d.TrangThaiXuLy, d.LoaiPhatSinh, d.MaNguoiDem?.ToString(),
                    d.NguyenNhan, d.MaViTriDung, d.MaViTriDung is int o ? ViTri[o].TenViTri : null, d.MaBTDung, d.MaBTDung is int b ? TenHang[b] : null);
            }).ToList());

        public Task<DongKiem?> TimDong(int maPK, int maDK, CancellationToken ct = default) => Task.FromResult(_dong.FirstOrDefault(d => d.MaPK == maPK && d.MaDK == maDK));

        public Task<ViTriChon?> TimViTri(int maViTri, CancellationToken ct = default)
            => Task.FromResult(ViTri.TryGetValue(maViTri, out var v) ? v with { DangKhoa = ODangKhoa.Contains(maViTri) } : null);

        public Task<IReadOnlyList<ViTriChon>> LayOLuuTruHoatDong(int maKho, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ViTriChon>>(ViTri.Values.Where(v => v.MaKho == maKho && v.LaOLuuTru && v.DangHoatDong).OrderBy(v => v.MaViTri).ToList());

        public Task<string?> TenBienThe(int maBT, CancellationToken ct = default) => Task.FromResult(TenHang.GetValueOrDefault(maBT));

        public Task<KichThuocBienThe?> KichThuoc(int maBT, CancellationToken ct = default)
            => Task.FromResult<KichThuocBienThe?>(TenHang.ContainsKey(maBT) ? new KichThuocBienThe(20, 20, 5) : null);   // áo thun 20×20×5 cm

        public Task<IReadOnlyList<KeyValuePair<int, string>>> LayBienTheChon(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<KeyValuePair<int, string>>>(TenHang.OrderBy(x => x.Value).ToList());

        public Task LuuThayDoi(CancellationToken ct = default)
        {
            SoLanLuu++;
            if (LoiKhiLuu) throw new InvalidOperationException("Không ghi được dòng kiểm (giả lập).");
            return Task.CompletedTask;
        }

        // Các hàm của kiểm đếm (T3-5) không dùng ở đây; xem KiemDemTests.
        public Task<OQuet?> TimO(string maVachO, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<LoQuet?> TimLo(string maVachLo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int?> TimBienThe(string maVach, CancellationToken ct = default) => throw new NotSupportedException();
        public void Them(DongKiem dong) => throw new NotSupportedException();
        public void Xoa(DongKiem dong) => throw new NotSupportedException();
    }
}
