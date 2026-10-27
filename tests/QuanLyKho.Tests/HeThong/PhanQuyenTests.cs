using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Tests.HeThong;

public class QuyenMacDinhTests
{
    private static PhanQuyen O(string vaiTro, LoaiChungTu loai)
        => QuyenMacDinh.Cho(1, vaiTro).Single(p => p.LoaiChungTu == loai.ToCsdl());

    [Fact]
    public void MoiVaiTro_CoDuBonChungTu_VaMaTranHopLe()
    {
        foreach (var vt in SeedDataSet.VaiTro)
        {
            var dong = QuyenMacDinh.Cho(vt.MaVaiTro, vt.TenVaiTro);
            Assert.Equal(new[] { "NK", "XK", "CK", "KK" }, dong.Select(d => d.LoaiChungTu).ToArray());
            foreach (var d in dong)
                Assert.True(QuyTacPhanQuyen.KiemTra(vt.TenVaiTro, d).ThanhCong, $"{vt.TenVaiTro} {d.LoaiChungTu}");
        }
    }

    [Fact]
    public void QuyenDuyetPhatSinhKiemKe_ChiQuanLyKho()   // Bảng 2.2, ghi chú dưới bảng
    {
        foreach (var vt in VaiTroTen.TatCa)
            Assert.Equal(vt == VaiTroTen.QuanLyKho, O(vt, LoaiChungTu.KK).CoDuyet);
    }

    [Fact]
    public void QuanLyKho_DuyetPhieuXuatVaChuyenKho()
    {
        Assert.True(O(VaiTroTen.QuanLyKho, LoaiChungTu.XK).CoDuyet);
        Assert.True(O(VaiTroTen.QuanLyKho, LoaiChungTu.CK).CoDuyet);
        Assert.True(O(VaiTroTen.QuanLyKho, LoaiChungTu.CK).CoThem);   // tạo phiếu chuyển kho
        Assert.True(O(VaiTroTen.QuanLyKho, LoaiChungTu.KK).CoThem);   // tạo phiếu kiểm
    }

    [Fact]
    public void ThuKho_TaoPhieuNhapVaDuyetXepHang_KhongDuyetXuat()
    {
        Assert.True(O(VaiTroTen.ThuKho, LoaiChungTu.NK).CoThem);
        Assert.True(O(VaiTroTen.ThuKho, LoaiChungTu.NK).CoDuyet);
        Assert.False(O(VaiTroTen.ThuKho, LoaiChungTu.XK).CoDuyet);
        Assert.False(O(VaiTroTen.ThuKho, LoaiChungTu.CK).CoThem);     // chỉ Quản lý kho tạo phiếu chuyển
    }

    [Fact]
    public void NhanVienBanHang_ChiXemVaTaoPhieuXuat()
    {
        var xk = O(VaiTroTen.NhanVienBanHang, LoaiChungTu.XK);
        Assert.True(xk.CoXem);
        Assert.True(xk.CoThem);
        Assert.False(xk.CoSua || xk.CoXoa || xk.CoDuyet);
        foreach (var c in new[] { LoaiChungTu.NK, LoaiChungTu.CK, LoaiChungTu.KK })
        {
            var p = O(VaiTroTen.NhanVienBanHang, c);
            Assert.False(p.CoXem || p.CoThem || p.CoSua || p.CoXoa || p.CoDuyet, c.ToString());
        }
    }

    [Fact]
    public void VaiTroLa_KhongCoQuyenNao()
    {
        Assert.Empty(QuyenMacDinh.Cho(9, "Vai trò không có"));
    }
}

public class PhanQuyenServiceTests
{
    [Fact]
    public async Task CoQuyen_TheoMaTranMacDinh()
    {
        var e = new MoiTruongThu();
        Assert.True(await e.PhanQuyen.CoQuyen(MoiTruongThu.QuanLyKho, LoaiChungTu.XK, QuyenThaoTac.Duyet));
        Assert.False(await e.PhanQuyen.CoQuyen(MoiTruongThu.ThuKho, LoaiChungTu.XK, QuyenThaoTac.Duyet));
        Assert.True(await e.PhanQuyen.CoQuyen(MoiTruongThu.NhanVienBanHang, LoaiChungTu.XK, QuyenThaoTac.Them));
        Assert.False(await e.PhanQuyen.CoQuyen(MoiTruongThu.NhanVienBanHang, LoaiChungTu.NK, QuyenThaoTac.Xem));
    }

    [Fact]
    public async Task CoQuyen_VaiTroKhongTonTai_LaFalse()
    {
        var e = new MoiTruongThu();
        Assert.False(await e.PhanQuyen.CoQuyen(99, LoaiChungTu.XK, QuyenThaoTac.Xem));
    }

