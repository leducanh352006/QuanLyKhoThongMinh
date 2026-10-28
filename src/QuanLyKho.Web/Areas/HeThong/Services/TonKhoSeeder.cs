using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Nạp TonKho từ <see cref="SeedDataSet"/> vào SQL Server, kèm một dòng BienDongTon loại Nhận hàng cho mỗi dòng tồn mẫu
/// (chứng từ giả NK số 0, người thực hiện là quản lý kho mẫu) để sổ cái khớp TonKho ngay từ đầu (DoiChieuSoCai rỗng).
/// Chạy sau LoHang (LoHangSeeder, ThuTu = 5) vì TonKho.MaLo tham chiếu LoHang đã seed trước. Không có cột tự tăng nên
/// không cần SET IDENTITY_INSERT. Chạy lại được nhiều lần: chỉ thêm dòng tồn còn thiếu, giữ nguyên tồn đã đổi qua nghiệp vụ.
/// </summary>
public sealed class TonKhoSeeder : IDbSeeder
{
    /// <summary>Người thực hiện ghi nhận tồn mẫu (quanlykho mẫu, xem SeedDataSet.NguoiDung).</summary>
    public const int MaNguoiGhiTonMau = 2;

    public int ThuTu => 6;

    public async Task SeedAsync(KhoDbContext db, CancellationToken ct = default)
    {
        var coSan = (await db.TonKho.Select(t => new { t.MaLo, t.MaViTri }).ToListAsync(ct))
            .Select(t => (t.MaLo, t.MaViTri)).ToHashSet();
        var toMoi = SeedDataSet.TonKho.Where(t => !coSan.Contains((t.MaLo, t.MaViTri))).ToList();
        if (toMoi.Count == 0) return;

        await using var gd = await db.Database.BeginTransactionAsync(ct);
        foreach (var t in toMoi)
        {
            db.TonKho.Add(new TonKho { MaLo = t.MaLo, MaViTri = t.MaViTri, SoLuongTon = t.SoLuongTon, SoLuongGiu = t.SoLuongGiu });
            if (t.SoLuongTon > 0)
            {
                db.BienDongTon.Add(new BienDongTon
                {
                    MaLo = t.MaLo, MaViTri = t.MaViTri, SoLuong = t.SoLuongTon,
                    LoaiBienDong = LoaiBienDongGiaTri.NhanHang, LoaiCT = "NK", MaCT = 0,
                    MaND = MaNguoiGhiTonMau, ThoiGianUtc = DateTime.UtcNow,
                });
            }
        }
        await db.SaveChangesAsync(ct);
        await gd.CommitAsync(ct);
    }
}
