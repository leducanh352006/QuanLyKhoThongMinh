using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T2-1 (Tạo phiếu xuất). Không cần SQL Server.</summary>
public class PhieuXuatTests
{
    // Một phiếu hợp lệ: kho 1, loại Khác, một dòng 5 cái của biến thể 1.
    private static PhieuXuat PhieuHopLe() => new()
    {
        LoaiPhieu = PhieuXuat.Khac, MaKho = 1,
        ChiTiet = { new ChiTietPhieuXuat { MaBT = 1, SoLuong = 5 } },
    };

    [Fact]
    public void PhieuHopLe_KhongCoLoi() => Assert.Null(PhieuHopLe().KiemTra());

    [Fact]
    public void PhieuThieuDoKiemKe_KhongTaoTay()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = PhieuXuat.ThieuDoKiemKe;
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void ChuaChonKho_BiTuChoi()
    {
        var p = PhieuHopLe(); p.MaKho = 0;
        Assert.Equal("Chọn kho xuất.", p.KiemTra());
    }

    [Fact]
    public void TraLaiHangMua_ThieuPhieuNhapGoc_BiTuChoi()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = PhieuXuat.TraLaiHangMua;
        Assert.Equal("Phiếu trả lại hàng mua phải chọn phiếu nhập gốc.", p.KiemTra());
    }

    [Fact]
    public void TraLaiHangMua_CoPhieuNhapGoc_KhongLoi()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = PhieuXuat.TraLaiHangMua; p.MaPNGoc = 10;
        Assert.Null(p.KiemTra());
    }

    [Fact]
    public void LoaiKhac_MaCoPhieuNhapGoc_BiTuChoi()
    {
        var p = PhieuHopLe(); p.MaPNGoc = 10;
        Assert.Equal("Chỉ phiếu trả lại hàng mua mới chọn phiếu nhập gốc.", p.KiemTra());
    }

    [Fact]
    public void HuyHang_ThieuLyDo_BiTuChoi()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = PhieuXuat.HuyHang;
        Assert.Equal("Phiếu hủy hàng phải ghi lý do.", p.KiemTra());
    }

    [Fact]
    public void HuyHang_CoLyDo_KhongLoi()
    {
        var p = PhieuHopLe(); p.LoaiPhieu = PhieuXuat.HuyHang; p.LyDo = "Hàng vỡ";
        Assert.Null(p.KiemTra());
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
        var p = PhieuHopLe(); p.ChiTiet[0].SoLuong = soLuong;
        Assert.Equal("Số lượng mỗi dòng phải lớn hơn 0.", p.KiemTra());
    }

    [Fact]
    public void DoiTuongQua150KyTu_BiTuChoi()
    {
        var p = PhieuHopLe(); p.DoiTuong = new string('a', 151);
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void ThamChieuQua100KyTu_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ThamChieu = new string('a', 101);
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void DienGiaiQua500KyTu_BiTuChoi()
    {
        var p = PhieuHopLe(); p.DienGiai = new string('a', 501);
        Assert.NotNull(p.KiemTra());
    }

    [Fact]
    public void PhieuXuat_NhapConSuaDuoc_DaDuyetBiKhoa()
    {
        Assert.False(new PhieuXuat { TrangThai = PhieuXuat.Nhap }.DaKhoa);
        Assert.True(new PhieuXuat { TrangThai = PhieuXuat.DaDuyet }.DaKhoa);
    }

    [Fact]
    public void CauHinhBang_DungTenCotVaKhoaNgoai()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);

        var phieu = db.Model.FindEntityType(typeof(PhieuXuat))!;
        Assert.Equal("PhieuXuat", phieu.GetTableName());
        Assert.Equal("varchar(20)", phieu.FindProperty("SoPhieu")!.GetColumnType());
        Assert.Contains(phieu.GetIndexes(), i => i.IsUnique && i.Properties.Any(c => c.Name == "SoPhieu"));
        Assert.Equal(new[] { "Kho", "NguoiDung", "PhieuNhap" },
            phieu.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).Distinct().OrderBy(x => x).ToArray());

        var dong = db.Model.FindEntityType(typeof(ChiTietPhieuXuat))!;
        Assert.Equal("ChiTietPhieuXuat", dong.GetTableName());
        Assert.Equal(new[] { "BienThe", "PhieuXuat" },
            dong.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());
    }
}
