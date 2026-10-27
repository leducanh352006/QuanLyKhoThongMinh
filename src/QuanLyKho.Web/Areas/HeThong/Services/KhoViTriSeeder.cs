using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Nạp Kho và ViTri từ <see cref="SeedDataSet"/> vào SQL Server, giữ nguyên mã (MaKho, MaViTri) để các nhóm khác tham chiếu.
/// Chạy sau <see cref="HeThongSeeder"/> (ThuTu = 1) vì Kho.MaQuanLy tham chiếu NguoiDung đã được seed trước.
/// Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu, giữ nguyên dữ liệu đã đổi qua màn hình quản lý.
/// </summary>
public sealed class KhoViTriSeeder : IDbSeeder
{
    public int ThuTu => 2;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var khoCoSan = (await db.Kho.Select(k => k.TenKho).ToListAsync(ct)).ToHashSet();
        var khoMoi = SeedDataSet.Kho.Where(k => !khoCoSan.Contains(k.TenKho))
            .Select(k => new Kho
            {
                MaKho = k.MaKho, TenKho = k.TenKho, LoaiKho = k.LoaiKho, DiaChi = k.DiaChi,
                MaQuanLy = k.MaQuanLy, ThoiGianCho = k.ThoiGianCho, TrangThai = TrangThaiHoatDong.HoatDong,
            }).ToList();
        await ThemCoMaTuDat(db, khoMoi, "Kho", ct);

        // ViTri tham chiếu MaViTriCha trong cùng danh sách; SeedDataSet.ViTri đã xếp cha trước con nên thêm theo đúng thứ tự đó là đủ.
        var viTriCoSan = (await db.ViTri.Select(v => v.MaViTri).ToListAsync(ct)).ToHashSet();
        var viTriMoi = SeedDataSet.ViTri.Where(v => !viTriCoSan.Contains(v.MaViTri))
            .Select(v => new ViTri
            {
                MaViTri = v.MaViTri, TenViTri = v.TenViTri, MaKho = v.MaKho, MaViTriCha = v.MaViTriCha, CapViTri = v.CapViTri,
                LoaiViTri = v.LoaiViTri, MaVachO = v.MaVachO, Dai = v.Dai, Rong = v.Rong, Cao = v.Cao,
                KhoiLuongToiDa = v.KhoiLuongToiDa, DangKhoa = false, TrangThai = TrangThaiHoatDong.HoatDong,
            }).ToList();
        await ThemCoMaTuDat(db, viTriMoi, "ViTri", ct);
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
        ("Kho", true) => "SET IDENTITY_INSERT [Kho] ON",
        ("Kho", false) => "SET IDENTITY_INSERT [Kho] OFF",
        ("ViTri", true) => "SET IDENTITY_INSERT [ViTri] ON",
        ("ViTri", false) => "SET IDENTITY_INSERT [ViTri] OFF",
        _ => throw new ArgumentOutOfRangeException(nameof(bang), bang, "Bảng không có trong danh sách seed."),
    };
}
