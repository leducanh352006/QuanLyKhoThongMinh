using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>CRUD danh mục sản phẩm (màn hình Danh mục, việc T3-1). Không có TrangThai: xóa bị chặn khi còn sản phẩm tham chiếu.</summary>
public interface IDanhMucQuanLy
{
    Task<IReadOnlyList<DanhMucDong>> LayDanhSach(CancellationToken ct = default);

    Task<DanhMucDong?> Lay(int maDM, CancellationToken ct = default);

    Task<KetQua> Them(DanhMucMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maDM, DanhMucSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Xoa(int maDM, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class DanhMucService(IHangHoaStore store, IAuditService audit) : IDanhMucQuanLy
{
    public async Task<IReadOnlyList<DanhMucDong>> LayDanhSach(CancellationToken ct = default)
        => (await store.LayDanhMuc(ct)).Select(ChoHienThi).ToList();

    public async Task<DanhMucDong?> Lay(int maDM, CancellationToken ct = default)
    {
        var dm = await store.TimDanhMuc(maDM, ct);
        return dm is null ? null : ChoHienThi(dm);
    }

    public async Task<KetQua> Them(DanhMucMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenDM.Trim();
        var loi = KiemTraTen(ten);
        if (loi is not null) return loi;

        DanhMuc dm;
        try
        {
            dm = await store.ThemDanhMuc(new DanhMuc { TenDM = ten }, ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Tên danh mục đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Thêm danh mục", chiTiet: ten, ct: ct);
        return KetQua.Ok($"Đã thêm danh mục {ten}.");
    }

    public async Task<KetQua> Sua(int maDM, DanhMucSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var dm = await store.TimDanhMuc(maDM, ct);
        if (dm is null) return KetQua.Loi("Không tìm thấy danh mục.", MaLoiDichVu.KhongTimThay);

        var ten = doi.TenDM.Trim();
        var loi = KiemTraTen(ten);
        if (loi is not null) return loi;

        if (!string.Equals(ten, dm.TenDM, StringComparison.OrdinalIgnoreCase)
            && (await store.LayDanhMuc(ct)).Any(d => d.MaDM != maDM && string.Equals(d.TenDM, ten, StringComparison.OrdinalIgnoreCase)))
            return KetQua.Loi("Tên danh mục đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

        dm.TenDM = ten;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa danh mục", chiTiet: ten, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    public async Task<KetQua> Xoa(int maDM, int maNguoiThucHien, CancellationToken ct = default)
    {
        var dm = await store.TimDanhMuc(maDM, ct);
        if (dm is null) return KetQua.Loi("Không tìm thấy danh mục.", MaLoiDichVu.KhongTimThay);

        try
        {
            await store.XoaDanhMuc(dm, ct);
        }
        catch (XungDotDuLieuException ex)
        {
            return KetQua.Loi(ex.Message, MaLoiDichVu.SaiTrangThai);
        }

        await audit.Ghi(maNguoiThucHien, "Xóa danh mục", chiTiet: dm.TenDM, ct: ct);
        return KetQua.Ok($"Đã xóa danh mục {dm.TenDM}.");
    }

    private static KetQua? KiemTraTen(string ten)
        => string.IsNullOrWhiteSpace(ten) || ten.Length > 100
            ? KetQua.Loi("Tên danh mục không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static DanhMucDong ChoHienThi(DanhMuc d) => new(d.MaDM, d.TenDM);
}
