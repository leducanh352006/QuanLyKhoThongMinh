using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-1 (Tạo phiếu nhập). Không cần SQL Server.</summary>
public class PhieuNhapTests
{
    // Một phiếu hợp lệ: kho 1, nhà cung cấp 1, một dòng 5 cái của biến thể 1.
    private static PhieuNhap PhieuHopLe() => new()
    {
        LoaiPhieu = "Mua hàng", MaKho = 1, MaNCC = 1,
        ChiTiet = { new ChiTietPhieuNhap { MaBT = 1, SLDuKien = 5 } },
    };

    [Fact]
    public void PhieuHopLe_KhongCoLoi() => Assert.Null(PhieuHopLe().KiemTra());

    [Fact]
    public void LoaiKhac_DuocChapNhan()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = "Khác";
        Assert.Null(p.KiemTra());
    }

    [Fact]
    public void PhieuThuaDoKiemKe_KhongTaoTay()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = "Thừa do kiểm kê";
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void ChuaChonKho_BiTuChoi()
    {
        var p = PhieuHopLe(); p.MaKho = 0;
        Assert.Equal("Chọn kho nhập.", p.KiemTra());
    }

    [Fact]
    public void ThieuNhaCungCap_BiTuChoi()
    {
        var p = PhieuHopLe(); p.MaNCC = null;
        Assert.Equal("Chọn nhà cung cấp.", p.KiemTra());
    }

    [Fact]
    public void KhongCoDongHang_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ChiTiet.Clear();
        Assert.Equal("Phiếu phải có ít nhất một dòng hàng.", p.KiemTra());
    }

    [Fact]
    public void DongChuaChonMatHang_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ChiTiet[0].MaBT = 0;
        Assert.Equal("Chọn mặt hàng cho mỗi dòng.", p.KiemTra());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void SoLuongKhongDuong_BiTuChoi(int soLuong)
    {
        var p = PhieuHopLe(); p.ChiTiet[0].SLDuKien = soLuong;
        Assert.Equal("Số lượng mỗi dòng phải lớn hơn 0.", p.KiemTra());
    }

    [Fact]
    public void DienGiaiQua500KyTu_BiTuChoi()
    {
        var p = PhieuHopLe(); p.DienGiai = new string('a', 501);
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void PhieuNhap_NhapConSuaDuoc_DaXacNhanBiKhoa()
    {
        Assert.False(new PhieuNhap { TrangThai = PhieuNhap.Nhap }.DaKhoa);
        Assert.True(new PhieuNhap { TrangThai = PhieuNhap.DaXacNhan }.DaKhoa);
    }

    [Fact]
    public void CauHinhBang_DungTenCotVaKhoaNgoai()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);

        var phieu = db.Model.FindEntityType(typeof(PhieuNhap))!;
        Assert.Equal("PhieuNhap", phieu.GetTableName());
        Assert.Equal("varchar(20)", phieu.FindProperty("SoPhieu")!.GetColumnType());
        Assert.Contains(phieu.GetIndexes(), i => i.IsUnique && i.Properties.Any(c => c.Name == "SoPhieu"));
        Assert.Equal(new[] { "Kho", "NguoiDung", "NhaCungCap" },
            phieu.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());

        var dong = db.Model.FindEntityType(typeof(ChiTietPhieuNhap))!;
        Assert.Equal("ChiTietPhieuNhap", dong.GetTableName());
        Assert.Equal(new[] { "BienThe", "PhieuNhap" },
            dong.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());
    }
}
