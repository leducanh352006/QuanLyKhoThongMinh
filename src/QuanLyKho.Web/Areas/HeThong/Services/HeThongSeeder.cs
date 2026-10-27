using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Nạp VaiTro, PhanQuyen (ma trận mặc định) và 4 NguoiDung mẫu từ <see cref="SeedDataSet"/> vào SQL Server, giữ NGUYÊN mã
/// (MaVaiTro, MaND) để các nhóm khác tham chiếu bằng khóa ngoại (ví dụ Kho.MaQuanLy = 2). Chạy lại được nhiều lần: chỉ thêm dòng còn thiếu,
/// không đè quyền hoặc mật khẩu đã thay đổi. Chạy đầu tiên (ThuTu = 1) vì mọi bảng khác tham chiếu NguoiDung.
/// Mật khẩu mẫu lấy từ cấu hình <c>Auth:SeedPassword</c>; thiếu hoặc yếu thì bỏ qua người dùng mẫu và ghi cảnh báo.
/// </summary>
public sealed class HeThongSeeder(IConfiguration cauHinh, IMatKhauService matKhau, ILogger<HeThongSeeder> log) : IDbSeeder
{
    public int ThuTu => 1;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        // 1. Vai trò (cột identity nên phải bật IDENTITY_INSERT để giữ mã mẫu)
        var coSan = (await db.VaiTro.Select(v => v.TenVaiTro).ToListAsync(ct)).ToHashSet();
        var vaiTroMoi = SeedDataSet.VaiTro.Where(v => !coSan.Contains(v.TenVaiTro))
            .Select(v => new VaiTro { MaVaiTro = v.MaVaiTro, TenVaiTro = v.TenVaiTro }).ToList();
        await ThemCoMaTuDat(db, vaiTroMoi, "VaiTro", ct);

        // 2. Ma trận quyền mặc định: chỉ thêm (vai trò, chứng từ) chưa có
        var vaiTro = await db.VaiTro.AsNoTracking().ToListAsync(ct);
        var quyenCoSan = (await db.PhanQuyen.Select(p => new { p.MaVaiTro, p.LoaiChungTu }).ToListAsync(ct))
            .Select(p => (p.MaVaiTro, p.LoaiChungTu)).ToHashSet();
        var quyenMoi = vaiTro.SelectMany(v => QuyenMacDinh.Cho(v.MaVaiTro, v.TenVaiTro))
            .Where(p => !quyenCoSan.Contains((p.MaVaiTro, p.LoaiChungTu))).ToList();
        if (quyenMoi.Count > 0)
        {
            db.PhanQuyen.AddRange(quyenMoi);
            await db.SaveChangesAsync(ct);
        }

        // 3. Người dùng mẫu
        var mk = cauHinh[BoNhoHeThongStore.KhoaCauHinhMatKhauMau];
        var kiemTra = matKhau.KiemTraDoManh(mk);
        if (!kiemTra.ThanhCong)
        {
            log.LogWarning("Bỏ qua người dùng mẫu: cấu hình {Khoa} {LyDo}. Đặt bằng user-secrets (xem Areas/HeThong/README.md).",
                BoNhoHeThongStore.KhoaCauHinhMatKhauMau, string.IsNullOrEmpty(mk) ? "chưa được đặt" : "không đủ mạnh: " + kiemTra.ThongBao);
            return;
        }

        var tenCoSan = (await db.NguoiDung.Select(n => n.TenDangNhap).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hash = matKhau.Bam(mk!);
        var nguoiMoi = SeedDataSet.NguoiDung.Where(n => !tenCoSan.Contains(n.TenDangNhap))
            .Select(n => new NguoiDung
            {
                MaND = n.MaND, TenDangNhap = n.TenDangNhap, MatKhauHash = hash, HoTen = n.HoTen, Email = n.Email,
                MaVaiTro = n.MaVaiTro, TrangThai = TrangThaiNguoiDung.HoatDong,
            }).ToList();
        await ThemCoMaTuDat(db, nguoiMoi, "NguoiDung", ct);
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
        ("VaiTro", true) => "SET IDENTITY_INSERT [VaiTro] ON",
        ("VaiTro", false) => "SET IDENTITY_INSERT [VaiTro] OFF",
        ("NguoiDung", true) => "SET IDENTITY_INSERT [NguoiDung] ON",
        ("NguoiDung", false) => "SET IDENTITY_INSERT [NguoiDung] OFF",
        _ => throw new ArgumentOutOfRangeException(nameof(bang), bang, "Bảng không có trong danh sách seed."),
    };
}
