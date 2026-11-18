using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>Một dòng hiển thị: tuyến vận chuyển kèm tên hai kho.</summary>
public sealed record TuyenVanChuyenDong(int MaKhoXuat, int MaKhoNhap, string TenKhoXuat, string TenKhoNhap, decimal DonGiaVC, decimal PhiChuyen);

/// <summary>
/// CRUD tuyến vận chuyển (màn hình Tuyến vận chuyển, việc T5-1): tham số chi phí giữa từng cặp kho, dùng làm đầu vào cho gợi ý
/// G4. Không có bảng Area riêng để giữ chỗ (stub) — chỉ cần KhoDbContext và Kho đã có từ Nhóm 1 (T4-3).
/// </summary>
public interface ITuyenVanChuyenQuanLy
{
    Task<IReadOnlyList<TuyenVanChuyenDong>> LayDanhSach(CancellationToken ct = default);

    Task<TuyenVanChuyen?> Lay(int maKhoXuat, int maKhoNhap, CancellationToken ct = default);

    Task<KetQua> Them(TuyenVanChuyen moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maKhoXuat, int maKhoNhap, decimal donGiaVC, decimal phiChuyen, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class TuyenVanChuyenService(KhoDbContext db, IAuditService audit) : ITuyenVanChuyenQuanLy
{
    public async Task<IReadOnlyList<TuyenVanChuyenDong>> LayDanhSach(CancellationToken ct = default) =>
        await db.TuyenVanChuyen.AsNoTracking()
            .Join(db.Kho.AsNoTracking(), t => t.MaKhoXuat, k => k.MaKho, (t, kx) => new { t, kx })
            .Join(db.Kho.AsNoTracking(), x => x.t.MaKhoNhap, k => k.MaKho, (x, kn) => new { x.t, x.kx, kn })
            .OrderBy(x => x.kx.TenKho).ThenBy(x => x.kn.TenKho)
            .Select(x => new TuyenVanChuyenDong(x.t.MaKhoXuat, x.t.MaKhoNhap, x.kx.TenKho, x.kn.TenKho, x.t.DonGiaVC, x.t.PhiChuyen))
            .ToListAsync(ct);

    public async Task<TuyenVanChuyen?> Lay(int maKhoXuat, int maKhoNhap, CancellationToken ct = default) =>
        await db.TuyenVanChuyen.AsNoTracking().Include(t => t.KhoXuat).Include(t => t.KhoNhap)
            .FirstOrDefaultAsync(t => t.MaKhoXuat == maKhoXuat && t.MaKhoNhap == maKhoNhap, ct);

    public async Task<KetQua> Them(TuyenVanChuyen moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var loi = moi.KiemTra();
        if (loi is not null) return KetQua.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        if (!await db.Kho.AnyAsync(k => k.MaKho == moi.MaKhoXuat, ct) || !await db.Kho.AnyAsync(k => k.MaKho == moi.MaKhoNhap, ct))
            return KetQua.Loi("Kho xuất hoặc kho nhập không tồn tại.", MaLoiDichVu.KhongTimThay);

        if (await db.TuyenVanChuyen.AnyAsync(t => t.MaKhoXuat == moi.MaKhoXuat && t.MaKhoNhap == moi.MaKhoNhap, ct))
            return KetQua.Loi("Tuyến giữa hai kho này đã tồn tại — hãy sửa tuyến sẵn có.", MaLoiDichVu.DuLieuKhongHopLe);

        db.TuyenVanChuyen.Add(new TuyenVanChuyen { MaKhoXuat = moi.MaKhoXuat, MaKhoNhap = moi.MaKhoNhap, DonGiaVC = moi.DonGiaVC, PhiChuyen = moi.PhiChuyen });
        await db.SaveChangesAsync(ct);

        await audit.Ghi(maNguoiThucHien, "Thêm tuyến vận chuyển", chiTiet: $"Kho {moi.MaKhoXuat} → Kho {moi.MaKhoNhap}", ct: ct);
        return KetQua.Ok("Đã thêm tuyến vận chuyển.");
    }

    public async Task<KetQua> Sua(int maKhoXuat, int maKhoNhap, decimal donGiaVC, decimal phiChuyen, int maNguoiThucHien, CancellationToken ct = default)
    {
        var t = await db.TuyenVanChuyen.FirstOrDefaultAsync(x => x.MaKhoXuat == maKhoXuat && x.MaKhoNhap == maKhoNhap, ct);
        if (t is null) return KetQua.Loi("Không tìm thấy tuyến vận chuyển.", MaLoiDichVu.KhongTimThay);

        if (donGiaVC < 0) return KetQua.Loi("Đơn giá vận chuyển không được âm.", MaLoiDichVu.DuLieuKhongHopLe);
        if (phiChuyen < 0) return KetQua.Loi("Phí mỗi chuyến không được âm.", MaLoiDichVu.DuLieuKhongHopLe);

        t.DonGiaVC = donGiaVC;
        t.PhiChuyen = phiChuyen;
        await db.SaveChangesAsync(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa tuyến vận chuyển", chiTiet: $"Kho {maKhoXuat} → Kho {maKhoNhap}", ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }
}
