using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Bản thật của <see cref="ILockService"/>: khóa/mở ô qua cột <see cref="ViTri.DangKhoa"/> (Bảng 2.20), lưu qua
/// <see cref="IKhoViTriStore"/> nên dùng chung dữ liệu với T4-3 (EF Core hoặc bộ nhớ Development).
/// </summary>
public sealed class LockService(IKhoViTriStore store, IAuditService audit) : ILockService
{
    public async Task<KetQua> KhoaO(int maViTri, int maND, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return KetQua.Loi($"Không tìm thấy vị trí {maViTri}.", MaLoiDichVu.KhongTimThay);
        if (!v.LaO) return KetQua.Loi("Chỉ ô mới khóa được.", MaLoiDichVu.DuLieuKhongHopLe);
        if (v.DangKhoa) return KetQua.Loi($"Ô '{v.TenViTri}' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        v.DangKhoa = true;
        await store.LuuThayDoi(ct);
        await audit.Ghi(maND, "Khóa ô kiểm kê", chiTiet: v.TenViTri, ct: ct);
        return KetQua.Ok($"Đã khóa ô '{v.TenViTri}' để kiểm kê.");
    }

    public async Task<KetQua> MoO(int maViTri, int maND, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return KetQua.Loi($"Không tìm thấy vị trí {maViTri}.", MaLoiDichVu.KhongTimThay);
        if (!v.DangKhoa) return KetQua.Ok($"Ô '{v.TenViTri}' đang mở.");

        v.DangKhoa = false;
        await store.LuuThayDoi(ct);
        await audit.Ghi(maND, "Mở khóa ô kiểm kê", chiTiet: v.TenViTri, ct: ct);
        return KetQua.Ok($"Đã mở khóa ô '{v.TenViTri}'.");
    }

    public async Task<bool> DangKhoa(int maViTri, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        return v?.DangKhoa ?? false;
    }
}
