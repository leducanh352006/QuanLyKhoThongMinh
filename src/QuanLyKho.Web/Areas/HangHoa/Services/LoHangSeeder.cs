using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Nạp LoHang từ <see cref="SeedDataSet"/> vào SQL Server, giữ nguyên mã (MaLo) để TonKho mẫu tham chiếu đúng.
/// Chạy sau SanPham, BienThe (SanPhamBienTheSeeder, ThuTu = 4) vì LoHang.MaBT tham chiếu BienThe đã seed trước.
/// Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu, giữ nguyên dữ liệu đã đổi qua màn hình quản lý.
/// </summary>
public sealed class LoHangSeeder : IDbSeeder
{
    public int ThuTu => 5;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var loCoSan = (await db.LoHang.Select(l => l.MaLo).ToListAsync(ct)).ToHashSet();
        var loMoi = SeedDataSet.LoHang.Where(l => !loCoSan.Contains(l.MaLo))
            .Select(l => new LoHang { MaLo = l.MaLo, MaVachLo = l.MaVachLo, MaBT = l.MaBT, HanDung = l.HanDung }).ToList();
        if (loMoi.Count == 0) return;

        await using var gd = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [LoHang] ON", ct);
        db.LoHang.AddRange(loMoi);
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [LoHang] OFF", ct);
        await gd.CommitAsync(ct);
    }
}