    [Fact]
    public async Task CoQuyenHienTai_ChuaDangNhap_LaFalse_VaKiemTraBaoChuaDangNhap()
    {
        var e = new MoiTruongThu();
        Assert.False(await e.PhanQuyen.CoQuyenHienTai(LoaiChungTu.XK, QuyenThaoTac.Xem));
        var kq = await e.PhanQuyen.KiemTra(LoaiChungTu.XK, QuyenThaoTac.Xem);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiPhanQuyen.ChuaDangNhap, kq.MaLoi);
    }

    [Fact]
    public async Task KiemTra_ThieuQuyen_ThongBaoTiengVietNeuRoQuyenVaChungTu()
    {
        var e = new MoiTruongThu();
        e.DangNhapLa(MoiTruongThu.ThuKho);
        var kq = await e.PhanQuyen.KiemTra(LoaiChungTu.XK, QuyenThaoTac.Duyet);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiPhanQuyen.KhongCoQuyen, kq.MaLoi);
        Assert.Contains("duyệt", kq.ThongBao);
        Assert.Contains("phiếu xuất", kq.ThongBao);
        Assert.True((await e.PhanQuyen.KiemTra(LoaiChungTu.NK, QuyenThaoTac.Them)).ThanhCong);
    }

    [Fact]
    public async Task LayMaTran_LuonDuBonChungTuDungThuTu_KeCaVaiTroChuaCoDong()
    {
        var e = new MoiTruongThu();
        var maTran = await e.PhanQuyen.LayMaTran(99);
        Assert.Equal(new[] { "NK", "XK", "CK", "KK" }, maTran.Select(p => p.LoaiChungTu).ToArray());
        Assert.All(maTran, p => Assert.False(p.CoXem || p.CoThem || p.CoSua || p.CoXoa || p.CoDuyet));
    }

    [Fact]
    public async Task Luu_DoiQuyen_CoHieuLucNgay_DuDaNhoTam()
    {
        var e = new MoiTruongThu();
        Assert.False(await e.PhanQuyen.CoQuyen(MoiTruongThu.NhanVienBanHang, LoaiChungTu.NK, QuyenThaoTac.Xem));   // làm nóng nhớ tạm

        var maTran = (await e.PhanQuyen.LayMaTran(MoiTruongThu.NhanVienBanHang)).ToList();
        maTran.Single(p => p.LoaiChungTu == "NK").CoXem = true;
        var kq = await e.PhanQuyen.Luu(MoiTruongThu.NhanVienBanHang, maTran, MoiTruongThu.QuanTriVien);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        Assert.True(await e.PhanQuyen.CoQuyen(MoiTruongThu.NhanVienBanHang, LoaiChungTu.NK, QuyenThaoTac.Xem));
        Assert.Contains(e.NhatKy.Dong, d => d.HanhDong == "Đổi phân quyền" && d.MaND == MoiTruongThu.QuanTriVien && d.ChiTiet!.Contains("Nhân viên bán hàng"));
    }

    [Fact]
    public async Task Luu_KhongDoiGiPhaiVanThanhCong_VaKhongLamMatQuyenKhac()
    {
        var e = new MoiTruongThu();
        var maTran = await e.PhanQuyen.LayMaTran(MoiTruongThu.ThuKho);
        Assert.True((await e.PhanQuyen.Luu(MoiTruongThu.ThuKho, maTran, MoiTruongThu.QuanTriVien)).ThanhCong);
        Assert.True(await e.PhanQuyen.CoQuyen(MoiTruongThu.QuanLyKho, LoaiChungTu.XK, QuyenThaoTac.Duyet));
    }

    [Fact]
    public async Task Luu_DuyetKiemKeChoThuKho_BiTuChoi_VaKhongGhiGi()
    {
        var e = new MoiTruongThu();
        var maTran = (await e.PhanQuyen.LayMaTran(MoiTruongThu.ThuKho)).ToList();
        maTran.Single(p => p.LoaiChungTu == "KK").CoDuyet = true;

        var kq = await e.PhanQuyen.Luu(MoiTruongThu.ThuKho, maTran, MoiTruongThu.QuanTriVien);

        Assert.False(kq.ThanhCong);
        Assert.Contains("Quản lý kho", kq.ThongBao);
        Assert.False(await e.PhanQuyen.CoQuyen(MoiTruongThu.ThuKho, LoaiChungTu.KK, QuyenThaoTac.Duyet));
        Assert.Empty(e.NhatKy.Dong);
    }

    [Fact]
    public async Task Luu_CoQuyenKhacNhungKhongCoXem_BiTuChoi()
    {
        var e = new MoiTruongThu();
        var maTran = (await e.PhanQuyen.LayMaTran(MoiTruongThu.NhanVienBanHang)).ToList();
        var xk = maTran.Single(p => p.LoaiChungTu == "XK");
        xk.CoXem = false;   // vẫn còn CoThem
        Assert.False((await e.PhanQuyen.Luu(MoiTruongThu.NhanVienBanHang, maTran, MoiTruongThu.QuanTriVien)).ThanhCong);
    }

    [Fact]
    public async Task Luu_ThieuHoacTrungChungTu_BiTuChoi()
    {
        var e = new MoiTruongThu();
        var maTran = (await e.PhanQuyen.LayMaTran(MoiTruongThu.ThuKho)).ToList();
        Assert.False((await e.PhanQuyen.Luu(MoiTruongThu.ThuKho, maTran.Take(3).ToList(), MoiTruongThu.QuanTriVien)).ThanhCong);

        maTran[3] = maTran[0].Sao();   // NK hai lần, thiếu KK
        Assert.False((await e.PhanQuyen.Luu(MoiTruongThu.ThuKho, maTran, MoiTruongThu.QuanTriVien)).ThanhCong);
    }

    [Fact]
    public async Task Luu_VaiTroKhongTonTai_BaoKhongTimThay()
    {
        var e = new MoiTruongThu();
        var maTran = await e.PhanQuyen.LayMaTran(MoiTruongThu.ThuKho);
        var kq = await e.PhanQuyen.Luu(99, maTran, MoiTruongThu.QuanTriVien);
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiDichVu.KhongTimThay, kq.MaLoi);
    }
}
