using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests.KhoViTri;

/// <summary>
/// Demo T4-5 trên SQL Server thật. Chỉ chạy khi có biến môi trường KHO_TEST_CONN (chuỗi kết nối tới CSDL đã migrate và nạp dữ liệu mẫu);
/// không có thì test bỏ qua. Mọi thay đổi nằm trong một giao dịch và luôn rollback nên không làm bẩn CSDL.
/// </summary>
public class SucChuaKhoaOSqlServerTests
{
    private const int NguoiThucHien = 2;
    private const int OA101 = 3, OA201 = 6;   // ô mẫu: A1-01 (100x60x50 cm, 120 kg), A2-01 (60x40x40 cm, 20 kg, đang có 60 tai nghe 15 kg)
    private const int TaiNgheBT = 4;
    private static readonly ThamChieuChungTu PhieuNhap = new(LoaiChungTu.NK, 9001);

    [Fact]
    public async Task Demo_SucChua_KhoaO_TrenSqlServerThat()
    {
        var conn = Environment.GetEnvironmentVariable("KHO_TEST_CONN");
        if (string.IsNullOrWhiteSpace(conn)) return;

        var options = new DbContextOptionsBuilder<KhoDbContext>().UseSqlServer(conn).Options;
        await using var db = new KhoDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var stock = new StockService(db);
            var khoa = new LockService(new EfKhoViTriStore(db), stock, new NhatKyGia());
            var cap = new CapacityService(db, new KhongCoHangChoXep());

            // 1. Sức chứa: ô A2-01 còn nhận tối đa 20 tai nghe (giới hạn khối lượng).
            Assert.True((await cap.KiemTra(OA201, TaiNgheBT, 20)).Du);
            var vuot = await cap.KiemTra(OA201, TaiNgheBT, 21);
            Assert.False(vuot.Du);
            Assert.Equal(MaLoiDichVu.VuotSucChua, vuot.MaLoi);
            Assert.Contains("khối lượng", vuot.LyDo);

            // 2. Khóa ô A1-01: nhập, xuất, chuyển đều bị từ chối.
            var ton = (await stock.LayTonTheoViTri(OA101)).First();
            var dong = new DongTon(ton.MaLo, OA101, 1);
            Assert.True((await khoa.KhoaO(OA101, NguoiThucHien)).ThanhCong);
            Assert.True(await khoa.DangKhoa(OA101));

            Assert.Equal(MaLoiDichVu.OBiKhoa, (await stock.GhiTang(dong, LoaiBienDong.SapXep, PhieuNhap, NguoiThucHien)).MaLoi);
            Assert.Equal(MaLoiDichVu.OBiKhoa, (await stock.GhiGiam(dong, LoaiBienDong.XuatKho, new(LoaiChungTu.XK, 9002), NguoiThucHien)).MaLoi);
            Assert.Equal(MaLoiDichVu.OBiKhoa, (await stock.ChuyenO(ton.MaLo, OA101, OA201, 1, new(LoaiChungTu.CK, 9003), NguoiThucHien)).MaLoi);
            Assert.Equal(MaLoiDichVu.OBiKhoa, (await stock.GiuCho(dong)).MaLoi);
            Assert.Equal(MaLoiDichVu.OBiKhoa, (await cap.KiemTra(OA101, TaiNgheBT, 1)).MaLoi);

            // 3. Chứng từ kiểm kê vẫn điều chỉnh được ô đang khóa; mở ô thì nhập lại được.
            Assert.True((await stock.GhiTang(dong, LoaiBienDong.SapXep, new(LoaiChungTu.KK, 9004), NguoiThucHien)).ThanhCong);
            Assert.True((await khoa.MoO(OA101, NguoiThucHien)).ThanhCong);
            Assert.True((await stock.GhiTang(dong, LoaiBienDong.SapXep, PhieuNhap, NguoiThucHien)).ThanhCong);

            // 4. Ô còn hàng giữ chỗ cho phiếu xuất thì chưa khóa được.
            var tonO6 = (await stock.LayTonTheoViTri(OA201)).First();
            var giu = new DongTon(tonO6.MaLo, OA201, 1);
            Assert.True((await stock.GiuCho(giu)).ThanhCong);
            Assert.Equal(MaLoiDichVu.SaiTrangThai, (await khoa.KhoaO(OA201, NguoiThucHien)).MaLoi);
            Assert.True((await stock.TraCho(giu)).ThanhCong);
            Assert.True((await khoa.KhoaO(OA201, NguoiThucHien)).ThanhCong);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}
