using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Nạp SanPham và BienThe từ <see cref="SeedDataSet"/> vào SQL Server, giữ nguyên mã (MaSP, MaBT) để các nhóm khác tham chiếu
/// (LoHang.MaBT, TonKho qua Lô). Chạy sau DanhMuc (HangHoaSeeder, ThuTu = 3) vì SanPham.MaDM tham chiếu DanhMuc đã seed trước.
/// Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu, giữ nguyên dữ liệu đã đổi qua màn hình quản lý.
/// </summary>
public sealed class SanPhamBienTheSeeder : IDbSeeder
{
    public int ThuTu => 4;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var spCoSan = (await db.SanPham.Select(s => s.MaSP).ToListAsync(ct)).ToHashSet();
        var spMoi = SeedDataSet.SanPham.Where(s => !spCoSan.Contains(s.MaSP))
            .Select(s => new SanPham { MaSP = s.MaSP, TenSP = s.TenSP, MaDM = s.MaDM, DonViTinh = s.DonViTinh, CoHanDung = s.CoHanDung }).ToList();
        await ThemCoMaTuDat(db, spMoi, "SanPham", ct);

        var btCoSan = (await db.BienThe.Select(b => b.MaBT).ToListAsync(ct)).ToHashSet();
        var btMoi = SeedDataSet.BienThe.Where(b => !btCoSan.Contains(b.MaBT))
            .Select(b => new BienThe { MaBT = b.MaBT, MaSP = b.MaSP, MaVach = b.MaVach, ThuocTinh = b.ThuocTinh, Dai = b.Dai, Rong = b.Rong, Cao = b.Cao, KhoiLuong = b.KhoiLuong }).ToList();
        await ThemCoMaTuDat(db, btMoi, "BienThe", ct);
    }

    private static async Task ThemCoMaTuDat<T>(KhoDbContext db, List<T> dong, string bang, CancellationToken ct) where T : class
    {
        if (dong.Count == 0) return;
        await using var gd = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync(LenhIdentityInsert(bang, bat: true), ct);
        db.Set<T>().AddRange(dong);
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync(LenhIdentityInsert(bang, bat: false), ct);
        await gd.CommitAsync(ct);
    }

    // Tên bảng chỉ lấy từ danh sách cố định bên dưới, không bao giờ từ dữ liệu người dùng.
    private static string LenhIdentityInsert(string bang, bool bat) => (bang, bat) switch
    {
        ("SanPham", true) => "SET IDENTITY_INSERT [SanPham] ON",
        ("SanPham", false) => "SET IDENTITY_INSERT [SanPham] OFF",
        ("BienThe", true) => "SET IDENTITY_INSERT [BienThe] ON",
        ("BienThe", false) => "SET IDENTITY_INSERT [BienThe] OFF",
        _ => throw new ArgumentOutOfRangeException(nameof(bang), bang, "Bảng không có trong danh sách seed."),
    };
}
