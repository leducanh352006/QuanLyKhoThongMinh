using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Nạp NhaCungCap và DanhMuc từ <see cref="SeedDataSet"/> vào SQL Server, giữ nguyên mã (MaNCC, MaDM) để các nhóm khác tham chiếu
/// (SanPham.MaDM). Chạy sau Kho/ViTri (ThuTu = 2); không phụ thuộc bảng nào nhưng SanPham cần DanhMuc có trước.
/// Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu, giữ nguyên dữ liệu đã đổi qua màn hình quản lý.
/// </summary>
public sealed class HangHoaSeeder : IDbSeeder
{
    public int ThuTu => 3;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var nccCoSan = (await db.NhaCungCap.Select(n => n.MaNCC).ToListAsync(ct)).ToHashSet();
        var nccMoi = SeedDataSet.NhaCungCap.Where(n => !nccCoSan.Contains(n.MaNCC))
            .Select(n => new NhaCungCap { MaNCC = n.MaNCC, TenNCC = n.TenNCC, SDT = n.SDT, DiaChi = n.DiaChi, TrangThai = TrangThaiNhaCungCap.HoatDong }).ToList();
        await ThemCoMaTuDat(db, nccMoi, "NhaCungCap", ct);

        var dmCoSan = (await db.DanhMuc.Select(d => d.MaDM).ToListAsync(ct)).ToHashSet();
        var dmMoi = SeedDataSet.DanhMuc.Where(d => !dmCoSan.Contains(d.MaDM))
            .Select(d => new DanhMuc { MaDM = d.MaDM, TenDM = d.TenDM }).ToList();
        await ThemCoMaTuDat(db, dmMoi, "DanhMuc", ct);
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
        ("NhaCungCap", true) => "SET IDENTITY_INSERT [NhaCungCap] ON",
        ("NhaCungCap", false) => "SET IDENTITY_INSERT [NhaCungCap] OFF",
        ("DanhMuc", true) => "SET IDENTITY_INSERT [DanhMuc] ON",
        ("DanhMuc", false) => "SET IDENTITY_INSERT [DanhMuc] OFF",
        _ => throw new ArgumentOutOfRangeException(nameof(bang), bang, "Bảng không có trong danh sách seed."),
    };
}
