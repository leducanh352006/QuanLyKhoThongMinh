using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>CRUD nhà cung cấp (màn hình Nhà cung cấp, việc T3-1). Khóa mềm bằng TrangThai.</summary>
public interface INhaCungCapQuanLy
{
    Task<IReadOnlyList<NhaCungCapDong>> LayDanhSach(CancellationToken ct = default);

    Task<NhaCungCapDong?> Lay(int maNCC, CancellationToken ct = default);

    /// <summary>Nhà cung cấp còn hoạt động, để chọn ở phiếu nhập (nhà cung cấp ngừng dùng không chọn được).</summary>
    Task<IReadOnlyList<NhaCungCapDong>> LayHoatDong(CancellationToken ct = default);

    Task<KetQua> Them(NhaCungCapMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maNCC, NhaCungCapSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> DatTrangThai(int maNCC, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class NhaCungCapService(IHangHoaStore store, IAuditService audit) : INhaCungCapQuanLy
{
    public async Task<IReadOnlyList<NhaCungCapDong>> LayDanhSach(CancellationToken ct = default)
        => (await store.LayNhaCungCap(ct)).Select(ChoHienThi).ToList();

    public async Task<NhaCungCapDong?> Lay(int maNCC, CancellationToken ct = default)
    {
        var ncc = await store.TimNhaCungCap(maNCC, ct);
        return ncc is null ? null : ChoHienThi(ncc);
    }

    public async Task<IReadOnlyList<NhaCungCapDong>> LayHoatDong(CancellationToken ct = default)
        => (await LayDanhSach(ct)).Where(n => n.DangHoatDong).ToList();

    public async Task<KetQua> Them(NhaCungCapMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenNCC.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraSdt(moi.SDT) ?? KiemTraDiaChi(moi.DiaChi);
        if (loi is not null) return loi;

        NhaCungCap ncc;
        try
        {
            ncc = await store.ThemNhaCungCap(new NhaCungCap { TenNCC = ten, SDT = Chuan(moi.SDT), DiaChi = Chuan(moi.DiaChi), TrangThai = TrangThaiNhaCungCap.HoatDong }, ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Tên nhà cung cấp đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Thêm nhà cung cấp", chiTiet: ten, ct: ct);
        return KetQua.Ok($"Đã thêm nhà cung cấp {ten}.");
    }

    public async Task<KetQua> Sua(int maNCC, NhaCungCapSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ncc = await store.TimNhaCungCap(maNCC, ct);
        if (ncc is null) return KetQua.Loi("Không tìm thấy nhà cung cấp.", MaLoiDichVu.KhongTimThay);

        var ten = doi.TenNCC.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraSdt(doi.SDT) ?? KiemTraDiaChi(doi.DiaChi);
        if (loi is not null) return loi;

        if (!string.Equals(ten, ncc.TenNCC, StringComparison.OrdinalIgnoreCase)
            && (await store.LayNhaCungCap(ct)).Any(n => n.MaNCC != maNCC && string.Equals(n.TenNCC, ten, StringComparison.OrdinalIgnoreCase)))
            return KetQua.Loi("Tên nhà cung cấp đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

        ncc.TenNCC = ten;
        ncc.SDT = Chuan(doi.SDT);
        ncc.DiaChi = Chuan(doi.DiaChi);
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa nhà cung cấp", chiTiet: ten, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    public async Task<KetQua> DatTrangThai(int maNCC, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ncc = await store.TimNhaCungCap(maNCC, ct);
        if (ncc is null) return KetQua.Loi("Không tìm thấy nhà cung cấp.", MaLoiDichVu.KhongTimThay);
        if (ncc.DangHoatDong == hoatDong) return KetQua.Ok(hoatDong ? "Nhà cung cấp đang hoạt động." : "Nhà cung cấp đã ngừng dùng từ trước.");

        ncc.TrangThai = hoatDong ? TrangThaiNhaCungCap.HoatDong : TrangThaiNhaCungCap.NgungHoatDong;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, hoatDong ? "Mở lại nhà cung cấp" : "Ngừng dùng nhà cung cấp", chiTiet: ncc.TenNCC, ct: ct);
        return KetQua.Ok(hoatDong ? $"Đã mở lại nhà cung cấp {ncc.TenNCC}." : $"Đã ngừng dùng nhà cung cấp {ncc.TenNCC}.");
    }

    private static KetQua? KiemTraTen(string ten)
        => string.IsNullOrWhiteSpace(ten) || ten.Length > 100
            ? KetQua.Loi("Tên nhà cung cấp không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraSdt(string? sdt)
        => sdt is { Length: > 15 } ? KetQua.Loi("Số điện thoại tối đa 15 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraDiaChi(string? diaChi)
        => diaChi is { Length: > 255 } ? KetQua.Loi("Địa chỉ tối đa 255 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static string? Chuan(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static NhaCungCapDong ChoHienThi(NhaCungCap n) => new(n.MaNCC, n.TenNCC, n.SDT, n.DiaChi, n.TrangThai);
}
