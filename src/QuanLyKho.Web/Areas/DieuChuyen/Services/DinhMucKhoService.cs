using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>Một dòng hiển thị: định mức kho kèm tên kho và tên biến thể.</summary>
public sealed record DinhMucKhoDong(int MaKho, int MaBT, string TenKho, string TenBienThe, int TonToiThieu, int NgayAnToan);

/// <summary>
/// CRUD định mức kho (màn hình Định mức kho, việc T5-1): tồn tối thiểu (m) và số ngày an toàn (N) của một biến thể tại một kho,
/// dùng làm đầu vào cho gợi ý G3. Không có bảng Area riêng để giữ chỗ — chỉ cần Kho (T4-3) và BienThe (T3-2) đã có.
/// </summary>
public interface IDinhMucKhoQuanLy
{
    Task<IReadOnlyList<DinhMucKhoDong>> LayDanhSach(CancellationToken ct = default);

    Task<DinhMucKho?> Lay(int maKho, int maBT, CancellationToken ct = default);

    Task<KetQua> Them(DinhMucKho moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maKho, int maBT, int tonToiThieu, int ngayAnToan, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class DinhMucKhoService(KhoDbContext db, IAuditService audit) : IDinhMucKhoQuanLy
{
    public async Task<IReadOnlyList<DinhMucKhoDong>> LayDanhSach(CancellationToken ct = default) =>
        await db.DinhMucKho.AsNoTracking()
            .Join(db.Kho.AsNoTracking(), d => d.MaKho, k => k.MaKho, (d, k) => new { d, k })
            .Join(db.BienThe.AsNoTracking().Join(db.SanPham.AsNoTracking(), b => b.MaSP, s => s.MaSP, (b, s) => new { b.MaBT, Ten = s.TenSP + " (" + b.ThuocTinh + ")" }),
                x => x.d.MaBT, b => b.MaBT, (x, b) => new { x.d, x.k, b.Ten })
            .OrderBy(x => x.k.TenKho).ThenBy(x => x.Ten)
            .Select(x => new DinhMucKhoDong(x.d.MaKho, x.d.MaBT, x.k.TenKho, x.Ten, x.d.TonToiThieu, x.d.NgayAnToan))
            .ToListAsync(ct);

    public async Task<DinhMucKho?> Lay(int maKho, int maBT, CancellationToken ct = default) =>
        await db.DinhMucKho.AsNoTracking().FirstOrDefaultAsync(d => d.MaKho == maKho && d.MaBT == maBT, ct);

    public async Task<KetQua> Them(DinhMucKho moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var loi = moi.KiemTra();
        if (loi is not null) return KetQua.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        if (!await db.Kho.AnyAsync(k => k.MaKho == moi.MaKho, ct)) return KetQua.Loi("Không tìm thấy kho.", MaLoiDichVu.KhongTimThay);
        if (!await db.BienThe.AnyAsync(b => b.MaBT == moi.MaBT, ct)) return KetQua.Loi("Không tìm thấy biến thể.", MaLoiDichVu.KhongTimThay);

        if (await db.DinhMucKho.AnyAsync(d => d.MaKho == moi.MaKho && d.MaBT == moi.MaBT, ct))
            return KetQua.Loi("Định mức cho biến thể này tại kho này đã tồn tại — hãy sửa định mức sẵn có.", MaLoiDichVu.DuLieuKhongHopLe);

        db.DinhMucKho.Add(new DinhMucKho { MaKho = moi.MaKho, MaBT = moi.MaBT, TonToiThieu = moi.TonToiThieu, NgayAnToan = moi.NgayAnToan });
        await db.SaveChangesAsync(ct);

        await audit.Ghi(maNguoiThucHien, "Thêm định mức kho", chiTiet: $"Kho {moi.MaKho}, biến thể {moi.MaBT}", ct: ct);
        return KetQua.Ok("Đã thêm định mức kho.");
    }

    public async Task<KetQua> Sua(int maKho, int maBT, int tonToiThieu, int ngayAnToan, int maNguoiThucHien, CancellationToken ct = default)
    {
        var d = await db.DinhMucKho.FirstOrDefaultAsync(x => x.MaKho == maKho && x.MaBT == maBT, ct);
        if (d is null) return KetQua.Loi("Không tìm thấy định mức kho.", MaLoiDichVu.KhongTimThay);

        if (tonToiThieu < 0) return KetQua.Loi("Tồn tối thiểu không được âm.", MaLoiDichVu.DuLieuKhongHopLe);
        if (ngayAnToan < 0) return KetQua.Loi("Số ngày an toàn không được âm.", MaLoiDichVu.DuLieuKhongHopLe);

        d.TonToiThieu = tonToiThieu;
        d.NgayAnToan = ngayAnToan;
        await db.SaveChangesAsync(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa định mức kho", chiTiet: $"Kho {maKho}, biến thể {maBT}", ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }
}
