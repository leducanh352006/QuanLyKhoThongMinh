using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T5-1 (issue #28, "Tuyến vận chuyển và định mức kho") phần tuyến vận chuyển: thêm/sửa tham số c(i,j), F(i,j)
/// giữa từng cặp kho — đầu vào cho gợi ý G4. Dùng hai kho đã có từ dữ liệu mẫu (Kho tổng = 1, Kho quầy cửa hàng = 2);
/// mỗi bài tự chạy trong giao dịch riêng (không commit) nên không ảnh hưởng tuyến mẫu (1 → 2) đã seed sẵn.
/// </summary>
public class TuyenVanChuyenServiceTests
{
    private sealed record MoiTruong(KhoDbContext Db, TuyenVanChuyenService Service);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
        var service = new TuyenVanChuyenService(db, audit);

        await chay(new MoiTruong(db, service));
    }

    [CanSqlServerFact]
    public async Task Them_CapKhoMoi_ThanhCong() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new TuyenVanChuyen { MaKhoXuat = 2, MaKhoNhap = 1, DonGiaVC = 4500m, PhiChuyen = 120000m }, maNguoiThucHien: 1);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var t = await e.Db.TuyenVanChuyen.AsNoTracking().SingleAsync(x => x.MaKhoXuat == 2 && x.MaKhoNhap == 1);
        Assert.Equal(4500m, t.DonGiaVC);
        Assert.Equal(120000m, t.PhiChuyen);
    });

    [CanSqlServerFact]
    public async Task Them_CungKhoXuatNhap_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new TuyenVanChuyen { MaKhoXuat = 1, MaKhoNhap = 1, DonGiaVC = 1000m, PhiChuyen = 1000m }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Kho xuất phải khác kho nhập.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_DonGiaAm_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new TuyenVanChuyen { MaKhoXuat = 2, MaKhoNhap = 1, DonGiaVC = -1m, PhiChuyen = 1000m }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Đơn giá vận chuyển không được âm.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_KhoKhongTonTai_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new TuyenVanChuyen { MaKhoXuat = 9999, MaKhoNhap = 1, DonGiaVC = 1000m, PhiChuyen = 1000m }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Kho xuất hoặc kho nhập không tồn tại.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_TrungCapKhoDaCo_BiTuChoi() => await DungAsync(async e =>
    {
        // Tuyến mẫu Kho tổng (1) → Kho quầy cửa hàng (2) đã có trong dữ liệu mẫu (tiêu chí hoàn thành T5-1).
        var kq = await e.Service.Them(new TuyenVanChuyen { MaKhoXuat = 1, MaKhoNhap = 2, DonGiaVC = 1000m, PhiChuyen = 1000m }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Tuyến giữa hai kho này đã tồn tại — hãy sửa tuyến sẵn có.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Sua_CapKhoSanCo_CapNhatThamSo() => await DungAsync(async e =>
    {
        var kq = await e.Service.Sua(maKhoXuat: 1, maKhoNhap: 2, donGiaVC: 9999m, phiChuyen: 250000m, maNguoiThucHien: 1);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var t = await e.Db.TuyenVanChuyen.AsNoTracking().SingleAsync(x => x.MaKhoXuat == 1 && x.MaKhoNhap == 2);
        Assert.Equal(9999m, t.DonGiaVC);
        Assert.Equal(250000m, t.PhiChuyen);
    });

    [CanSqlServerFact]
    public async Task Sua_KhongTonTai_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Sua(maKhoXuat: 2, maKhoNhap: 1, donGiaVC: 1000m, phiChuyen: 1000m, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Không tìm thấy tuyến vận chuyển.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task LayDanhSach_CoTuyenMau_TraVeKemTenKho() => await DungAsync(async e =>
    {
        var ds = await e.Service.LayDanhSach();

        var mau = Assert.Single(ds, x => x.MaKhoXuat == 1 && x.MaKhoNhap == 2);
        Assert.Equal("Kho tổng", mau.TenKhoXuat);
        Assert.Equal("Kho quầy cửa hàng", mau.TenKhoNhap);
    });
}
