using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>
/// Nạp TuyenVanChuyen và DinhMucKho từ <see cref="SeedDataSet"/> vào SQL Server (việc T5-1) — tiêu chí hoàn thành yêu cầu "dữ
/// liệu mẫu có tuyến giữa 2 kho". Chạy sau Kho (HeThongSeeder, ThuTu = 1) và BienThe (SanPhamBienTheSeeder, ThuTu = 4). Khóa
/// chính của cả hai bảng không phải identity nên không cần IDENTITY_INSERT. Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu,
/// giữ nguyên dữ liệu đã đổi qua màn hình quản lý.
/// </summary>
public sealed class DieuChuyenSeeder : IDbSeeder
{
    public int ThuTu => 7;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var tuyenCoSan = (await db.TuyenVanChuyen.Select(t => new { t.MaKhoXuat, t.MaKhoNhap }).ToListAsync(ct))
            .Select(t => (t.MaKhoXuat, t.MaKhoNhap)).ToHashSet();
        var tuyenMoi = SeedDataSet.TuyenVanChuyen.Where(t => !tuyenCoSan.Contains((t.MaKhoXuat, t.MaKhoNhap)))
            .Select(t => new TuyenVanChuyen { MaKhoXuat = t.MaKhoXuat, MaKhoNhap = t.MaKhoNhap, DonGiaVC = t.DonGiaVC, PhiChuyen = t.PhiChuyen });
        db.TuyenVanChuyen.AddRange(tuyenMoi);

        var dinhMucCoSan = (await db.DinhMucKho.Select(d => new { d.MaKho, d.MaBT }).ToListAsync(ct))
            .Select(d => (d.MaKho, d.MaBT)).ToHashSet();
        var dinhMucMoi = SeedDataSet.DinhMucKho.Where(d => !dinhMucCoSan.Contains((d.MaKho, d.MaBT)))
            .Select(d => new DinhMucKho { MaKho = d.MaKho, MaBT = d.MaBT, TonToiThieu = d.TonToiThieu, NgayAnToan = d.NgayAnToan });
        db.DinhMucKho.AddRange(dinhMucMoi);

        await db.SaveChangesAsync(ct);
    }
}
