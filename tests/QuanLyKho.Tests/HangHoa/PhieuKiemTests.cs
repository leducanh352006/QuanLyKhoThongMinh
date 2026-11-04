using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T3-4 (Tạo phiếu kiểm và thành phần tham gia). Không cần SQL Server.</summary>
public class PhieuKiemTests
{
    // Một phiếu hợp lệ: kho 1, kiểm toàn bộ, một người tham gia.
    private static PhieuKiem PhieuHopLe() => new()
    {
        LoaiKiemKe = PhieuKiem.ToanBo, MaKho = 1,
        ThamGia = { new ThamGiaKiem { MaND = 1 } },
    };

    [Fact]
    public void PhieuHopLe_KhongCoLoi() => Assert.Null(PhieuHopLe().KiemTra());

    [Fact]
    public void ChuaChonKho_BiTuChoi()
    {
        var p = PhieuHopLe(); p.MaKho = 0;
        Assert.Equal("Chọn kho kiểm kê.", p.KiemTra());
    }

    [Fact]
    public void LoaiKiemKeKhongHopLe_BiTuChoi()
    {
        var p = PhieuHopLe(); p.LoaiKiemKe = "Khác";
        Assert.Equal("Loại kiểm kê chỉ được chọn Toàn bộ hoặc Một phần.", p.KiemTra());
    }

    [Fact]
    public void MotPhan_ChuaChonPhamVi_BiTuChoi()
    {
        var p = PhieuHopLe(); p.LoaiKiemKe = PhieuKiem.MotPhan;
        Assert.Equal("Kiểm kê một phần phải chọn phạm vi mặt hàng.", p.KiemTra());
    }

    [Fact]
    public void MotPhan_CoPhamVi_KhongLoi()
    {
        var p = PhieuHopLe(); p.LoaiKiemKe = PhieuKiem.MotPhan; p.PhamViMaBT.Add(1);
        Assert.Null(p.KiemTra());
    }

    [Fact]
    public void ToanBo_MaCoPhamVi_BiTuChoi()
    {
        var p = PhieuHopLe(); p.PhamViMaBT.Add(1);
        Assert.Equal("Kiểm kê toàn bộ không chọn phạm vi mặt hàng.", p.KiemTra());
    }

    [Fact]
    public void KhongCoNguoiThamGia_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ThamGia.Clear();
        Assert.Equal("Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.", p.KiemTra());
    }

    [Fact]
    public void DongThamGiaChuaChonNguoi_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ThamGia[0].MaND = 0;
        Assert.Equal("Chọn người cho mỗi dòng tham gia kiểm.", p.KiemTra());
    }

    [Fact]
    public void MotNguoiThamGiaHaiLan_BiTuChoi()
    {
        var p = PhieuHopLe(); p.ThamGia.Add(new ThamGiaKiem { MaND = 1 });
        Assert.Equal("Một người không khai báo tham gia hai lần trên cùng phiếu.", p.KiemTra());
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
    public void CauHinhBang_DungTenCotVaKhoaNgoai()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);

        var phieu = db.Model.FindEntityType(typeof(PhieuKiem))!;
        Assert.Equal("PhieuKiem", phieu.GetTableName());
        Assert.Equal("varchar(20)", phieu.FindProperty("SoPhieu")!.GetColumnType());
        Assert.Contains(phieu.GetIndexes(), i => i.IsUnique && i.Properties.Any(c => c.Name == "SoPhieu"));
        Assert.Equal(new[] { "Kho", "NguoiDung" },
            phieu.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).Distinct().OrderBy(x => x).ToArray());

        var chiTiet = db.Model.FindEntityType(typeof(ChiTietPhieuKiem))!;
        Assert.Equal("ChiTietPhieuKiem", chiTiet.GetTableName());
        Assert.Equal(new[] { "BienThe", "LoHang", "PhieuKiem", "ViTri" },
            chiTiet.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());

        var thamGia = db.Model.FindEntityType(typeof(ThamGiaKiem))!;
        Assert.Equal("ThamGiaKiem", thamGia.GetTableName());
        Assert.Equal(new[] { "MaPK", "MaND" }, thamGia.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "NguoiDung", "PhieuKiem" },
            thamGia.GetForeignKeys().Select(f => f.PrincipalEntityType.GetTableName()!).OrderBy(x => x).ToArray());
    }
}
