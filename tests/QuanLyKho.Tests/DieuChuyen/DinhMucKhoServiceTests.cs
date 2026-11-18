using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>
/// Kiểm thử việc T5-1 (issue #28, "Tuyến vận chuyển và định mức kho") phần định mức kho: thêm/sửa tồn tối thiểu (m) và số
/// ngày an toàn (N) của một biến thể tại một kho — đầu vào cho gợi ý G3. Dùng Kho quầy cửa hàng (2) và biến thể tai nghe (4)
/// đã có sẵn định mức mẫu; mỗi bài tự chạy trong giao dịch riêng (không commit).
/// </summary>
public class DinhMucKhoServiceTests
{
    private sealed record MoiTruong(KhoDbContext Db, DinhMucKhoService Service);

    private static async Task DungAsync(Func<MoiTruong, Task> chay)
    {
        var tuyChon = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable(CanSqlServerFactAttribute.BienMoiTruong)).Options;
        await using var db = new KhoDbContext(tuyChon);
        await using var giaoDich = await db.Database.BeginTransactionAsync();   // không commit: hủy hết khi kết thúc

        var audit = new AuditService(new EfNhatKyStore(db), TimeProvider.System);
        var service = new DinhMucKhoService(db, audit);

        await chay(new MoiTruong(db, service));
    }

    [CanSqlServerFact]
    public async Task Them_CapKhoBienTheMoi_ThanhCong() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new DinhMucKho { MaKho = 1, MaBT = 1, TonToiThieu = 20, NgayAnToan = 5 }, maNguoiThucHien: 1);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = await e.Db.DinhMucKho.AsNoTracking().SingleAsync(x => x.MaKho == 1 && x.MaBT == 1);
        Assert.Equal(20, d.TonToiThieu);
        Assert.Equal(5, d.NgayAnToan);
    });

    [CanSqlServerFact]
    public async Task Them_TonToiThieuAm_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new DinhMucKho { MaKho = 1, MaBT = 1, TonToiThieu = -1, NgayAnToan = 5 }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Tồn tối thiểu không được âm.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_KhoKhongTonTai_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new DinhMucKho { MaKho = 9999, MaBT = 1, TonToiThieu = 1, NgayAnToan = 1 }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Không tìm thấy kho.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_BienTheKhongTonTai_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Them(new DinhMucKho { MaKho = 1, MaBT = 9999, TonToiThieu = 1, NgayAnToan = 1 }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Không tìm thấy biến thể.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Them_TrungCapDaCo_BiTuChoi() => await DungAsync(async e =>
    {
        // Định mức mẫu Kho quầy cửa hàng (2) / biến thể tai nghe (4) đã có trong dữ liệu mẫu (dùng demo G3).
        var kq = await e.Service.Them(new DinhMucKho { MaKho = 2, MaBT = 4, TonToiThieu = 1, NgayAnToan = 1 }, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Định mức cho biến thể này tại kho này đã tồn tại — hãy sửa định mức sẵn có.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task Sua_CapSanCo_CapNhatThamSo() => await DungAsync(async e =>
    {
        var kq = await e.Service.Sua(maKho: 2, maBT: 4, tonToiThieu: 15, ngayAnToan: 7, maNguoiThucHien: 1);

        Assert.True(kq.ThanhCong, kq.ThongBao);
        var d = await e.Db.DinhMucKho.AsNoTracking().SingleAsync(x => x.MaKho == 2 && x.MaBT == 4);
        Assert.Equal(15, d.TonToiThieu);
        Assert.Equal(7, d.NgayAnToan);
    });

    [CanSqlServerFact]
    public async Task Sua_KhongTonTai_BiTuChoi() => await DungAsync(async e =>
    {
        var kq = await e.Service.Sua(maKho: 1, maBT: 1, tonToiThieu: 1, ngayAnToan: 1, maNguoiThucHien: 1);

        Assert.False(kq.ThanhCong);
        Assert.Equal("Không tìm thấy định mức kho.", kq.ThongBao);
    });

    [CanSqlServerFact]
    public async Task LayDanhSach_CoDinhMucMau_TraVeKemTenKhoVaBienThe() => await DungAsync(async e =>
    {
        var ds = await e.Service.LayDanhSach();

        var mau = Assert.Single(ds, x => x.MaKho == 2 && x.MaBT == 4);
        Assert.Equal("Kho quầy cửa hàng", mau.TenKho);
        Assert.Contains("Tai nghe", mau.TenBienThe);
    });
}
